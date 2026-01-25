using System.Collections.Concurrent;
using Game.Application;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Services.Map;
using Game.Infrastructure.LiteNetLib;
using Game.Infrastructure.EfCore;
using Game.Simulation;

namespace Server.Host.WorldServer;

/// <summary>
/// Worker que gerencia o servidor de mundo do jogo.
/// Utiliza a simulação ECS para gerenciar entidades e processar comandos.
/// Suporta tanto movimento simples por delta quanto navegação por pathfinding.
/// </summary>
public class WorldServerWorker(
    IServiceScopeFactory scopeFactory,
    NetServer server,
    ILogger<WorldServerWorker>? logger = null) : BackgroundService
{
    private const int WorldPort = 9051;
    private const int TickIntervalMs = 1;

    // Configuração do mapa padrão
    private const int DefaultMapWidth = 100;
    private const int DefaultMapHeight = 100;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger?.LogInformation("WorldServer iniciando na porta {Port}", WorldPort);
        Console.WriteLine($"WorldServer listening on {WorldPort}");

        // Inicializa banco de dados
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            await DbInitializer.EnsureCreatedAndSeedAsync(db, stoppingToken);
        }

        // Cria mapa do mundo para navegação
        var worldMap = CreateDefaultWorldMap();

        // Usa a simulação baseada em ECS com suporte a navegação
        var simulation = new ServerWorldSimulation(worldMap, logger: logger);
        var commandQueue = new CommandQueue();
        var peerByCharacter = new ConcurrentDictionary<int, LiteNetLib.NetPeer>();
        var characterByPeer = new ConcurrentDictionary<int, int>();

        // Estado para delta compression por peer
        var lastSnapshotByPeer = new ConcurrentDictionary<int, WorldSnapshot>();
        var fullSnapshotCounter = 0;
        const int fullSnapshotInterval = 10; // Envia snapshot completo a cada 10 ticks

        logger?.LogInformation("Mapa criado: {Width}x{Height}",
            worldMap.Width, worldMap.Height);

        // Handler de desconexão
        server.PeerDisconnected += (peer, info) =>
        {
            if (characterByPeer.TryRemove(peer.Id, out var characterId))
            {
                peerByCharacter.TryRemove(characterId, out _);
                simulation.RemovePlayer(characterId);
                lastSnapshotByPeer.TryRemove(peer.Id, out _);
                logger?.LogDebug("Jogador {CharacterId} desconectado", characterId);
            }
        };

        // Handler de mensagens recebidas
        server.EnvelopeReceived += async (peer, envelope) =>
        {
            switch (envelope.OpCode)
            {
                case OpCode.WorldEnterRequest:
                    await HandleEnterWorld(peer, envelope, simulation, peerByCharacter, characterByPeer, stoppingToken);
                    break;

                case OpCode.WorldMoveCommand:
                    HandleMoveCommand(peer, envelope, characterByPeer, commandQueue);
                    break;

                case OpCode.WorldNavigateCommand:
                    HandleNavigateCommand(peer, envelope, characterByPeer, commandQueue);
                    break;

                case OpCode.WorldStopCommand:
                    HandleStopCommand(peer, envelope, characterByPeer, commandQueue);
                    break;

                case OpCode.WorldSnapshotRequest:
                    // Cliente solicitou snapshot completo (após perda de pacotes)
                    lastSnapshotByPeer.TryRemove(peer.Id, out _);
                    break;
            }
        };

        // Captura Ctrl+C para shutdown graceful
        Console.CancelKeyPress += (_, e) => { e.Cancel = true; };

        server.Start(WorldPort);

        // Game loop principal
        while (!stoppingToken.IsCancellationRequested)
        {
            server.PollEvents();

            // Processa comandos pendentes na simulação ECS
            commandQueue.Drain(simulation);

            // Atualiza a simulação ECS (processa sistemas e navegação)
            simulation.Update(TickIntervalMs);

            // Constrói snapshot atual
            var currentSnapshot = simulation.BuildSnapshot();
            fullSnapshotCounter++;

            // Envia para cada peer conectado
            foreach (var (characterId, peer) in peerByCharacter)
            {
                var hasLastSnapshot = lastSnapshotByPeer.TryGetValue(peer.Id, out var lastSnapshot);

                // Envia snapshot completo periodicamente ou se não há snapshot anterior
                var sendFullSnapshot = fullSnapshotCounter >= fullSnapshotInterval || !hasLastSnapshot;

                if (sendFullSnapshot)
                {
                    server.Send(peer, new Envelope(OpCode.WorldSnapshot, currentSnapshot),
                        LiteNetLib.DeliveryMethod.Unreliable);
                    lastSnapshotByPeer[peer.Id] = currentSnapshot;
                }
                else
                {
                    // Calcula e envia delta
                    var delta = SnapshotDeltaCalculator.Calculate(lastSnapshot, currentSnapshot);

                    // Só envia delta se houver mudanças
                    if (delta.HasChanges)
                    {
                        server.Send(peer, new Envelope(OpCode.WorldSnapshotDelta, delta),
                            LiteNetLib.DeliveryMethod.Unreliable);
                    }

                    lastSnapshotByPeer[peer.Id] = currentSnapshot;
                }
            }

            // Reseta contador após envio de snapshot completo
            if (fullSnapshotCounter >= fullSnapshotInterval)
                fullSnapshotCounter = 0;
            
            await Task.Delay(TickIntervalMs, stoppingToken);
        }

        logger?.LogInformation("WorldServer finalizado");
    }

    /// <summary>
    /// Cria o mapa padrão do mundo.
    /// </summary>
    private static WorldMap CreateDefaultWorldMap()
    {
        var map = new WorldMap(
            id: 1,
            name: "MainWorld",
            width: DefaultMapWidth,
            height: DefaultMapHeight,
            floors: 1,
            defaultSpawnX: DefaultMapWidth / 2,
            defaultSpawnY: DefaultMapHeight / 2);

        // Adiciona algumas paredes/obstáculos de exemplo (opcional)
        // map.SetTile(10, 10, 0, Tile.Blocked);

        return map;
    }

    private async Task HandleEnterWorld(
        LiteNetLib.NetPeer peer,
        Envelope envelope,
        ServerWorldSimulation simulation,
        ConcurrentDictionary<int, LiteNetLib.NetPeer> peerByCharacter,
        ConcurrentDictionary<int, int> characterByPeer,
        CancellationToken stoppingToken)
    {
        if (envelope.Payload is not EnterWorldRequest request)
        {
            server.Send(peer,
                new Envelope(OpCode.WorldEnterResponse,
                    new EnterWorldResponse(false, "Invalid payload", null)));
            return;
        }

        using var scope = scopeFactory.CreateScope();
        var world = scope.ServiceProvider.GetRequiredService<WorldUseCases>();
        var result = await world.EnterWorldAsync(request, stoppingToken);

        if (!result.Success || result.Value.Spawn is null)
        {
            server.Send(peer,
                new Envelope(OpCode.WorldEnterResponse, new EnterWorldResponse(false, result.Error, null)));
            return;
        }

        var spawn = result.Value.Spawn;

        // Adiciona jogador à simulação ECS com suporte a navegação
        simulation.UpsertPlayer(spawn.Value.CharacterId, spawn.Value.Name, spawn.Value.X, spawn.Value.Y, spawn.Value.Floor);
        peerByCharacter[spawn.Value.CharacterId] = peer;
        characterByPeer[peer.Id] = spawn.Value.CharacterId;

        logger?.LogDebug("Jogador {CharacterId} ({Name}) entrou no mundo em ({X}, {Y})",
            spawn.Value.CharacterId, spawn.Value.Name, spawn.Value.X, spawn.Value.Y);

        server.Send(peer, new Envelope(OpCode.WorldEnterResponse, result.Value));
    }

    private void HandleMoveCommand(
        LiteNetLib.NetPeer peer,
        Envelope envelope,
        ConcurrentDictionary<int, int> characterByPeer,
        CommandQueue commandQueue)
    {
        if (envelope.Payload is not WorldMoveCommand request)
            return;

        if (!characterByPeer.TryGetValue(peer.Id, out var characterId) ||
            characterId != request.CharacterId)
            return;
        
        logger?.LogTrace("Movimento solicitado: {CharacterId} -> (ΔX: {Dx}, ΔY: {Dy})",
            characterId, request.Dx, request.Dy);

        // Movimento simples por delta
        commandQueue.Enqueue(new DeltaMoveCommand(request.CharacterId, request.Dx, request.Dy));
    }

    private void HandleNavigateCommand(
        LiteNetLib.NetPeer peer,
        Envelope envelope,
        ConcurrentDictionary<int, int> characterByPeer,
        CommandQueue commandQueue)
    {
        if (envelope.Payload is not WorldNavigateCommand request)
            return;

        if (!characterByPeer.TryGetValue(peer.Id, out var characterId) ||
            characterId != request.CharacterId)
            return;

        // Navegação com pathfinding para posição específica
        commandQueue.Enqueue(new NavigateCommand(request.CharacterId, request.TargetX, request.TargetY,
            request.TargetFloor));
        
        logger?.LogTrace("Navegação solicitada: {CharacterId} -> ({X}, {Y})",
            characterId, request.TargetX, request.TargetY);
    }

    private void HandleStopCommand(
        LiteNetLib.NetPeer peer,
        Envelope envelope,
        ConcurrentDictionary<int, int> characterByPeer,
        CommandQueue commandQueue)
    {
        if (envelope.Payload is not WorldStopCommand request)
            return;

        if (!characterByPeer.TryGetValue(peer.Id, out var characterId) ||
            characterId != request.CharacterId)
            return;

        // Para movimento
        commandQueue.Enqueue(new StopMoveCommand(request.CharacterId));
    }
}
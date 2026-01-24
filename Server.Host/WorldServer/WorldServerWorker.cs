using System.Collections.Concurrent;
using Game.Application;
using Game.Contracts;
using Game.Infrastructure.LiteNetLib;
using Game.Persistence;
using Game.Simulation;

namespace Server.Host.WorldServer;

public class WorldServerWorker(IServiceScopeFactory scopeFactory, NetServer server) : BackgroundService
{
    private const int WorldPort = 9051;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        Console.WriteLine($"WorldServer listening on {WorldPort}");
        
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            await DbInitializer.EnsureCreatedAndSeedAsync(db, stoppingToken);
        }
        
        var worldState = new WorldState();
        var commandQueue = new CommandQueue();
        var peerByCharacter = new ConcurrentDictionary<int, LiteNetLib.NetPeer>();
        var characterByPeer = new ConcurrentDictionary<int, int>();

        server.PeerDisconnected += (peer, info) =>
        {
            if (characterByPeer.TryRemove(peer.Id, out var characterId))
            {
                peerByCharacter.TryRemove(characterId, out _);
            }
        };

        server.EnvelopeReceived += async (peer, envelope) =>
        {
            switch (envelope.OpCode)
            {
                case OpCode.WorldEnterRequest:
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
                    worldState.UpsertPlayer(spawn.Value.CharacterId, spawn.Value.Name, spawn.Value.X, spawn.Value.Y);
                    peerByCharacter[spawn.Value.CharacterId] = peer;
                    characterByPeer[peer.Id] = spawn.Value.CharacterId;

                    server.Send(peer, new Envelope(OpCode.WorldEnterResponse, result.Value));
                    break;
                }
                case OpCode.WorldMoveCommand:
                {
                    if (envelope.Payload is not WorldMoveCommand request)
                    {
                        return;
                    }

                    if (!characterByPeer.TryGetValue(peer.Id, out var characterId) ||
                        characterId != request.CharacterId)
                    {
                        return;
                    }

                    commandQueue.Enqueue(new MoveCommand(request.CharacterId, request.Dx, request.Dy));
                    break;
                }
            }
        };

        var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        server.Start(WorldPort);
        
        var tickInterval = TimeSpan.FromMilliseconds(100);
        
        while (!stoppingToken.IsCancellationRequested)
        {
            var nextTick = DateTime.UtcNow + tickInterval;
            server.PollEvents();

            if (DateTime.UtcNow >= nextTick)
            {
                commandQueue.Drain(worldState);
                var snapshot = worldState.BuildSnapshot();
                server.SendToAll(new Envelope(OpCode.WorldSnapshot, snapshot), LiteNetLib.DeliveryMethod.Unreliable);
                nextTick = DateTime.UtcNow + tickInterval;
            }

            await Task.Delay(10, stoppingToken).ContinueWith(_ => { });
        }
    }
}
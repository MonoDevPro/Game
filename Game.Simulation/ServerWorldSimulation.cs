using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS;
using Game.Infrastructure.ArchECS.Commons.Components;
using Game.Infrastructure.ArchECS.Services.Map;
using Game.Infrastructure.ArchECS.Services.Navigation;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Core;
using Microsoft.Extensions.Logging;

namespace Game.Simulation;

/// <summary>
/// Implementação concreta da simulação do mundo para o servidor.
/// Herda de WorldSimulation (ECS) e gerencia entidades de jogadores.
/// Integra com o módulo de navegação para pathfinding e movimento.
/// </summary>
public sealed class ServerWorldSimulation : WorldSimulation, IWorldSimulation
{
    private readonly ILogger? _logger;
    private readonly Dictionary<int, int> _playerIdToEntityId = new();
    private readonly WorldMap? _worldMap;
    
    /// <summary>
    /// Cria uma simulação com mapa e navegação completa.
    /// </summary>
    /// <param name="worldMap">Mapa do mundo para colisão e pathfinding.</param>
    /// <param name="navigationConfig">Configuração do sistema de navegação (opcional).</param>
    /// <param name="logger">Logger (opcional).</param>
    public ServerWorldSimulation(
        WorldMap worldMap,
        NavigationConfig? navigationConfig = null,
        ILogger? logger = null) : this(
        World.Create(
            chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: SimulationConfig.ArchetypeCapacity,
            entityCapacity: SimulationConfig.EntityCapacity),
        worldMap,
        navigationConfig,
        logger)
    {
        ConfigureSystems(World, Systems);
        Systems.Initialize();
        
        _logger?.LogInformation(
            "ServerWorldSimulation inicializada com mapa {MapId} ({Width}x{Height})", 
            worldMap.Id, worldMap.Width, worldMap.Height);
    }
    
    private ServerWorldSimulation(
        World world,
        WorldMap worldMap,
        NavigationConfig? navigationConfig = null,
        ILogger? logger = null
        ) : base(world, new NavigationModule(world, worldMap, navigationConfig), SimulationConfig.TickDeltaMilliseconds, logger)
    {
        _logger = logger;
        _worldMap = worldMap;
        
        ConfigureSystems(World, Systems);
        Systems.Initialize();
        
        _logger?.LogInformation("ServerWorldSimulation inicializada sem mapa");
    }

    /// <summary>
    /// Mapa do mundo (pode ser null se criado sem mapa).
    /// </summary>
    public WorldMap? Map => _worldMap;

    protected override void ConfigureSystems(World world, Group<float> systems)
    {
        // Sistemas de navegação são gerenciados pelo NavigationModule
        // Outros sistemas podem ser adicionados aqui conforme necessidade
        _logger?.LogInformation("ServerWorldSimulation: Sistemas configurados");
    }

    /// <summary>
    /// Adiciona ou atualiza um jogador no mundo ECS.
    /// Se navegação estiver habilitada, registra o jogador no mapa.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="name">Nome do jogador.</param>
    /// <param name="x">Posição X inicial.</param>
    /// <param name="y">Posição Y inicial.</param>
    /// <param name="floor">Andar (padrão 0).</param>
    /// <returns>A entidade criada ou atualizada.</returns>
    public Entity UpsertPlayer(int characterId, string name, int x, int y, int floor)
    {
        if (_playerIdToEntityId.TryGetValue(characterId, out var entityId))
            if (Navigation.Registry.TryGetEntity(entityId, out var e))
            {
                // Já registrado na navegação, apenas atualiza posição
                Navigation.MovePlayerDirectly(entityId, x, y, floor);
                return e;
            }

        // Cria nova entidade com componentes básicos
        var entity = World.Create(
            new CharacterId { Value = characterId },
            new PlayerName { Name = name }
        );

        // Mapeia characterId para entityId
        entityId = entity.Id;
        _playerIdToEntityId[characterId] = entityId;

        // Se tem navegação, adiciona componentes de navegação
        Navigation.AddNavigationComponents(entityId, entity, new Position { X = x, Y = y }, floor);
        
        _logger?.LogDebug("Jogador criado: {CharacterId} ({Name}) em ({X}, {Y}, {Floor})", 
            characterId, name, x, y, floor);
        return entity;
    }

    /// <summary>
    /// Solicita movimento de um jogador para uma posição usando pathfinding.
    /// Requer que a simulação tenha sido criada com um mapa.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="targetX">Posição X de destino.</param>
    /// <param name="targetY">Posição Y de destino.</param>
    /// <param name="targetFloor">Andar de destino.</param>
    /// <param name="flags">Flags de pathfinding.</param>
    /// <returns>True se a requisição foi aceita.</returns>
    public bool RequestPlayerMove(int characterId, int targetX, int targetY, int targetFloor, 
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!_playerIdToEntityId.TryGetValue(characterId, out var entityId))
        {
            _logger?.LogWarning("Jogador {CharacterId} não encontrado para movimento", characterId);
            return false;
        }

        Navigation.RequestPathfindingMove(entityId, targetX, targetY, targetFloor, flags);
        _logger?.LogTrace("Movimento solicitado para {CharacterId}: ({X}, {Y}, {Floor})", 
            characterId, targetX, targetY, targetFloor);
        
        return true;
    }
    
    public bool RequestPlayerMoveDelta(int characterId, int deltaX, int deltaY, 
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!_playerIdToEntityId.TryGetValue(characterId, out var entityId))
        {
            _logger?.LogWarning("Jogador {CharacterId} não encontrado para movimento delta", characterId);
            return false;
        }

        Navigation.RequestDirectionalMove(entityId, new Direction { X = deltaX, Y = deltaY }, flags);

        _logger?.LogTrace("Movimento delta solicitado para {CharacterId}: Δ({Dx}, {Dy})", 
            characterId, deltaX, deltaY);
        
        return true;
    }
    

    /// <summary>
    /// Para o movimento de um jogador.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <returns>True se o movimento foi parado.</returns>
    public bool StopPlayerMove(int characterId)
    {
        if (!_playerIdToEntityId.TryGetValue(characterId, out var entity))
            return false;

        Navigation.StopMovement(entity);
        return true;
    }

    /// <summary>
    /// Remove um jogador do mundo.
    /// </summary>
    /// <param name="characterId">ID do personagem a remover.</param>
    /// <returns>True se o jogador foi removido com sucesso.</returns>
    public bool RemovePlayer(int characterId)
    {
        if (!_playerIdToEntityId.TryGetValue(characterId, out var entityId))
        {
            _logger?.LogWarning("Tentativa de remover jogador {CharacterId}, mas não encontrado", characterId);
            return false;
        }
        
        if (!Navigation.TryRemoveNavigationComponents(entityId, out var entity))
        {
            _logger?.LogWarning("Tentativa de remover jogador {CharacterId}, mas entidade não encontrada", characterId);
            return false;
        }
        
        // Destroi a entidade no mundo ECS
        World.Destroy(entity);
        
        // Remove do dicionário de jogadores
        _playerIdToEntityId.Remove(characterId);
        _logger?.LogDebug("Jogador removido: {CharacterId}", characterId);
        return true;
    }

    /// <summary>
    /// Constrói um snapshot do estado atual do mundo para sincronização com clientes.
    /// Inclui posição, direção, estado de movimento e dados de interpolação.
    /// </summary>
    public WorldSnapshot BuildSnapshot()
    {
        var players = new List<PlayerState>(_playerIdToEntityId.Count);
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        
        foreach (Entity entity in Navigation.Registry.GetAllEntities())
        {
            var characterId = World.Get<CharacterId>(entity).Value;
            
            // Ignora entidades destruídas
            if (!_playerIdToEntityId.ContainsKey(characterId))
                continue;
            
            // Ignora entidades não vivas
            if (!World.IsAlive(entity))
                continue;
                
            var pos = World.Get<Position>(entity);
            var name = World.Get<PlayerName>(entity);
            var floor = World.Has<FloorId>(entity) ? World.Get<FloorId>(entity).Value : 0;
            var dir = World.Has<Direction>(entity) ? World.Get<Direction>(entity) : new Direction { X = 0, Y = 0 };
            var target = pos;
            
            bool isMoving = false;
            float moveProgress = 0f;
            
            if (World.Has<NavMovementState>(entity))
            {
                ref readonly var movement = ref World.Get<NavMovementState>(entity);
                if (movement.IsMoving)
                {
                    isMoving = true;
                    dir = movement.MovementDirection;
                    target = movement.TargetCell;
                    
                    // Calcula progresso do movimento (0.0 a 1.0)
                    if (movement.EndTick > movement.StartTick)
                    {
                        var totalDuration = movement.EndTick - movement.StartTick;
                        var elapsed = CurrentTick - movement.StartTick;
                        moveProgress = Math.Clamp((float)elapsed / totalDuration, 0f, 1f);
                    }
                }
            }
            players.Add(new PlayerState(
                characterId, 
                name.Name, 
                pos.X, 
                pos.Y, 
                floor, 
                dir.X, 
                dir.Y, 
                isMoving,
                target.X,
                target.Y,
                moveProgress));
        }

        return new WorldSnapshot(CurrentTick, timestamp, players);
    }

    /// <summary>
    /// Wrapper para implementar a interface IWorldSimulation.
    /// </summary>
    void IWorldSimulation.Update(long deltaTimeMs)
    {
        Update(in deltaTimeMs);
    }

    public override void Dispose()
    {
        Navigation?.Dispose();
        _playerIdToEntityId.Clear();
        base.Dispose();
    }
}

/// <summary>
/// Componente para armazenar o nome do jogador.
/// </summary>
public struct PlayerName
{
    public string Name;
}

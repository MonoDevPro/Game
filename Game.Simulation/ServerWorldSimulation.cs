using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS;
using Game.Infrastructure.ArchECS.Services.Combat;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.EntityRegistry;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Game.Infrastructure.ArchECS.Services.Navigation;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;
using Game.Infrastructure.ArchECS.Services.Navigation.Core;
using Game.Infrastructure.ArchECS.Services.Navigation.Map;
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
    private readonly CombatModule? _combat;
    private readonly NavigationModule? _navigation;

    /// <summary>
    /// Cria uma simulação com mapa e navegação completa.
    /// </summary>
    /// <param name="worldMap">Mapa do mundo para colisão e pathfinding.</param>
    /// <param name="navigationConfig">Configuração do sistema de navegação (opcional).</param>
    /// <param name="combatConfig">Configuração do sistema de combate (opcional).</param>
    /// <param name="logger">Logger (opcional).</param>
    public ServerWorldSimulation(
        WorldMap worldMap,
        NavigationConfig? navigationConfig = null,
        CombatConfig? combatConfig = null,
        ILogger? logger = null) : this(
        World.Create(
            chunkSizeInBytes: SimulationConfig.ChunkSizeInBytes,
            minimumAmountOfEntitiesPerChunk: SimulationConfig.MinimumAmountOfEntitiesPerChunk,
            archetypeCapacity: SimulationConfig.ArchetypeCapacity,
            entityCapacity: SimulationConfig.EntityCapacity),
        worldMap,
        navigationConfig,
        combatConfig,
        logger)
    {
        _logger?.LogInformation(
            "ServerWorldSimulation inicializada com mapa {MapId} ({Width}x{Height})",
            worldMap.Id, worldMap.Width, worldMap.Height);
    }

    private ServerWorldSimulation(
        World world,
        WorldMap worldMap,
        NavigationConfig? navigationConfig = null,
        CombatConfig? combatConfig = null,
        ILogger? logger = null
    ) : base(world, SimulationConfig.TickDeltaMilliseconds, logger)
    {
        _logger = logger;
        Map = worldMap;
        _navigation = new NavigationModule(world, worldMap, navigationConfig);
        _combat = new CombatModule(world, worldMap, combatConfig);

        ConfigureSystems(Systems);
        Systems.Initialize();
    }

    /// <summary>
    /// Mapa do mundo (pode ser null se criado sem mapa).
    /// </summary>
    public WorldMap? Map { get; }

    protected override void ConfigureSystems(Group<float> systems)
    {
        // Sistemas de navegação são gerenciados pelo NavigationModule
        // Outros sistemas podem ser adicionados aqui conforme necessidade
        _logger?.LogInformation("ServerWorldSimulation: Sistemas configurados");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    protected override void OnTick(in long serverTick)
    {
        base.OnTick(serverTick);

        _navigation?.Tick(serverTick);
        _combat?.Tick(serverTick);
    }

    /// <summary>
    /// Adiciona ou atualiza um jogador no mundo ECS.
    /// Se navegação estiver habilitada, registra o jogador no mapa.
    /// </summary>
    /// <param name="characterId">ID do personagem.</param>
    /// <param name="teamId">ID do time do personagem.</param>
    /// <param name="name">Nome do jogador.</param>
    /// <param name="x">Posição X inicial.</param>
    /// <param name="y">Posição Y inicial.</param>
    /// <param name="floor">Andar (padrão 0).</param>
    /// <param name="dirX">Direção X inicial.</param>
    /// <param name="dirY">Direção Y inicial.</param>
    /// <param name="vocation">Vocação do personagem.</param>
    /// <param name="level">Nível do personagem.</param>
    /// <param name="experience">Experiência do personagem.</param>
    /// <param name="strength">Força do personagem.</param>
    /// <param name="endurance">Resistência do personagem.</param>
    /// <param name="agility">Agilidade do personagem.</param>
    /// <param name="intelligence">Inteligência do personagem.</param>
    /// <param name="willpower">Força de vontade do personagem.</param>
    /// <param name="healthPoints">Pontos de vida do personagem.</param>
    /// <param name="manaPoints">Pontos de mana do personagem.</param>
    /// <returns>A entidade criada ou atualizada.</returns>
    public Entity UpsertPlayer(int characterId, int teamId, string name, int x, int y, int floor, int dirX, int dirY,
        byte vocation, int level, long experience, 
        int strength, int endurance, int agility, int intelligence, int willpower, 
        int healthPoints, int manaPoints)
    {
        // Cria nova entidade com componentes básicos
        var entity = World.Create(
            new CharacterId { Value = characterId },
            new PlayerName { Name = name }
        );

        // Mapeia characterId para entityId
        var entityId = entity.Id;
        _playerIdToEntityId[characterId] = entityId;
        
        // Registra no registry
        Registry.RegisterMultiDomain(entityId, entity, 
            EntityDomain.Navigation | EntityDomain.Combat);

        // Adiciona componentes de navegação
        entity = AddToNavigation(entity, x, y, floor, dirX, dirY);
        
        // Registra no módulo de combate
        entity = AddToCombat(entity, teamId, vocation, level, experience,
            strength, endurance, agility, intelligence, willpower,
            healthPoints, manaPoints);
        
        _logger?.LogDebug("Jogador criado: {CharacterId} ({Name}) em ({X}, {Y}, {Floor})",
            characterId, name, x, y, floor);
        return entity;
    }
    
    public bool RemovePlayer(int characterId)
    {
        if (!_playerIdToEntityId.TryGetValue(characterId, out var entityId))
            return false;

        if (!Registry.TryGetEntity(entityId, EntityDomain.Navigation, out var entity))
            return false;

        // Remove do módulo de navegação
        _navigation?.RemoveNavigationComponents(entity);
        
        // Remove do módulo de combate
        _combat?.RemoveCombatComponents(entity);
        
        Registry.Unregister(entity);
        
        _playerIdToEntityId.Remove(characterId);
        
        // Destroi entidade
        World.Destroy(entity);
        
        _logger?.LogDebug("Jogador removido: {CharacterId}", characterId);
        return true;
    }
    
    private Entity AddToNavigation(Entity entity, int x, int y, int floor, int dirX, int dirY)
    {
        if (_navigation is null)
            return entity;

        // Adiciona componentes de navegação
        _navigation.AddNavigationComponents(
            entity,
            new Position { X = x, Y = y },
            new Direction { X = dirX, Y = dirY },
            floor);

        return entity;
    }
    
    private Entity AddToCombat(Entity entity, int teamId, byte vocation, int level, long experience,
        int strength, int endurance, int agility, int intelligence, int willpower,
        int healthPoints, int manaPoints)
    {
        if (_combat is null)
            return entity;
        
        var stats = new CombatStats
        {
            Level = level,
            Experience = experience,
            Strength = strength,
            Endurance = endurance,
            Agility = agility,
            Intelligence = intelligence,
            Willpower = willpower,
            MaxHealth = healthPoints,
            MaxMana = manaPoints,
            CurrentHealth = healthPoints,
            CurrentMana = manaPoints
        };

        _combat.AddCombatComponents(entity, stats, vocation, teamId);
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

        _navigation?.RequestPathfindingMove(entityId, targetX, targetY, targetFloor, flags);
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

        _navigation?.RequestDirectionalMove(entityId, new Direction { X = deltaX, Y = deltaY }, flags);

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

        _navigation?.StopMovement(entity);
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
        
        if (_navigation is null)
            return new WorldSnapshot(CurrentTick, timestamp, players);

        foreach (Entity entity in Registry.GetEntitiesByDomain(EntityDomain.Navigation | EntityDomain.Combat))
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
            var currentHp = 0;
            var maxHp = 0;
            var currentMp = 0;
            var maxMp = 0;

            if (World.Has<CombatStats>(entity))
            {
                var stats = World.Get<CombatStats>(entity);
                currentHp = stats.CurrentHealth;
                maxHp = stats.MaxHealth;
                currentMp = stats.CurrentMana;
                maxMp = stats.MaxMana;
            }

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
                moveProgress,
                currentHp,
                maxHp,
                currentMp,
                maxMp));
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
        _navigation?.Dispose();
        _combat?.Dispose();
        _playerIdToEntityId.Clear();
        base.Dispose();
    }

    public bool RequestBasicAttack(int characterId, int dirX, int dirY)
    {
        if (_combat is null)
            return false;
        
        if (!_playerIdToEntityId.TryGetValue(characterId, out var entityId))
            return false;
        
        if (!Registry.TryGetEntity(entityId, EntityDomain.Combat, out var entity))
            return false;
        
        return _combat.RequestBasicAttack(entity, dirX, dirY, CurrentTick);
    }

    public bool TryDrainCombatEvents(out List<CombatEvent> events)
    {
        if (_combat is null)
        {
            events = [];
            return false;
        }

        return _combat.TryDrainEvents(out events);
    }
}

/// <summary>
/// Componente para armazenar o nome do jogador.
/// </summary>
public struct PlayerName
{
    public string Name;
}
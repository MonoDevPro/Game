using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.Contracts;
using Game.Infrastructure.ArchECS;
using Game.Infrastructure.ArchECS.Services.Combat;
using Game.Infrastructure.ArchECS.Services.Combat.Components;
using Game.Infrastructure.ArchECS.Services.Entities;
using Game.Infrastructure.ArchECS.Services.Entities.Components;
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
    private readonly EntityModule _entity;
    private readonly CombatModule _combat;
    private readonly NavigationModule _navigation;

    private readonly List<PlayerState> _snapshotBuffer = [];

    /// <summary>
    /// Cria uma simulação com mapa e navegação completa.
    /// </summary>
    /// <param name="world">Mundo ECS para a simulação.</param>
    /// <param name="worldMap">Mapa do mundo para colisão e pathfinding.</param>
    /// <param name="navigationConfig">Configuração do sistema de navegação (opcional).</param>
    /// <param name="combatConfig">Configuração do sistema de combate (opcional).</param>
    /// <param name="logger">Logger (opcional).</param>
    public ServerWorldSimulation(
        World world,
        WorldMap worldMap,
        NavigationConfig? navigationConfig = null,
        CombatConfig? combatConfig = null,
        ILogger? logger = null
    ) : base(world, SimulationConfig.TickDeltaMilliseconds, logger)
    {
        _logger = logger;
        Map = worldMap;
        _entity = new EntityModule(world, worldMap);
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
        var entity = _entity.CreatePlayerEntity(characterId, name);

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
        if (!_entity.TryGetEntityMetadataByCharacterId(characterId, out var playerMeta))
            return false;

        // Remove do módulo de navegação
        _navigation?.RemoveNavigationComponents(playerMeta.Entity);
        
        // Remove do módulo de combate
        _combat?.RemoveCombatComponents(playerMeta.Entity);
        
        _entity.DestroyPlayerEntity(characterId);

        _logger?.LogDebug("Jogador removido: {CharacterId}", characterId);
        return true;
    }
    
    private Entity AddToNavigation(Entity entity, int x, int y, int floor, int dirX, int dirY)
    {
        if (_navigation is null)
            return entity;

        _navigation.AddNavigationComponents(entity, x, y, dirX, dirY, floor);

        return entity;
    }
    
    private Entity AddToCombat(Entity entity, int teamId, byte vocation, int level, long experience,
        int strength, int endurance, int agility, int intelligence, int willpower,
        int healthPoints, int manaPoints)
    {
        if (_combat is null)
            return entity;
        

        _combat.AddCombatComponents(entity, level, experience, strength, endurance, agility, intelligence, willpower,
            healthPoints, manaPoints, healthPoints, manaPoints, vocation, teamId);
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
        if (!_entity.TryGetEntityMetadataByCharacterId(characterId, out var playerMeta))
            return false;

        _navigation?.RequestPathfindingMove(playerMeta.Entity, targetX, targetY, targetFloor, flags);
        _logger?.LogTrace("Movimento solicitado para {CharacterId}: ({X}, {Y}, {Floor})",
            characterId, targetX, targetY, targetFloor);

        return true;
    }

    public bool RequestPlayerMoveDelta(int characterId, int deltaX, int deltaY,
        PathRequestFlags flags = PathRequestFlags.None)
    {
        if (!_entity.TryGetEntityMetadataByCharacterId(characterId, out var entityMetadata))
            return false;

        _navigation?.RequestDirectionalMove(entityMetadata.Entity, new Direction { X = deltaX, Y = deltaY }, flags);

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
        if (!_entity.TryGetEntityMetadataByCharacterId(characterId, out var entityMetadata))
            return false;

        _navigation?.StopMovement(entityMetadata.Entity);
        return true;
    }

    /// <summary>
    /// Constrói um snapshot do estado atual do mundo para sincronização com clientes.
    /// Inclui posição, direção, estado de movimento e dados de interpolação.
    /// </summary>
    public WorldSnapshot BuildSnapshot()
    {
        var serverTick = CurrentTick;
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var players = _snapshotBuffer;
        players.Clear();

        int playerCount = _entity.GetPlayerEntityCount();

        if (playerCount > players.Capacity)
            players.Capacity = playerCount;

        if (playerCount == 0)
            return new WorldSnapshot(serverTick, timestamp, players);

        foreach (EntityMetadata meta in _entity.GetAllPlayerEntities())
        {
            if (TryBuildPlayerState(meta.ExternalId, meta, serverTick, out var state))
                players.Add(state);
        }

        return new WorldSnapshot(serverTick, timestamp, players);
    }

    public bool TryBuildPlayerState(int characterId, EntityMetadata metadata,long serverTick, out PlayerState state)
    {
        if (!_entity.TryGetEntityMetadataByCharacterId(characterId, out var playerMeta))
        {
            state = default;
            return false;
        }
        _combat.GetCombatVitals(metadata.Entity, out var currentHp, out var maxHp, out var currentMp, out var maxMp);
        _navigation.GetMovementState(metadata.Entity, serverTick, out NavEntityState navState);

        state = new PlayerState(
            metadata.ExternalId,
            playerMeta.Name,
            navState.CurrentX,
            navState.CurrentY,
            navState.FloorId,
            navState.DirectionX,
            navState.DirectionY,
            navState.IsMoving,
            navState.TargetX,
            navState.TargetY,
            navState.MoveProgress,
            currentHp,
            maxHp,
            currentMp,
            maxMp);

        return true;
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
        base.Dispose();
    }

    public bool RequestBasicAttack(int characterId, int dirX, int dirY)
    {
        if (!_entity.TryGetEntityMetadataByCharacterId(characterId, out var meta))
            return false;
        
        return _combat.RequestBasicAttack(meta.Entity, dirX, dirY, CurrentTick);
    }

    public bool TryDrainCombatEvents(List<CombatEvent> buffer, List<EntityCombatEvent> eventBuffer)
    {
        buffer.Clear();
        _combat.TryDrainEvents(eventBuffer);

        if (eventBuffer.Count == 0)
            return false;

        foreach (var evt in eventBuffer)
        {
            if (!_entity.TryGetPlayerEntityCharacterId(evt.Attacker, out var attackerId))
                attackerId = -1;

            if (!_entity.TryGetPlayerEntityCharacterId(evt.Target, out var targetId))
                targetId = -1;

            buffer.Add(new CombatEvent(
                Type: evt.Type,
                AttackerId: attackerId,
                TargetId: targetId,
                DirX: evt.DirX,
                DirY: evt.DirY,
                Damage: evt.Damage,
                X: evt.X,
                Y: evt.Y,
                Floor: evt.Floor,
                Speed: evt.Speed,
                Range: evt.Range));
        }

        return true;
    }
}

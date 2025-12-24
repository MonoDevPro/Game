using Arch.Core;
using System.Collections.Concurrent;
using GameECS.Server.Combat;
using GameECS.Server.Entities;
using GameECS.Server.Entities.Persistence;
using GameECS.Server.Navigation;
using GameECS.Server.Navigation.Components;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Components;
using GameECS.Shared.Entities.Data;
using GameECS.Shared.Navigation.Components;
using GameECS.Shared.Navigation.Data;
using MemoryPack;

namespace GameECS.Server;

/// <summary>
/// Dados para criação de jogador.
/// </summary>
[MemoryPackable]
public readonly partial record struct PlayerSpawnData(
    int AccountId,
    int CharacterId,
    int NetworkId,
    string Name,
    int X,
    int Y,
    int Level = 1,
    byte Vocation = 0,
    int Health = 100,
    int Mana = 50
);

/// <summary>
/// Dados de vitais para eventos.
/// </summary>
[MemoryPackable]
public readonly partial record struct VitalsData(
    int EntityId,
    int CurrentHp,
    int MaxHp,
    int CurrentMp,
    int MaxMp
);

/// <summary>
/// Simulação principal do servidor integrando todos os módulos ECS.
/// </summary>
public sealed class ServerGameSimulation : IDisposable
{
    private readonly World _world;
    private readonly ServerNavigationModule _navigation;
    private readonly ServerCombatModule _combat;
    private readonly ServerEntitiesModule _entities;
    
    private readonly ConcurrentDictionary<int, Entity> _networkIdToEntity = new();
    
    private long _currentTick;
    private bool _disposed;
    
    public World World => _world;
    public ServerNavigationModule Navigation => _navigation;
    public ServerCombatModule Combat => _combat;
    public ServerEntitiesModule Entities => _entities;
    public long CurrentTick => _currentTick;

    /// <summary>
    /// Evento disparado quando vitais mudam.
    /// </summary>
    public event Action<VitalsData>? OnVitalsChanged;

    public ServerGameSimulation(
        int mapWidth = 200, 
        int mapHeight = 200,
        INpcTemplateProvider? npcTemplateProvider = null,
        IPetTemplateProvider? petTemplateProvider = null,
        IPlayerPersistence? playerPersistence = null,
        IPetPersistence? petPersistence = null)
    {
        _world = World.Create();
        
        // Inicializa módulos
        _navigation = new ServerNavigationModule(_world, mapWidth, mapHeight);
        _combat = new ServerCombatModule(_world);
        _entities = new ServerEntitiesModule(
            _world, 
            npcTemplateProvider, 
            petTemplateProvider, 
            playerPersistence, 
            petPersistence);
        
        // Subscreve eventos
        _combat.OnDamageDealt += HandleDamageDealt;
        _combat.OnEntityDeath += HandleEntityDeath;
    }

    /// <summary>
    /// Atualiza a simulação.
    /// </summary>
    public void Update(float deltaTime)
    {
        _currentTick++;
        
        // Atualiza módulos na ordem correta
        _navigation.Tick(_currentTick);
        _combat.Tick(_currentTick);
        _entities.Update(_currentTick);
    }

    /// <summary>
    /// Cria uma entidade de jogador.
    /// </summary>
    public Entity CreatePlayerEntity(PlayerSpawnData spawnData)
    {
        // Cria entidade base via módulo de entidades
        var entity = _entities.CreatePlayer(
            spawnData.AccountId,
            spawnData.CharacterId,
            spawnData.Name,
            spawnData.Level,
            spawnData.X,
            spawnData.Y);
        
        // Adiciona componentes de navegação
        _world.Add(entity, 
            new GridPosition(spawnData.X, spawnData.Y),
            new ServerMovement(),
            new GridPathBuffer(),
            new PathState(),
            ServerAgentConfig.Default,
            new NavigationAgent());
        
        // Adiciona componentes de combate
        var vocation = (VocationType)spawnData.Vocation;
        _combat.AddCombatComponents(entity, vocation, spawnData.Level);
        
        // Sobrescreve HP/MP se necessário
        if (_world.TryGet<Health>(entity, out var health))
        {
            health = new Health(spawnData.Health);
            _world.Set(entity, health);
        }
        if (_world.TryGet<Mana>(entity, out var mana))
        {
            mana = new Mana(spawnData.Mana);
            _world.Set(entity, mana);
        }
        
        // Adiciona NetworkId para mapeamento
        _world.Add(entity, new NetworkId { Value = spawnData.NetworkId });
        
        // Registra mapeamentos
        _networkIdToEntity[spawnData.NetworkId] = entity;
        
        // Ocupa célula no grid
        _navigation.Grid.TryOccupy(new GridPosition(spawnData.X, spawnData.Y), entity.Id);
        
        return entity;
    }

    /// <summary>
    /// Cria uma entidade de NPC.
    /// </summary>
    public Entity CreateNpcEntity(string templateId, int x, int y)
    {
        var entity = _entities.CreateNpc(templateId, x, y);
        
        // Adiciona componentes de navegação
        _world.Add(entity,
            new GridPosition(x, y),
            new ServerMovement(),
            new GridPathBuffer(),
            new PathState(),
            ServerAgentConfig.Default,
            new NavigationAgent());
        
        _navigation.Grid.TryOccupy(new GridPosition(x, y), entity.Id);
        
        return entity;
    }

    /// <summary>
    /// Remove uma entidade de jogador.
    /// </summary>
    public void DestroyPlayer(int networkId)
    {
        if (!_networkIdToEntity.TryRemove(networkId, out var entity))
            return;
        
        if (!_world.IsAlive(entity))
            return;
        
        // Libera posição no grid
        if (_world.TryGet<GridPosition>(entity, out var pos))
        {
            _navigation.Grid.Release(pos, entity.Id);
        }
        
        _world.Destroy(entity);
    }

    /// <summary>
    /// Aplica input de movimento de um jogador.
    /// </summary>
    public bool ApplyPlayerInput(int peerId, MoveInputData input)
    {
        // Encontra a entidade pelo peerId (que é o networkId)
        if (!_networkIdToEntity.TryGetValue(peerId, out var entity))
            return false;
        
        if (!_world.IsAlive(entity))
            return false;
        
        // Solicita movimento via módulo de navegação
        var target = new GridPosition(input.TargetX, input.TargetY);
        _navigation.RequestMoveTo(entity, target);
        
        return true;
    }

    /// <summary>
    /// Obtém snapshot de movimento de uma entidade.
    /// </summary>
    public MovementSnapshot GetMovementSnapshot(Entity entity)
    {
        return _navigation.GetSnapshot(entity, _currentTick);
    }

    /// <summary>
    /// Obtém entidade por networkId.
    /// </summary>
    public bool TryGetEntity(int networkId, out Entity entity)
    {
        return _networkIdToEntity.TryGetValue(networkId, out entity);
    }

    /// <summary>
    /// Obtém posição de uma entidade.
    /// </summary>
    public GridPosition? GetEntityPosition(int networkId)
    {
        if (!_networkIdToEntity.TryGetValue(networkId, out var entity))
            return null;
        
        if (!_world.IsAlive(entity))
            return null;
        
        if (_world.TryGet<GridPosition>(entity, out var pos))
            return pos;
        
        return null;
    }

    private void HandleDamageDealt(DamageMessage msg)
    {
        // Dispara evento de vitais se necessário
        if (_networkIdToEntity.TryGetValue(msg.TargetId, out var entity))
        {
            if (_world.TryGet<Health>(entity, out var health))
            {
                if (_world.TryGet<Mana>(entity, out var mana))
                {
                    OnVitalsChanged?.Invoke(new VitalsData(
                        msg.TargetId,
                        health.Current,
                        health.Maximum,
                        mana.Current,
                        mana.Maximum));
                }
            }
        }
    }

    private void HandleEntityDeath(DeathMessage msg)
    {
        // Lógica de morte pode ser implementada aqui
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _combat.OnDamageDealt -= HandleDamageDealt;
        _combat.OnEntityDeath -= HandleEntityDeath;
        
        _navigation.Dispose();
        _combat.Dispose();
        _entities.Dispose();
        
        _world.Dispose();
    }
}

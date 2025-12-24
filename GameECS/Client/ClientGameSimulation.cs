using Arch.Core;
using GameECS.Client.Combat;
using GameECS.Client.Entities;
using GameECS.Client.Navigation;
using GameECS.Client.Navigation.Components;
using GameECS.Client.Navigation.Systems;
using GameECS.Shared.Combat.Components;
using GameECS.Shared.Entities.Components;

namespace GameECS.Client;

/// <summary>
/// Visual base para entidades.
/// </summary>
public interface IEntityVisual
{
    void Initialize(int networkId, int x, int y);
    void Destroy();
}

/// <summary>
/// Simulação principal do cliente integrando todos os módulos ECS.
/// </summary>
public sealed class ClientGameSimulation : IDisposable
{
    private readonly World _world;
    private readonly ClientNavigationModule _navigation;
    private readonly ClientCombatModule _combat;
    private readonly ClientEntitiesModule _entities;
    
    private readonly Dictionary<int, Entity> _players = new();
    private readonly Dictionary<int, Entity> _npcs = new();
    private readonly Dictionary<int, IEntityVisual?> _visuals = new();
    
    private int _localNetworkId = -1;
    private Entity _localPlayerEntity = Entity.Null;
    private bool _disposed;

    public World World => _world;
    public ClientNavigationModule NavigationModule => _navigation;
    public ClientCombatModule CombatModule => _combat;
    public ClientEntitiesModule EntitiesModule => _entities;
    public int LocalNetworkId => _localNetworkId;
    public Entity LocalPlayerEntity => _localPlayerEntity;

    public ClientGameSimulation(
        float cellSize = 32f,
        float tickRate = 60f,
        IInputProvider? inputProvider = null,
        INetworkSender? networkSender = null)
    {
        _world = World.Create();
        
        _navigation = new ClientNavigationModule(
            _world, 
            cellSize, 
            tickRate,
            inputProvider,
            networkSender);
        
        _combat = new ClientCombatModule(_world);
        _entities = new ClientEntitiesModule(_world);
    }

    /// <summary>
    /// Atualiza a simulação.
    /// </summary>
    public void Update(float deltaTime)
    {
        _navigation.Update(deltaTime);
        _combat.Update(deltaTime);
    }

    /// <summary>
    /// Cria o jogador local.
    /// </summary>
    public Entity CreateLocalPlayer(in PlayerData data, IEntityVisual? visual = null)
    {
        _localNetworkId = data.NetworkId;
        
        var entity = _navigation.CreateEntity(data.NetworkId, data.X, data.Y, isLocalPlayer: true);
        _localPlayerEntity = entity;
        
        // Adiciona componentes de identidade
        _world.Add(entity, new NetworkId(data.NetworkId));
        _world.Add(entity, new PlayerTag());
        _world.Add(entity, new LocalPlayerTag());
        
        // Adiciona componentes de combate
        _world.Add(entity, new Health { Current = data.Hp, Maximum = data.MaxHp });
        _world.Add(entity, new Mana { Current = data.Mp, Maximum = data.MaxMp });
        
        _players[data.NetworkId] = entity;
        
        if (visual != null)
        {
            visual.Initialize(data.NetworkId, data.X, data.Y);
            _visuals[data.NetworkId] = visual;
        }
        
        return entity;
    }

    /// <summary>
    /// Cria um jogador remoto.
    /// </summary>
    public Entity CreateRemotePlayer(in PlayerData data, IEntityVisual? visual = null)
    {
        var entity = _navigation.CreateEntity(data.NetworkId, data.X, data.Y, isLocalPlayer: false);
        
        // Adiciona componentes de identidade
        _world.Add(entity, new NetworkId(data.NetworkId));
        _world.Add(entity, new PlayerTag());
        
        // Adiciona componentes de combate
        _world.Add(entity, new Health { Current = data.Hp, Maximum = data.MaxHp });
        _world.Add(entity, new Mana { Current = data.Mp, Maximum = data.MaxMp });
        
        _players[data.NetworkId] = entity;
        
        if (visual != null)
        {
            visual.Initialize(data.NetworkId, data.X, data.Y);
            _visuals[data.NetworkId] = visual;
        }
        
        return entity;
    }

    /// <summary>
    /// Cria um NPC.
    /// </summary>
    public Entity CreateNpc(in NpcData data, IEntityVisual? visual = null)
    {
        var entity = _navigation.CreateEntity(data.NetworkId, data.X, data.Y, isLocalPlayer: false);
        
        // Adiciona componentes de identidade
        _world.Add(entity, new NetworkId(data.NetworkId));
        _world.Add(entity, new NpcTag());
        
        // Adiciona componentes de combate
        _world.Add(entity, new Health { Current = data.Hp, Maximum = data.MaxHp });
        _world.Add(entity, new Mana { Current = data.Mp, Maximum = data.MaxMp });
        
        _npcs[data.NetworkId] = entity;
        
        if (visual != null)
        {
            visual.Initialize(data.NetworkId, data.X, data.Y);
            _visuals[data.NetworkId] = visual;
        }
        
        return entity;
    }

    /// <summary>
    /// Destrói qualquer entidade por network ID.
    /// </summary>
    public void DestroyAny(int networkId)
    {
        // Remove visual
        if (_visuals.TryGetValue(networkId, out var visual))
        {
            visual?.Destroy();
            _visuals.Remove(networkId);
        }
        
        // Remove da navegação
        _navigation.DestroyEntity(networkId);
        
        // Remove dos dicionários
        _players.Remove(networkId);
        _npcs.Remove(networkId);
    }

    /// <summary>
    /// Tenta obter qualquer entidade (player ou NPC) por network ID.
    /// </summary>
    public bool TryGetAnyEntity(int networkId, out Entity entity)
    {
        if (_players.TryGetValue(networkId, out entity))
            return true;
        
        if (_npcs.TryGetValue(networkId, out entity))
            return true;
        
        entity = Entity.Null;
        return false;
    }

    /// <summary>
    /// Aplica snapshot de vitais.
    /// </summary>
    public void ApplyVitals(in VitalsSnapshot snapshot)
    {
        if (!TryGetAnyEntity(snapshot.NetworkId, out var entity))
            return;
        
        if (_world.TryGet<Health>(entity, out var health))
        {
            health.Current = snapshot.CurrentHp;
            health.Maximum = snapshot.MaxHp;
            _world.Set(entity, health);
        }
        
        if (_world.TryGet<Mana>(entity, out var mana))
        {
            mana.Current = snapshot.CurrentMp;
            mana.Maximum = snapshot.MaxMp;
            _world.Set(entity, mana);
        }
    }

    /// <summary>
    /// Aplica snapshot de movimento do servidor.
    /// </summary>
    public void ApplyMovementSnapshot(MovementSnapshot snapshot)
    {
        _navigation.OnMovementSnapshot(snapshot);
    }

    /// <summary>
    /// Obtém posição visual de uma entidade.
    /// </summary>
    public (float X, float Y)? GetVisualPosition(int networkId)
    {
        var entity = _navigation.GetEntity(networkId);
        if (entity == null)
            return null;
        
        if (!_world.TryGet<VisualPosition>(entity.Value, out var pos))
            return null;
        
        return (pos.X, pos.Y);
    }

    /// <summary>
    /// Obtém o visual de uma entidade.
    /// </summary>
    public IEntityVisual? GetVisual(int networkId)
    {
        return _visuals.TryGetValue(networkId, out var visual) ? visual : null;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        // Limpa visuals
        foreach (var visual in _visuals.Values)
            visual?.Destroy();
        _visuals.Clear();
        
        _navigation.Dispose();
        _combat.Dispose();
        _entities.Dispose();
        _world.Dispose();
    }
}

// Tags para identificação
public struct LocalPlayerTag { }
public struct PlayerTag { }
public struct NpcTag { }

using Arch.Core;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema central de eventos para o ECS.
/// Fornece callback hooks para eventos importantes do jogo.
/// </summary>
public class GameEventSystem
{
    // ============================================
    // Lifecycle Events
    // ============================================
    
    public event Action<Entity>? OnEntitySpawned;
    public event Action<Entity>? OnEntityDespawned;
    
    public event Action<int>? OnPlayerJoined;      // PlayerNetworkId
    public event Action<int>? OnPlayerLeft;        // PlayerNetworkId
    
    // ============================================
    // Gameplay Events
    // ============================================
    
    public event Action<Entity?, Entity, int>? OnDamage;  // Attacker, Victim, Damage
    public event Action<Entity?, Entity, int>? OnHeal;    // Healer, Target, Amount
    public event Action<Entity, Entity?>? OnDeath;       // DeadEntity, Killer (nullable)
    
    // ============================================
    // State Events
    // ============================================
    
    public event Action<Entity>? OnCombatEnter;
    public event Action<Entity>? OnCombatExit;
    public event Action<Entity, string>? OnStatusApplied;   // Entity, StatusType
    public event Action<Entity, string>? OnStatusRemoved;   // Entity, StatusType
    
    // ============================================
    // Movement Events
    // ============================================
    
    public event Action<Entity, int, int>? OnPositionChanged;  // Entity, NewX, NewY
    public event Action<Entity, int, int>? OnFacingChanged;    // Entity, DirectionX, DirectionY
    
    // ============================================
    // Network Events
    // ============================================
    
    public event Action<Entity>? OnNetworkDirty;
    public event Action<Entity>? OnNetworkSync;
    
    // ============================================
    // Invoke Methods (called by systems)
    // ============================================
    
    public void RaiseEntitySpawned(Entity entity) => OnEntitySpawned?.Invoke(entity);
    public void RaiseEntityDespawned(Entity entity) => OnEntityDespawned?.Invoke(entity);
    
    public void RaisePlayerJoined(int networkId) => OnPlayerJoined?.Invoke(networkId);
    public void RaisePlayerLeft(int networkId) => OnPlayerLeft?.Invoke(networkId);
    
    public void RaiseDamage(Entity? attacker, Entity victim, int damage) => OnDamage?.Invoke(attacker, victim, damage);
    public void RaiseHeal(Entity? healer, Entity target, int amount) => OnHeal?.Invoke(healer, target, amount);
    public void RaiseDeath(Entity deadEntity, Entity? killer = null) => OnDeath?.Invoke(deadEntity, killer);
    
    public void RaiseCombatEnter(Entity entity) => OnCombatEnter?.Invoke(entity);
    public void RaiseCombatExit(Entity entity) => OnCombatExit?.Invoke(entity);
    public void RaiseStatusApplied(Entity entity, string statusType) => OnStatusApplied?.Invoke(entity, statusType);
    public void RaiseStatusRemoved(Entity entity, string statusType) => OnStatusRemoved?.Invoke(entity, statusType);
    
    public void RaisePositionChanged(Entity entity, int x, int y) => OnPositionChanged?.Invoke(entity, x, y);
    public void RaiseFacingChanged(Entity entity, int dx, int dy) => OnFacingChanged?.Invoke(entity, dx, dy);
    
    public void RaiseNetworkDirty(Entity entity) => OnNetworkDirty?.Invoke(entity);
    public void RaiseNetworkSync(Entity entity) => OnNetworkSync?.Invoke(entity);
}

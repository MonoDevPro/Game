using Arch.Core;
using Game.ECS.Components;

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
    
    public event Action<Entity>? OnPlayerJoined;      // PlayerNetworkId
    public event Action<Entity>? OnPlayerLeft;        // PlayerNetworkId
    
    public event Action<Entity, sbyte, sbyte, InputFlags>? OnPlayerInput; // Entity, InputX, InputY, InputFlags
    
    // ============================================
    // Gameplay Events
    // ============================================
    
    public event Action<Entity?, Entity, int>? OnDamage;  // Attacker, Victim, Damage
    public event Action<Entity?, Entity, int>? OnHealHp;    // Healer, Target, Amount
    public event Action<Entity?, Entity, int>? OnHealMp;    // Healer, Target, Amount
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
    // Invoke Methods (called by systems)
    // ============================================
    public void RaiseEntitySpawned(Entity entity) => OnEntitySpawned?.Invoke(entity);
    public void RaiseEntityDespawned(Entity entity) => OnEntityDespawned?.Invoke(entity);
    
    public void RaisePlayerJoined(Entity entity) => OnPlayerJoined?.Invoke(entity);
    public void RaisePlayerLeft(Entity entity) => OnPlayerLeft?.Invoke(entity);
    public void RaisePlayerInput(Entity entity, sbyte inputX, sbyte inputY, InputFlags flags) => OnPlayerInput?.Invoke(entity, inputX, inputY, flags);
    
    public void RaiseDamage(Entity? attacker, Entity victim, int damage) => OnDamage?.Invoke(attacker, victim, damage);
    public void RaiseHealHp(Entity? healer, Entity target, int amount) => OnHealHp?.Invoke(healer, target, amount);
    public void RaiseHealMp(Entity? healer, Entity target, int amount) => OnHealMp?.Invoke(healer, target, amount);
    public void RaiseDeath(Entity deadEntity, Entity? killer = null) => OnDeath?.Invoke(deadEntity, killer);
    
    public void RaiseCombatEnter(Entity entity) => OnCombatEnter?.Invoke(entity);
    public void RaiseCombatExit(Entity entity) => OnCombatExit?.Invoke(entity);
    public void RaiseStatusApplied(Entity entity, string statusType) => OnStatusApplied?.Invoke(entity, statusType);
    public void RaiseStatusRemoved(Entity entity, string statusType) => OnStatusRemoved?.Invoke(entity, statusType);
    
    public void RaisePositionChanged(Entity entity, int x, int y) => OnPositionChanged?.Invoke(entity, x, y);
    public void RaiseFacingChanged(Entity entity, int dx, int dy) => OnFacingChanged?.Invoke(entity, dx, dy);
}

using Arch.Core;

namespace Game.ECS.Systems;

/// <summary>
/// Sistema central de eventos para o ECS.
/// Fornece callback hooks para eventos importantes do jogo.
/// </summary>
public sealed class GameEventSystem
{
    // ============================================
    // Lifecycle Events
    // ============================================
    
    public event Action<Entity>? OnPlayerJoined;            // PlayerNetworkId
    public event Action<Entity>? OnPlayerLeft;              // PlayerNetworkId
    
    // Gameplay Events
    // ============================================
    
    public event Action<Entity, sbyte, sbyte, ushort>? OnPlayerInput; // Entity, InputX, InputY, InputFlags
    public event Action<Entity?, Entity, int>? OnDamage;    // Attacker, Victim, Damage
    public event Action<Entity?, Entity, int>? OnHealHp;    // Healer, Target, Amount
    public event Action<Entity?, Entity, int>? OnHealMp;    // Healer, Target, Amount
    public event Action<Entity, Entity?>? OnDeath;          // DeadEntity, Killer (nullable)
    
    // ============================================
    // State Events
    // ============================================
    public event Action<Entity>? OnCombatEnter;
    public event Action<Entity>? OnCombatExit;
    
    // ============================================
    // Movement Events
    // ============================================
    public event Action<Entity, int, int, int>? OnPositionChanged;  // Entity, NewX, NewY, NewZ
    public event Action<Entity, int, int>? OnFacingChanged;         // Entity, DirectionX, DirectionY
    
    // ============================================
    // Invoke Methods (called by systems)
    // ============================================
    public void RaisePlayerJoined(Entity entity) => OnPlayerJoined?.Invoke(entity);
    public void RaisePlayerLeft(Entity entity) => OnPlayerLeft?.Invoke(entity);
    
    public void RaisePlayerInput(Entity entity, sbyte inputX, sbyte inputY, ushort flags) => OnPlayerInput?.Invoke(entity, inputX, inputY, flags);
    public void RaiseDamage(Entity? attacker, Entity victim, int damage) => OnDamage?.Invoke(attacker, victim, damage);
    public void RaiseHealHp(Entity? healer, Entity target, int amount) => OnHealHp?.Invoke(healer, target, amount);
    public void RaiseHealMp(Entity? healer, Entity target, int amount) => OnHealMp?.Invoke(healer, target, amount);
    public void RaiseDeath(Entity deadEntity, Entity? killer = null) => OnDeath?.Invoke(deadEntity, killer);
    
    public void RaiseCombatEnter(Entity entity) => OnCombatEnter?.Invoke(entity);
    public void RaiseCombatExit(Entity entity) => OnCombatExit?.Invoke(entity);
    
    public void RaisePositionChanged(Entity entity, int x, int y, int z) => OnPositionChanged?.Invoke(entity, x, y, z);
    public void RaiseFacingChanged(Entity entity, int dx, int dy) => OnFacingChanged?.Invoke(entity, dx, dy);
}

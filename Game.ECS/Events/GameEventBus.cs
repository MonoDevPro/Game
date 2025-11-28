using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Events;

/// <summary>
/// Simple in-memory event bus for game events.
/// Provides a centralized way for systems to communicate without direct coupling.
/// </summary>
public sealed class GameEventBus
{
    // Event queues for deferred processing
    private readonly List<DamageEvent> _damageEvents = new();
    private readonly List<DeathEvent> _deathEvents = new();
    private readonly List<SpawnEvent> _spawnEvents = new();
    private readonly List<DespawnEvent> _despawnEvents = new();
    private readonly List<AttackEvent> _attackEvents = new();
    
    // Event handlers
    public event Action<DamageEvent>? OnDamage;
    public event Action<DeathEvent>? OnDeath;
    public event Action<SpawnEvent>? OnSpawn;
    public event Action<DespawnEvent>? OnDespawn;
    public event Action<AttackEvent>? OnAttack;
    public event Action<HealthChangedEvent>? OnHealthChanged;
    public event Action<ManaChangedEvent>? OnManaChanged;
    public event Action<MovementEvent>? OnMovement;
    public event Action<NpcStateChangedEvent>? OnNpcStateChanged;

    #region Send Events (Immediate)
    
    /// <summary>
    /// Sends a damage event immediately to all handlers.
    /// </summary>
    public void Send(in DamageEvent evt) => OnDamage?.Invoke(evt);
    
    /// <summary>
    /// Sends a death event immediately to all handlers.
    /// </summary>
    public void Send(in DeathEvent evt) => OnDeath?.Invoke(evt);
    
    /// <summary>
    /// Sends a spawn event immediately to all handlers.
    /// </summary>
    public void Send(in SpawnEvent evt) => OnSpawn?.Invoke(evt);
    
    /// <summary>
    /// Sends a despawn event immediately to all handlers.
    /// </summary>
    public void Send(in DespawnEvent evt) => OnDespawn?.Invoke(evt);
    
    /// <summary>
    /// Sends an attack event immediately to all handlers.
    /// </summary>
    public void Send(in AttackEvent evt) => OnAttack?.Invoke(evt);
    
    /// <summary>
    /// Sends a health changed event immediately to all handlers.
    /// </summary>
    public void Send(in HealthChangedEvent evt) => OnHealthChanged?.Invoke(evt);
    
    /// <summary>
    /// Sends a mana changed event immediately to all handlers.
    /// </summary>
    public void Send(in ManaChangedEvent evt) => OnManaChanged?.Invoke(evt);
    
    /// <summary>
    /// Sends a movement event immediately to all handlers.
    /// </summary>
    public void Send(in MovementEvent evt) => OnMovement?.Invoke(evt);
    
    /// <summary>
    /// Sends an NPC state changed event immediately to all handlers.
    /// </summary>
    public void Send(in NpcStateChangedEvent evt) => OnNpcStateChanged?.Invoke(evt);
    
    #endregion
    
    #region Queue Events (Deferred)
    
    /// <summary>
    /// Queues a damage event for deferred processing.
    /// </summary>
    public void Queue(in DamageEvent evt) => _damageEvents.Add(evt);
    
    /// <summary>
    /// Queues a death event for deferred processing.
    /// </summary>
    public void Queue(in DeathEvent evt) => _deathEvents.Add(evt);
    
    /// <summary>
    /// Queues a spawn event for deferred processing.
    /// </summary>
    public void Queue(in SpawnEvent evt) => _spawnEvents.Add(evt);
    
    /// <summary>
    /// Queues a despawn event for deferred processing.
    /// </summary>
    public void Queue(in DespawnEvent evt) => _despawnEvents.Add(evt);
    
    /// <summary>
    /// Queues an attack event for deferred processing.
    /// </summary>
    public void Queue(in AttackEvent evt) => _attackEvents.Add(evt);
    
    #endregion
    
    #region Process Queued Events
    
    /// <summary>
    /// Processes all queued events and clears the queues.
    /// Call this once per frame/tick.
    /// </summary>
    public void ProcessQueuedEvents()
    {
        // Process damage events
        foreach (var evt in _damageEvents)
            OnDamage?.Invoke(evt);
        _damageEvents.Clear();
        
        // Process death events
        foreach (var evt in _deathEvents)
            OnDeath?.Invoke(evt);
        _deathEvents.Clear();
        
        // Process spawn events
        foreach (var evt in _spawnEvents)
            OnSpawn?.Invoke(evt);
        _spawnEvents.Clear();
        
        // Process despawn events
        foreach (var evt in _despawnEvents)
            OnDespawn?.Invoke(evt);
        _despawnEvents.Clear();
        
        // Process attack events
        foreach (var evt in _attackEvents)
            OnAttack?.Invoke(evt);
        _attackEvents.Clear();
    }
    
    /// <summary>
    /// Clears all queued events without processing them.
    /// </summary>
    public void ClearQueues()
    {
        _damageEvents.Clear();
        _deathEvents.Clear();
        _spawnEvents.Clear();
        _despawnEvents.Clear();
        _attackEvents.Clear();
    }
    
    #endregion
    
    #region Helpers
    
    /// <summary>
    /// Returns the number of queued events across all types.
    /// </summary>
    public int QueuedEventCount => 
        _damageEvents.Count + 
        _deathEvents.Count + 
        _spawnEvents.Count + 
        _despawnEvents.Count + 
        _attackEvents.Count;
    
    #endregion
}

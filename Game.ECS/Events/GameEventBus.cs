using Arch.Bus;
using Arch.LowLevel;

namespace Game.ECS.Events;

/// <summary>
/// Simple in-memory event bus for game events.
/// Provides a centralized way for systems to communicate without direct coupling.
/// </summary>
public sealed partial class GameEventBus : IDisposable
{
    public GameEventBus() { Hook(); }
    ~GameEventBus() { Dispose(); }

    // Event queues for deferred processing
    private const int InitialQueueCapacity = 10;
    private UnsafeQueue<DamageEvent> _unsafeDamageEvents = new(capacity: InitialQueueCapacity);
    private UnsafeQueue<DeathEvent> _unsafeDeathEvents = new(capacity: InitialQueueCapacity);
    private UnsafeQueue<SpawnEvent> _unsafeSpawnEvents = new(capacity: InitialQueueCapacity);
    private UnsafeQueue<DespawnEvent> _unsafeDespawnEvents = new(capacity: InitialQueueCapacity);
    private UnsafeQueue<AttackEvent> _unsafeAttackEvents = new(capacity: InitialQueueCapacity);
    
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
    [Event]
    public void Send(ref DamageEvent evt) => OnDamage?.Invoke(evt);
    
    /// <summary>
    /// Sends a death event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref DeathEvent evt) => OnDeath?.Invoke(evt);
    
    /// <summary>
    /// Sends a spawn event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref SpawnEvent evt) => OnSpawn?.Invoke(evt);
    
    /// <summary>
    /// Sends a despawn event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref DespawnEvent evt) => OnDespawn?.Invoke(evt);
    
    /// <summary>
    /// Sends an attack event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref AttackEvent evt) => OnAttack?.Invoke(evt);
    
    /// <summary>
    /// Sends a health changed event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref HealthChangedEvent evt) => OnHealthChanged?.Invoke(evt);
    
    /// <summary>
    /// Sends a mana changed event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref ManaChangedEvent evt) => OnManaChanged?.Invoke(evt);
    
    /// <summary>
    /// Sends a movement event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref MovementEvent evt) => OnMovement?.Invoke(evt);
    
    /// <summary>
    /// Sends an NPC state changed event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref NpcStateChangedEvent evt) => OnNpcStateChanged?.Invoke(evt);
    
    #endregion
    
    #region Queue Events (Deferred)

    /// <summary>
    /// Queues a damage event for deferred processing.
    /// </summary>
    [Event]
    public void Queue(ref DamageEvent evt) => _unsafeDamageEvents.Enqueue(evt);
    
    /// <summary>
    /// Queues a death event for deferred processing.
    /// </summary>
    [Event]
    public void Queue(ref DeathEvent evt) => _unsafeDeathEvents.Enqueue(evt);
    
    /// <summary>
    /// Queues a spawn event for deferred processing.
    /// </summary>
    [Event]
    public void Queue(ref SpawnEvent evt) => _unsafeSpawnEvents.Enqueue(evt);
    
    /// <summary>
    /// Queues a despawn event for deferred processing.
    /// </summary>
    [Event]
    public void Queue(ref DespawnEvent evt) => _unsafeDespawnEvents.Enqueue(evt);
    
    /// <summary>
    /// Queues an attack event for deferred processing.
    /// </summary>
    [Event]
    public void Queue(ref AttackEvent evt) => _unsafeAttackEvents.Enqueue(evt);
    
    #endregion
    
    #region Process Queued Events
    
    /// <summary>
    /// Processes all queued events and clears the queues.
    /// Call this once per frame/tick.
    /// </summary>
    public void ProcessQueuedEvents()
    {
        // Process damage events
        
        foreach (ref var evt in _unsafeDamageEvents)
            OnDamage?.Invoke(evt);
        _unsafeDamageEvents.Clear();
        
        // Process death events
        foreach (ref var evt in _unsafeDeathEvents)
            OnDeath?.Invoke(evt);
        _unsafeDeathEvents.Clear();
        
        // Process spawn events
        foreach (ref var evt in _unsafeSpawnEvents)
            OnSpawn?.Invoke(evt);
        _unsafeSpawnEvents.Clear();
        
        // Process despawn events
        foreach (ref var evt in _unsafeDespawnEvents)
            OnDespawn?.Invoke(evt);
        _unsafeDespawnEvents.Clear();
        
        // Process attack events
        foreach (ref var evt in _unsafeAttackEvents)
            OnAttack?.Invoke(evt);
        _unsafeAttackEvents.Clear();
    }
    
    /// <summary>
    /// Clears all queued events without processing them.
    /// </summary>
    public void ClearQueues()
    {
        _unsafeDamageEvents.Clear();
        _unsafeDeathEvents.Clear();
        _unsafeSpawnEvents.Clear();
        _unsafeDespawnEvents.Clear();
        _unsafeAttackEvents.Clear();
    }
    
    #endregion
    
    #region Helpers
    
    /// <summary>
    /// Returns the number of queued events across all types.
    /// </summary>
    public int QueuedEventCount => 
        _unsafeDamageEvents.Count + 
        _unsafeDeathEvents.Count + 
        _unsafeSpawnEvents.Count + 
        _unsafeDespawnEvents.Count + 
        _unsafeAttackEvents.Count;
    
    #endregion

    private void ReleaseUnmanagedResources()
    {
        Unhook();
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            _unsafeDamageEvents.Dispose();
            _unsafeDeathEvents.Dispose();
            _unsafeSpawnEvents.Dispose();
            _unsafeDespawnEvents.Dispose();
            _unsafeAttackEvents.Dispose();
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

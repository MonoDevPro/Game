using Arch.Bus;

namespace Game.ECS.Events;

/// <summary>
/// Simple in-memory event bus for game events.
/// Provides a centralized way for systems to communicate without direct coupling.
/// </summary>
public sealed partial class GameEventBus : IDisposable
{
    public GameEventBus() { Hook(); }
    ~GameEventBus() { Dispose(); }

    // Event handlers
    public event Action<DamageEvent>? OnDamage;
    public event Action<DeathEvent>? OnDeath;
    public event Action<SpawnEvent>? OnSpawn;
    public event Action<DespawnEvent>? OnDespawn;
    public event Action<AttackEvent>? OnAttack;
    public event Action<HealthChangedEvent>? OnHealthChanged;
    public event Action<ManaChangedEvent>? OnManaChanged;
    public event Action<MovementEvent>? OnMovement;
    public event Action<DirectionChangedEvent>? OnDirectionChanged; 
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
    /// Sends a direction changed event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref DirectionChangedEvent evt) => OnDirectionChanged?.Invoke(evt);
    
    /// <summary>
    /// Sends an NPC state changed event immediately to all handlers.
    /// </summary>
    [Event]
    public void Send(ref NpcStateChangedEvent evt) => OnNpcStateChanged?.Invoke(evt);
    
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
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

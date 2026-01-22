using Arch.Bus;

namespace Game.ECS.Events;

/// <summary>
/// Simple in-memory event bus for game events.
/// Provides a centralized way for systems to communicate without direct coupling.
/// </summary>
public sealed partial class GameEventBus : IDisposable
{
    /// Event handlers -> Lifecycle Events
    public event Action<SpawnEvent>? OnSpawn;
    public event Action<DespawnEvent>? OnDespawn;
    [Event] public void Send(ref SpawnEvent evt) => OnSpawn?.Invoke(evt);
    [Event] public void Send(ref DespawnEvent evt) => OnDespawn?.Invoke(evt);
    
    /// Event handlers -> Combat Events
    public event Action<DeathEvent>? OnDeath;
    public event Action<AttackEvent>? OnAttack;
    public event Action<DamageEvent>? OnDamage;
    [Event] public void Send(ref DeathEvent evt) => OnDeath?.Invoke(evt); 
    [Event] public void Send(ref AttackEvent evt) => OnAttack?.Invoke(evt);
    [Event] public void Send(ref DamageEvent evt) => OnDamage?.Invoke(evt);
    
    /// Event handlers -> State Change Events
    public event Action<HealthChangedEvent>? OnHealthChanged;
    public event Action<ManaChangedEvent>? OnManaChanged;
    public event Action<MovementEvent>? OnMovement;
    public event Action<DirectionChangedEvent>? OnDirectionChanged; 
    [Event] public void Send(ref HealthChangedEvent evt) => OnHealthChanged?.Invoke(evt);
    [Event] public void Send(ref ManaChangedEvent evt) => OnManaChanged?.Invoke(evt);
    [Event] public void Send(ref MovementEvent evt) => OnMovement?.Invoke(evt);
    [Event] public void Send(ref DirectionChangedEvent evt) => OnDirectionChanged?.Invoke(evt);
    
    public GameEventBus() { Hook(); }
    ~GameEventBus() { Dispose(); }
    
    private void ReleaseUnmanagedResources()
    {
        Unhook();
    }

    private void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing) { }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

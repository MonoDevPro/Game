using Arch.Bus;

namespace Game.Infrastructure.ArchECS.Services.Events;

/// <summary>
/// Simple in-memory event bus for game events.
/// Provides a centralized way for systems to communicate without direct coupling.
/// </summary>
public sealed partial class GameEventBus : IDisposable
{
    public event Action<SpawnEvent>? OnSpawn;
    public event Action<DespawnEvent>? OnDespawn;
    public event Action<MoveEvent>? OnMove;
    public event Action<AttackStartedEvent>? OnAttackStarted;
    public event Action<ProjectileSpawnedEvent>? OnProjectileSpawned;
    public event Action<CombatDamageEvent>? OnCombatDamageEvent;
    
    [Event] public void Send(ref SpawnEvent evt) => OnSpawn?.Invoke(evt);
    [Event] public void Send(ref DespawnEvent evt) => OnDespawn?.Invoke(evt);
    [Event] public void Send(ref MoveEvent evt) => OnMove?.Invoke(evt);
    [Event] public void Send(ref AttackStartedEvent evt) => OnAttackStarted?.Invoke(evt);
    [Event] public void Send(ref ProjectileSpawnedEvent evt) => OnProjectileSpawned?.Invoke(evt);
    [Event] public void Send(ref CombatDamageEvent evt) => OnCombatDamageEvent?.Invoke(evt);
    
    public GameEventBus() { Hook(); }
    ~GameEventBus() { Dispose(); }
    private void ReleaseUnmanagedResources() { Unhook(); }
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

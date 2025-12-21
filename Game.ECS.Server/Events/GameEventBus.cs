using Arch.Bus;

namespace Game.ECS.Server.Events;

public readonly record struct EventExample(int ExampleId);

/// <summary>
/// Simple in-memory event bus for game events.
/// Provides a centralized way for systems to communicate without direct coupling.
/// </summary>
public sealed partial class GameEventBus : IDisposable
{
    /// Event handlers -> Lifecycle Events
    public event Action<EventExample>? OnEventExample;
    [Event] public void Send(ref EventExample evt) => OnEventExample?.Invoke(evt);
    
    
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

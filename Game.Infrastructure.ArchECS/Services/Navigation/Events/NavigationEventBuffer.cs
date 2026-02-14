using Arch.Bus;
using Arch.Core;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.Navigation.Events;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS.Services.Events;

/// <summary>
/// Simple in-memory event bus for game events.
/// Provides a centralized way for systems to communicate without direct coupling.
/// </summary>
public sealed partial class NavigationEventBuffer : GameSystem
{
    private readonly List<MoveEvent> _events;
    
    [Event] public void Send(ref MoveEvent evt) => _events.Add(evt);

    public bool TryDrain(out List<MoveEvent> events)
    {
        if (_events.Count == 0)
        {
            events = [];
            return false;
        }

        events = new List<MoveEvent>(_events);
        _events.Clear();
        return true;
    }

    public NavigationEventBuffer(World world, ILogger? logger = null) : base(world, logger) { Hook(); }
    public override void Dispose() { base.Dispose(); Unhook(); _events.Clear(); }
}

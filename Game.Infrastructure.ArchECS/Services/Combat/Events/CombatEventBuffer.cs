using Arch.Bus;
using Arch.Core;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS.Services.Combat.Events;

/// <summary>
/// Buffer simples de eventos de combate por tick.
/// </summary>
public sealed partial class CombatEventBuffer : GameSystem
{
    private readonly List<CombatEvent> _events = [];
    
    [Event] public void Send(ref CombatEvent evt) => _events.Add(evt);

    public bool TryDrain(List<CombatEvent> buffer)
    {
        buffer.Clear();

        if (_events.Count == 0)
            return false;

        buffer.AddRange(_events);
        _events.Clear();
        return true;
    }

    public CombatEventBuffer(World world, ILogger? logger = null) : base(world, logger) { Hook(); }
    public override void Dispose() { base.Dispose(); Unhook(); _events.Clear(); }
}

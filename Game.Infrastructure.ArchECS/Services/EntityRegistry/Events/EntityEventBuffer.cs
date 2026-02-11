using Arch.Bus;
using Arch.Core;
using Game.Infrastructure.ArchECS.Commons;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS.Services.EntityRegistry.Events;

public sealed partial class EntityEventBuffer : GameSystem
{
    private Arch.LowLevel.UnsafeQueue<EntityRegisteredEvent> _spawnEvents = new(10);
    private Arch.LowLevel.UnsafeQueue<EntityUnregisteredEvent> _despawnEvents = new(10);
    
    [Event] public void Send(ref EntityRegisteredEvent evt) => _spawnEvents.Enqueue(evt);
    [Event] public void Send(ref EntityUnregisteredEvent evt) => _despawnEvents.Enqueue(evt);

    public bool TryDrain(out Span<EntityRegisteredEvent> registerEvents, out Span<EntityUnregisteredEvent> unregisterEvents)
    {
        if (_spawnEvents.Count == 0 && _despawnEvents.Count == 0)
        {
            registerEvents = Span<EntityRegisteredEvent>.Empty;
            unregisterEvents = Span<EntityUnregisteredEvent>.Empty;
            return false;
        }

        registerEvents = _spawnEvents.AsSpan();
        unregisterEvents = _despawnEvents.AsSpan();
        return true;
    }

    public EntityEventBuffer(World world, ILogger? logger = null) : base(world, logger) { Hook(); }
    public override void Dispose() { base.Dispose(); Unhook(); _spawnEvents.Clear(); _despawnEvents.Clear(); }
}

using Arch.Bus;
using Arch.Core;
using Game.Contracts;
using Game.Infrastructure.ArchECS.Commons;
using Game.Infrastructure.ArchECS.Services.EntityRegistry.Components;
using Game.Infrastructure.ArchECS.Services.Combat.Events;
using Microsoft.Extensions.Logging;

namespace Game.Infrastructure.ArchECS.Services.Combat;

/// <summary>
/// Buffer simples de eventos de combate por tick.
/// </summary>
public sealed partial class CombatEventBuffer : GameSystem
{
    private readonly List<CombatEvent> _events = [];
    
    [Event] public void Send(ref AttackStartedEvent evt)
    {
        _events.Add(new CombatEvent(
            Type: CombatEventType.AttackStarted,
            AttackerId: ResolveNetworkId(evt.Attacker),
            TargetId: 0,
            Damage: 0,
            DirX: evt.DirX,
            DirY: evt.DirY,
            X: evt.PosX,
            Y: evt.PosY,
            Floor: evt.Floor,
            Speed: 0f,
            Range: 0));
    }

    [Event] public void Send(ref ProjectileSpawnedEvent evt)
    {
        _events.Add(new CombatEvent(
            Type: CombatEventType.ProjectileSpawn,
            AttackerId: ResolveNetworkId(evt.Attacker),
            TargetId: 0,
            Damage: evt.Damage,
            DirX: evt.DirX,
            DirY: evt.DirY,
            X: evt.PosX,
            Y: evt.PosY,
            Floor: evt.Floor,
            Speed: evt.Speed,
            Range: evt.Range));
    }

    [Event] public void Send(ref CombatDamageEvent evt)
    {
        _events.Add(new CombatEvent(
            Type: CombatEventType.Hit,
            AttackerId: ResolveNetworkId(evt.Attacker),
            TargetId: ResolveNetworkId(evt.Target),
            Damage: evt.Damage,
            DirX: evt.DirX,
            DirY: evt.DirY,
            X: evt.PosX,
            Y: evt.PosY,
            Floor: evt.Floor,
            Speed: 0f,
            Range: 0));
    }

    private int ResolveNetworkId(Entity entity)
    {
        if (entity != Entity.Null && World.Has<CharacterId>(entity))
        {
            return World.Get<CharacterId>(entity).Value;
        }

        return entity.Id;
    }

    public bool TryDrain(out List<CombatEvent> events)
    {
        if (_events.Count == 0)
        {
            events = [];
            return false;
        }
        
        events = new List<CombatEvent>(_events);
        _events.Clear();
        return true;
    }

    public CombatEventBuffer(World world, ILogger? logger = null) : base(world, logger) { Hook(); }
    public override void Dispose() { base.Dispose(); Unhook(); _events.Clear(); }
}

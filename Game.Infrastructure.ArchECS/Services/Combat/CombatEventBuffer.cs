using Game.Contracts;

namespace Game.Infrastructure.ArchECS.Services.Combat;

/// <summary>
/// Buffer simples de eventos de combate por tick.
/// </summary>
public sealed class CombatEventBuffer
{
    private readonly List<CombatEvent> _events = new();

    public void Add(in CombatEvent combatEvent)
    {
        _events.Add(combatEvent);
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

    public void Clear()
    {
        _events.Clear();
    }
}

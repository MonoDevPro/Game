namespace Game.Infrastructure.ArchECS.Services.Combat;

public readonly record struct CombatVitalUpdate(int CharacterId, int CurrentHealth, int CurrentMana);

/// <summary>
/// Buffer para persistência de HP/MP (dedup por personagem).
/// </summary>
public sealed class CombatVitalsBuffer
{
    private readonly Dictionary<int, CombatVitalUpdate> _updates = new();

    public void MarkDirty(int characterId, int currentHealth, int currentMana)
    {
        if (characterId <= 0)
            return;

        _updates[characterId] = new CombatVitalUpdate(characterId, currentHealth, currentMana);
    }

    public bool TryDrain(out List<CombatVitalUpdate> updates)
    {
        if (_updates.Count == 0)
        {
            updates = [];
            return false;
        }

        updates = new List<CombatVitalUpdate>(_updates.Values);
        _updates.Clear();
        return true;
    }

    public void Clear()
    {
        _updates.Clear();
    }
}

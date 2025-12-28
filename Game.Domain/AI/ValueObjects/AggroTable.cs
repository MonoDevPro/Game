namespace Game.Domain.AI.ValueObjects;

/// <summary>
/// Tabela de aggro (threat).
/// </summary>
public unsafe struct AggroTable
{
    public const int MaxEntries = 16;

    private fixed int _entityIds[MaxEntries];
    private fixed int _threats[MaxEntries];
    public byte Count;

    public readonly bool HasThreat => Count > 0;

    public void AddThreat(int entityId, int amount)
    {
        // Procura existente
        for (int i = 0; i < Count; i++)
        {
            if (_entityIds[i] == entityId)
            {
                _threats[i] += amount;
                return;
            }
        }

        // Adiciona novo se tem espaço
        if (Count < MaxEntries)
        {
            _entityIds[Count] = entityId;
            _threats[Count] = amount;
            Count++;
        }
    }

    public void RemoveThreat(int entityId, int amount)
    {
        for (int i = 0; i < Count; i++)
        {
            if (_entityIds[i] == entityId)
            {
                _threats[i] = Math.Max(0, _threats[i] - amount);
                return;
            }
        }
    }

    public readonly int GetThreat(int entityId)
    {
        for (int i = 0; i < Count; i++)
        {
            if (_entityIds[i] == entityId)
                return _threats[i];
        }
        return 0;
    }

    public readonly bool TryGetHighestThreat(out int entityId, out int threat)
    {
        entityId = 0;
        threat = 0;

        for (int i = 0; i < Count; i++)
        {
            if (_threats[i] > threat)
            {
                entityId = _entityIds[i];
                threat = _threats[i];
            }
        }
        return threat > 0;
    }

    public void Clear() => Count = 0;

    public void DecayThreat(float percentage)
    {
        for (int i = 0; i < Count; i++)
        {
            _threats[i] = (int)(_threats[i] * (1f - percentage));
        }
    }
}
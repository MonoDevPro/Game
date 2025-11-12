namespace Game.ECS.Components;

// ============================================
// Network Sync - Flags de sujidade
// ============================================
[Flags]
public enum DirtyComponentType : ushort
{
    None          = 0,
    Position      = 1 << 0,
    Health        = 1 << 1,
    Mana          = 1 << 2,
    Facing        = 1 << 3,
    Input         = 1 << 4,
    Velocity      = 1 << 5,
    CombatState   = 1 << 6,
	
    All	     = Position | Health | Mana | Facing | Input | Velocity | CombatState
}

public struct DirtyFlags
{
    private ushort _flags;

    public bool IsEmpty => _flags == 0;
    public ushort Raw => _flags;

    public void MarkDirty(DirtyComponentType type) => _flags |= (ushort)type;
    public void MarkDirtyMask(DirtyComponentType mask) => _flags |= (ushort)mask;

    public void ClearDirty(DirtyComponentType type) => _flags &= (ushort)~(ushort)type;
    public void ClearDirtyMask(DirtyComponentType mask) => _flags &= (ushort)~(ushort)mask;

    public bool IsDirty(DirtyComponentType type) => (_flags & (ushort)type) != 0;

    public DirtyComponentType Snapshot() => (DirtyComponentType)_flags;

    // Consome (lê + limpa todos os bits) — útil quando você quer tentar enviar tudo e só limpar se usar ConsumeSnapshot.
    public DirtyComponentType ConsumeSnapshot()
    {
        var s = (DirtyComponentType)_flags;
        _flags = 0;
        return s;
    }
    public void ClearAll() => _flags = 0;
    public override string ToString() => ((DirtyComponentType)_flags).ToString();
}

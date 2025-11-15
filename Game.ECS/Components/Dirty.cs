namespace Game.ECS.Components;

// ============================================
// Network Sync - Flags de sujidade
// ============================================
[Flags]
public enum DirtyComponentType : ushort
{
    None          = 0,
    State         = 1 << 0,
    Vitals        = 1 << 1,
    Input         = 1 << 2,
    Combat        = 1 << 3,
	
    All	     = State | Vitals | Input | Combat
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
    public readonly bool IsDirtyMask(DirtyComponentType mask) => (_flags & (ushort)mask) != 0;

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

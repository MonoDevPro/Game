using MemoryPack;

namespace GameECS.Modules.Navigation.Shared.Data;

/// <summary>
/// Input de movimento enviado pelo cliente.
/// </summary>
[MemoryPackable]
public partial struct MoveInputData
{
    public int TargetX;
    public int TargetY;
}

/// <summary>
/// Snapshot de movimento para sincronizacao.
/// </summary>
[MemoryPackable]
public partial struct MovementSnapshot
{
    public int NetworkId;
    public int X;
    public int Y;
    public int TargetX;
    public int TargetY;
    public byte Direction;
    public bool IsMoving;
    public long StartTick;
    public long EndTick;
}

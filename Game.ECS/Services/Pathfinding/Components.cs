using System.Runtime. CompilerServices;
using System.Runtime.InteropServices;
using Game.ECS.Components;

namespace Game. ECS.Services.Pathfinding;

public struct PathfindingRequest
{
    public int StartX;
    public int StartY;
    public int GoalX;
    public int GoalY;
    public int MaxSearchNodes;
    public PathfindingStatus Status;
}

public enum PathfindingStatus :  byte
{
    Pending,
    InProgress,
    Completed,
    Failed,
    Cancelled
}

/// <summary>
/// Nó do A* com generation counter para invalidação rápida
/// </summary>
public struct PathNode :  IEquatable<PathNode>
{
    public int X;
    public int Y;
    public float GCost;
    public float HCost;
    public int ParentIndex;
    public int Generation;  // ← NOVO: Marca qual "sessão" de busca este nó pertence

    public float FCost
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GCost + HCost;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsValidForGeneration(int currentGeneration) => Generation == currentGeneration;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(PathNode other) => X == other.X && Y == other.Y;

    public override int GetHashCode() => X * 31 + Y;
}

public struct PathResult
{
    public int PathLength;
    public bool IsValid;
}

public unsafe struct PositionsUnmanagedComponent
{
    public IntPtr Ptr;   // ponteiro para bloco de Position
    public int Length;

    public void Init(int length)
    {
        if (length <= 0) throw new ArgumentOutOfRangeException(nameof(length));
        Length = length;
        // sizeof(Position) funciona porque Position é um tipo unmanaged
        int bytes = sizeof(Position) * length;
        Ptr = Marshal.AllocHGlobal(bytes);
        // opcional: zera a memória
        Unsafe.InitBlockUnaligned((void*)Ptr, 0, (uint)bytes);
    }

    public Span<Position> AsSpan()
    {
        if (Ptr == IntPtr.Zero || Length == 0) return Span<Position>.Empty;
        return new Span<Position>((void*)Ptr, Length);
    }

    public void Free()
    {
        if (Ptr != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(Ptr);
            Ptr = IntPtr.Zero;
            Length = 0;
        }
    }
}

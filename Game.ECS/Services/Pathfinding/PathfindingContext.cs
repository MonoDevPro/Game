using System.Runtime.CompilerServices;

namespace Game.ECS.Services.Pathfinding;

/// <summary>
/// Contexto reutilizável para uma operação de pathfinding
/// Usa "generation counter" para invalidar nós sem limpar array
/// </summary>
public sealed class PathfindingContext
{
    public PathNode[] Nodes { get; }
    public int[] Path { get; }
    public int NodeCapacity { get; }
    public int PathCapacity { get; }
    
    // Binary Heap inline para OpenList
    public int[] OpenHeap { get; }
    public int OpenCount;
    
    // BitArray para ClosedList
    public ulong[] ClosedBits { get; }
    
    // Generation counter - evita limpar arrays enormes
    public int Generation { get; private set; }

    public PathfindingContext(PathNode[] nodes, int[] path, int nodeCapacity, int pathCapacity)
    {
        Nodes = nodes;
        Path = path;
        NodeCapacity = nodeCapacity;
        PathCapacity = pathCapacity;
        OpenHeap = new int[nodeCapacity];
        ClosedBits = new ulong[(nodeCapacity + 63) / 64];
        OpenCount = 0;
        Generation = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        OpenCount = 0;
        Generation++; // Incrementa geração - invalida todos os nós anteriores
        Array.Clear(ClosedBits, 0, ClosedBits.Length);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void MarkClosed(int index)
    {
        ClosedBits[index >> 6] |= 1UL << (index & 63);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsClosed(int index)
    {
        return (ClosedBits[index >> 6] & (1UL << (index & 63))) != 0;
    }
}
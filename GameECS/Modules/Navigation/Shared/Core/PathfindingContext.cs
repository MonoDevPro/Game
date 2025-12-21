using System.Runtime.CompilerServices;

namespace GameECS.Modules.Navigation.Shared.Core;

/// <summary>
/// Contexto reutilizável para operações de pathfinding.
/// Elimina alocações durante runtime.
/// </summary>
public sealed class PathfindingContext
{
    public PathNode[] Nodes { get; }
    public int NodeCapacity { get; }
    
    // Binary Heap para Open List
    public int[] OpenHeap { get; }
    public int OpenCount;
    
    // BitArray para Closed List
    public ulong[] ClosedBits { get; }
    
    // Generation counter para invalidação rápida
    public int Generation { get; private set; }
    
    // Buffer temporário para reconstrução de caminho
    public int[] TempPath { get; }
    public int TempPathCapacity { get; }

    public PathfindingContext(int nodeCapacity, int pathCapacity = 256)
    {
        NodeCapacity = nodeCapacity;
        Nodes = new PathNode[nodeCapacity];
        OpenHeap = new int[nodeCapacity];
        ClosedBits = new ulong[(nodeCapacity + 63) / 64];
        TempPath = new int[pathCapacity];
        TempPathCapacity = pathCapacity;
        OpenCount = 0;
        Generation = 0;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Reset()
    {
        OpenCount = 0;
        Generation++;
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
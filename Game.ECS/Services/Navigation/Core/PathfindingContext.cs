using System.Runtime.CompilerServices;
using Game.ECS.Navigation.Components;

namespace Game.ECS.Navigation.Core;

/// <summary>
/// Contexto reutilizável para uma operação de pathfinding
/// Usa "generation counter" para invalidar nós sem limpar array
/// </summary>
public sealed class PathfindingContext(PathNode[] nodes, int[] path, int nodeCapacity, int pathCapacity)
{
    public PathNode[] Nodes { get; } = nodes;
    public int[] Path { get; } = path;
    public int NodeCapacity { get; } = nodeCapacity;
    public int PathCapacity { get; } = pathCapacity;

    // Binary Heap inline para OpenList
    public int[] OpenHeap { get; } = new int[nodeCapacity];
    public int OpenCount = 0;
    
    // BitArray para ClosedList
    public ulong[] ClosedBits { get; } = new ulong[(nodeCapacity + 63) / 64];

    // Generation counter - evita limpar arrays enormes
    public int Generation { get; private set; } = 0;

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
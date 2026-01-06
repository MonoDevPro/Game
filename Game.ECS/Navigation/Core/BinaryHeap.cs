using System.Runtime.CompilerServices;
using Game.ECS.Navigation.Components;

namespace Game.ECS.Navigation.Core;

/// <summary>
/// Binary Heap inline otimizado para A* - zero alocação
/// </summary>
public static class BinaryHeap
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Push(PathfindingContext ctx, int nodeIndex, PathNode[] nodes)
    {
        int i = ctx.OpenCount++;
        ctx.OpenHeap[i] = nodeIndex;
            
        // Bubble up
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (nodes[ctx.OpenHeap[i]].FCost >= nodes[ctx.OpenHeap[parent]].FCost)
                break;
                
            // Swap
            (ctx.OpenHeap[i], ctx.OpenHeap[parent]) = (ctx.OpenHeap[parent], ctx.OpenHeap[i]);
            i = parent;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Pop(PathfindingContext ctx, PathNode[] nodes)
    {
        int result = ctx.OpenHeap[0];
        ctx.OpenHeap[0] = ctx.OpenHeap[--ctx.OpenCount];
            
        // Bubble down
        int i = 0;
        while (true)
        {
            int left = (i << 1) + 1;
            int right = (i << 1) + 2;
            int smallest = i;

            if (left < ctx.OpenCount && 
                nodes[ctx.OpenHeap[left]].FCost < nodes[ctx.OpenHeap[smallest]].FCost)
                smallest = left;

            if (right < ctx.OpenCount && 
                nodes[ctx.OpenHeap[right]].FCost < nodes[ctx.OpenHeap[smallest]].FCost)
                smallest = right;

            if (smallest == i) break;

            (ctx.OpenHeap[i], ctx.OpenHeap[smallest]) = (ctx.OpenHeap[smallest], ctx.OpenHeap[i]);
            i = smallest;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdatePriority(PathfindingContext ctx, int heapIndex, PathNode[] nodes)
    {
        // Bubble up após atualização de custo
        int i = heapIndex;
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (nodes[ctx.OpenHeap[i]].FCost >= nodes[ctx.OpenHeap[parent]].FCost)
                break;
                
            (ctx.OpenHeap[i], ctx.OpenHeap[parent]) = (ctx.OpenHeap[parent], ctx.OpenHeap[i]);
            i = parent;
        }
    }
}
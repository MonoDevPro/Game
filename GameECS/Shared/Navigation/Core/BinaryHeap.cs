using System.Runtime.CompilerServices;

namespace GameECS.Shared.Navigation.Core;

/// <summary>
/// Binary Heap estático otimizado para A*.
/// </summary>
public static class BinaryHeap
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Push(PathfindingContext ctx, int nodeIndex)
    {
        int i = ctx.OpenCount++;
        ctx.OpenHeap[i] = nodeIndex;
        
        // Bubble up
        while (i > 0)
        {
            int parent = (i - 1) >> 1;
            if (ctx.Nodes[ctx.OpenHeap[i]].FCost >= ctx.Nodes[ctx.OpenHeap[parent]].FCost)
                break;
            
            (ctx.OpenHeap[i], ctx.OpenHeap[parent]) = (ctx.OpenHeap[parent], ctx.OpenHeap[i]);
            i = parent;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int Pop(PathfindingContext ctx)
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
                ctx.Nodes[ctx.OpenHeap[left]].FCost < ctx.Nodes[ctx.OpenHeap[smallest]].FCost)
                smallest = left;

            if (right < ctx.OpenCount && 
                ctx.Nodes[ctx.OpenHeap[right]].FCost < ctx.Nodes[ctx.OpenHeap[smallest]].FCost)
                smallest = right;

            if (smallest == i) break;

            (ctx.OpenHeap[i], ctx.OpenHeap[smallest]) = (ctx.OpenHeap[smallest], ctx.OpenHeap[i]);
            i = smallest;
        }

        return result;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEmpty(PathfindingContext ctx) => ctx.OpenCount == 0;
}
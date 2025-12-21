using System.Runtime.CompilerServices;

namespace Game.ECS.Shared.Core.Navigation;

/// <summary>
/// Nó do A* otimizado para cache locality.
/// </summary>
public struct PathNode
{
    public int X;
    public int Y;
    public float GCost;
    public float HCost;
    public int ParentIndex;
    public int Generation;

    public float FCost
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => GCost + HCost;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public readonly bool IsValidForGeneration(int currentGeneration) 
        => Generation == currentGeneration;

    public override readonly int GetHashCode() => X * 31 + Y;
}
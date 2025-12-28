using System.Runtime.CompilerServices;

namespace Game.Domain.Navigation.ValueObjects;

/// <summary>
/// Nó do A* otimizado para cache locality.
/// </summary>
public struct PathNode : IEquatable<PathNode>
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

    public readonly override int GetHashCode() => X * 31 + Y;

    public bool Equals(PathNode other)
    {
        return X == other.X && Y == other.Y && GCost.Equals(other.GCost) && HCost.Equals(other.HCost) && ParentIndex == other.ParentIndex && Generation == other.Generation;
    }

    public override bool Equals(object? obj)
    {
        return obj is PathNode other && Equals(other);
    }
}
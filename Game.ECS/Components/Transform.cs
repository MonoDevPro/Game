// ============================================
// Transform - Posicionamento
// ============================================

namespace Game.ECS.Components;

public struct Position { public int X; public int Y; public int Z;

    /// <summary>
    /// Distância Manhattan (taxicab) em células.
    /// </summary>
    public readonly int ManhattanDistance(Position other)
        => Math.Abs(X - other.X) + Math.Abs(Y - other.Y);
}

public struct Velocity
{
    public int DirectionX; 
    public int DirectionY; 
    public float Speed;
    
    public bool IsMoving() => Speed > 0f && (DirectionX != 0 || DirectionY != 0);
    public void Stop() { Speed = 0f; DirectionX = 0; DirectionY = 0; }
}

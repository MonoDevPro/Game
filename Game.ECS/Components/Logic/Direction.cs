namespace Game.ECS.Components;

// ============================================
// Transform - Posicionamento
// ============================================
public partial struct Direction : IEquatable<Direction>
{
    public static Direction FromAngle(float angleInDegrees)
    {
        var radians = angleInDegrees * (MathF.PI / 180f);
        
        return new Direction
        {
            X = (sbyte)MathF.Round(MathF.Cos(radians)), 
            Y = (sbyte)MathF.Round(MathF.Sin(radians))
        };
    }

    public float ToAngleInDegrees() => MathF.Atan2(Y, X) * (180f / MathF.PI);

    public bool Equals(Direction other) => X == other.X && Y == other.Y;
    public override bool Equals(object? obj) => obj is Direction other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(X, Y);
}
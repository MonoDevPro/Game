namespace Game.Domain.VOs;

public readonly struct Vector2F(float x, float y)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public static Vector2F Zero => new(0, 0);
    public float MagnitudeSquared => X * X + Y * Y;
    public float Magnitude => MathF.Sqrt(MagnitudeSquared);
    public Vector2F Normalized => Magnitude > 1e-6f ? new Vector2F(X / Magnitude, Y / Magnitude) : new Vector2F(0, 0);
    public static Vector2F operator *(Vector2F v, float s) => new(v.X * s, v.Y * s);
    public static Vector2F operator +(Vector2F a, Vector2F b) => new(a.X + b.X, a.Y + b.Y);
    public static Vector2F operator -(Vector2F a, Vector2F b) => new(a.X - b.X, a.Y - b.Y);
}
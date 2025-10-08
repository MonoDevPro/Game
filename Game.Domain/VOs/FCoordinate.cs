namespace Game.Domain.VOs;

public readonly struct FCoordinate(float x, float y)
{
    public float X { get; } = x;
    public float Y { get; } = y;
    public static FCoordinate Zero => new(0, 0);
    public float MagnitudeSquared => X * X + Y * Y;
    public float Magnitude => MathF.Sqrt(MagnitudeSquared);
    public FCoordinate Normalized => Magnitude > 1e-6f ? new FCoordinate(X / Magnitude, Y / Magnitude) : new FCoordinate(0, 0);
    public static FCoordinate operator *(FCoordinate v, float s) => new(v.X * s, v.Y * s);
    public static FCoordinate operator +(FCoordinate a, FCoordinate b) => new(a.X + b.X, a.Y + b.Y);
    public static FCoordinate operator -(FCoordinate a, FCoordinate b) => new(a.X - b.X, a.Y - b.Y);
}
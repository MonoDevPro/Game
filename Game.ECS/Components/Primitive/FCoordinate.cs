using System.Runtime.InteropServices;

namespace Game.ECS.Components.Primitive;

// Estrutura auxiliar para representar coordenadas 2D em float
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct FCoordinate(float X, float Y)
{
    public static FCoordinate Zero => new(0, 0);
    public float MagnitudeSquared => X * X + Y * Y;
    public float Magnitude => MathF.Sqrt(MagnitudeSquared);
    public FCoordinate Normalized => Magnitude > 1e-6f ? new FCoordinate(X / Magnitude, Y / Magnitude) : new FCoordinate(0, 0);
    public static FCoordinate operator *(FCoordinate v, float s) => new(v.X * s, v.Y * s);
    public static FCoordinate operator +(FCoordinate a, FCoordinate b) => new(a.X + b.X, a.Y + b.Y);
    public static FCoordinate operator -(FCoordinate a, FCoordinate b) => new(a.X - b.X, a.Y - b.Y);
    
    public readonly float DistanceSquaredTo(FCoordinate to)
    {
        return (float) ((X - (double) to.X) * (X - (double) to.X) + (Y - (double) to.Y) * (Y - (double) to.Y));
    }
    
    public readonly float LengthSquared()
    {
        return (float) (X * (double) X + Y * (double) Y);
    }
}
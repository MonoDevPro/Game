namespace Game.ECS.Components;

public struct Position { public Coordinate Value; }
public struct Rotation { public ushort Value; }
public struct Velocity { public Speed Value; }
public struct Scale { public int Value; }

// Estrutura auxiliar para representar coordenadas 2D
public readonly record struct Coordinate(int X, int Y);
public readonly record struct Speed(int X, int Y);

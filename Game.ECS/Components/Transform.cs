using Game.Domain.Enums;
using Game.Domain.VOs;

namespace Game.ECS.Components;

public struct Position { public FCoordinate Value; }
public struct GridPosition { public Coordinate Value; }
public struct Direction { public Coordinate Value; }
public struct Velocity { public FCoordinate Value; }
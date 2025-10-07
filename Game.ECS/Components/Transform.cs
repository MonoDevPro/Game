using Game.Domain.Enums;
using Game.Domain.VOs;

namespace Game.ECS.Components;

public struct Position { public Coordinate Value; }
public struct Direction { public Coordinate Value; }
public struct Velocity { public Vector2F Value; }
public struct MoveAccumulator { public Vector2F Value; }
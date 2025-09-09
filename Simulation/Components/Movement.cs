namespace Simulation.Components;

public struct MoveIntent { public Direction Direction; }
public struct MoveStats { public float Speed; }
public struct MoveAction { public Position Start, Target; public float Elapsed, Duration; }
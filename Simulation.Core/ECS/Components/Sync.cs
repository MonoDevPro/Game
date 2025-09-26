namespace Simulation.Core.ECS.Components;

// ---> Flags <---
[Flags] public enum InputFlags : byte {None=0, Up=1<<0, Down=1<<1, Left=1<<2, Right=1<<3}
[Flags] public enum IntentFlags : byte {None=0, Move=1<<0, Attack=1<<1 }
[Flags] public enum StateFlags : byte {None=0, Idle=1<<0, Running=1<<1, Attacking=1<<2, Dead=1<<3 }

// ---> Synced Components <---
public readonly record struct Input(IntentFlags IntentState, InputFlags InputDir);
public readonly record struct State(StateFlags Value);
public readonly record struct Position(int X, int Y);
public readonly record struct Direction(int X, int Y);
public readonly record struct Health(int Current, int Max);
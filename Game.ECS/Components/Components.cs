using System.Runtime.InteropServices;

namespace Game.ECS.Components;

// Tags
public struct PlayerControlled;
public struct AIControlled;
public struct Dead;

// Identity
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct PlayerId { public int Value; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct NetworkId { public int Value; }

// Gameplay
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Health { public int Current; public int Max; public float RegenerationRate; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Mana { public int Current; public int Max; public float RegenerationRate; }

// Transforms
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Position { public int X; public int Y; public int Z; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Velocity { public int DirectionX; public int DirectionY; public float Speed; }

// Movement
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Walkable { public float BaseSpeed; public float CurrentModifier; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Facing { public int DirectionX; public int DirectionY; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Movement { public float Timer; }

// Combat
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AttackPower { public int Physical; public int Magical; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct Defense { public int Physical; public int Magical; }

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct CombatState { public bool InCombat; public uint TargetNetworkId; public float LastAttackTime; }
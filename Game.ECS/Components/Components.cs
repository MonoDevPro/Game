namespace Game.ECS.Components;

// Tags
public struct PlayerControlled;
public struct AIControlled;
public struct Dead;

// Identity
public struct PlayerId { public int Value; }

public struct NetworkId { public int Value; }

// Gameplay
public struct Health { public int Current; public int Max; public float RegenerationRate; }

public struct Mana { public int Current; public int Max; public float RegenerationRate; }

// Transforms
public struct Position { public int X; public int Y; public int Z; }

public struct Velocity { public int DirectionX; public int DirectionY; public float Speed; }

// Movement
public struct Walkable { public float BaseSpeed; public float CurrentModifier; }

public struct Facing { public int DirectionX; public int DirectionY; }

public struct Movement { public float Timer; }

// Combat
public struct AttackPower { public int Physical; public int Magical; }

public struct Defense { public int Physical; public int Magical; }

public struct CombatState { public bool InCombat; public uint TargetNetworkId; public float LastAttackTime; }
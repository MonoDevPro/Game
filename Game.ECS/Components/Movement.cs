namespace Game.ECS.Components;

// ============================================
// Movement - Movimento
// ============================================
public struct Walkable { public float BaseSpeed; public float CurrentModifier; }
public struct Facing { public sbyte DirectionX; public sbyte DirectionY; }
public struct Movement { public float Timer; } // Acumulador de movimento por célula
namespace Game.ECS.Components;

// ============================================
// Movement - Movimento
// ============================================
public struct Walkable { public float BaseSpeed; public float CurrentModifier; }
public struct Facing { public int DirectionX; public int DirectionY; }
public struct Movement { public float Timer; } // Acumulador de movimento por célula
namespace Game.ECS.Components;

// ============================================
// Movement - Movimento
// ============================================
public struct Walkable { public float BaseSpeed; public float CurrentModifier; }
public struct Direction { public sbyte X; public sbyte Y; }
public struct Movement { public float Timer; } // Acumulador de movimento por célula
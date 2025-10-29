namespace Game.ECS.Components;

// ============================================
// Status Effects - Efeitos de status
// ============================================
public struct Stun { public float RemainingTime; } // Atordoado
public struct Slow { public float RemainingTime; public float SpeedModifier; } // Desacelerado
public struct Poison { public float RemainingTime; public int DamagePerSecond; } // Envenenado
public struct Burning { public float RemainingTime; public int DamagePerSecond; } // Queimado
namespace Game.ECS.Components;

// ============================================
// Cooldowns - Tempos de espera
// ============================================
public struct AbilityCooldown { public float[] RemainingTimes; } // Array de cooldowns por habilidade
public struct ItemCooldown { public float RemainingTime; }
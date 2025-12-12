namespace Game.ECS.Components;

// ============================================
// Movement - Movimento
// ============================================
public struct Walkable { public float BaseSpeed; public float CurrentModifier; public float Accumulator; }

/// <summary>
/// Componente temporário que sinaliza intenção de movimento. 
/// Adicionado pelo MovementIntentSystem, consumido pelo ValidationSystem.
/// </summary>
public struct MovementIntent { public Position TargetPosition; }

/// <summary>
/// Tag: Movimento foi validado e pode ser aplicado.
/// </summary>
public struct MovementApproved;

public struct MovementBlocked { public MovementResult Reason; }
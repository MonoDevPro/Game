namespace Game.ECS.Shared.Components.Entities;

// ============================================
// AI identificadores
// ============================================
public struct AIControlled { }

/// <summary>
/// NPC behavior configuration component.
/// Stores the behavior type and ranges for AI decision making.
/// </summary>
public struct AIBehaviour
{
    public BehaviorType Type;
    public float VisionRange;
    public float AttackRange;
    public float LeashRange;
    public float PatrolRadius;
    public float IdleDurationMin;
    public float IdleDurationMax;

    public static readonly AIBehaviour Default = new()
    {
        Type = BehaviorType.Passive,
        VisionRange = 5f,
        AttackRange = 1.5f,
        LeashRange = 10f,
        PatrolRadius = 3f,
        IdleDurationMin = 2f,
        IdleDurationMax = 5f
    };
}

// 2. Estado Mental (O "Cérebro" Dinâmico)
public struct Brain
{
    public AIState CurrentState;
    public float StateTimer;
    public Arch.Core.Entity CurrentTarget;
}

public enum AIState : byte
{
    Idle,
    Patrol,
    Chase,
    Combat,
    ReturnHome
}

/// <summary>
/// Types of NPC behavior patterns.
/// </summary>
public enum BehaviorType : byte
{
    Passive,      // Won't attack unless attacked
    Aggressive,   // Attacks on sight
    Defensive,    // Defends territory
    Fearful,      // Runs from players
    Neutral       // Ignores players
}
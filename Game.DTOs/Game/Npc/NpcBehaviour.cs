namespace Game.DTOs.Game.Npc;

public readonly record struct Behaviour(
    BehaviorType BehaviorType,
    float VisionRange,
    float AttackRange,
    float LeashRange,
    float PatrolRadius,
    float IdleDurationMin,
    float IdleDurationMax
)
{
    public static readonly Behaviour Default = new(
        BehaviorType.Passive,
        VisionRange: 5f,
        AttackRange: 1.5f,
        LeashRange: 10f,
        PatrolRadius: 3f,
        IdleDurationMin: 2f,
        IdleDurationMax: 5f
    );
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
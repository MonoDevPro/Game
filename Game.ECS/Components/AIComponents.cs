using Arch.Core;
using Game.DTOs.Game.Npc;

namespace Game.ECS.Components;

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
}

// 2. Estado Mental (O "Cérebro" Dinâmico)
public struct Brain
{
    public AIState CurrentState;
    public float StateTimer;
    public Entity CurrentTarget;
}

public enum AIState : byte
{
    Idle,
    Patrol,
    Chase,
    Combat,
    ReturnHome
}
using Arch.Core;
using Game.ECS.Components;

namespace Game.ECS.Components;

/// <summary>
/// NPC behavior configuration component.
/// Stores the behavior type and ranges for AI decision making.
/// </summary>
public struct NpcBehavior
{
    public NpcBehaviorType Type;
    public float VisionRange;
    public float AttackRange;
    public float LeashRange;
    public float PatrolRadius;
    public float IdleDurationMin;
    public float IdleDurationMax;
}

// 2. Estado Mental (O "Cérebro" Dinâmico)
public struct NpcBrain
{
    public NpcState CurrentState;
    public float StateTimer;
    public Entity CurrentTarget;
}

public enum NpcState : byte
{
    Idle,
    Patrol,
    Chase,
    Combat,
    ReturnHome
}

// 3. Intenção de Navegação (Desacopla "Querer ir" de "Como ir")
// O BehaviorSystem define o Destination, o MovementSystem decide como chegar lá.
public struct NavigationAgent
{
    public Position? Destination; 
    public float StoppingDistance;
    public bool IsPathPending; // Flag para pedir recalculo de path
}

/// <summary>
/// Types of NPC behavior patterns.
/// </summary>
public enum NpcBehaviorType : byte
{
    Passive,      // Won't attack unless attacked
    Aggressive,   // Attacks on sight
    Defensive,    // Defends territory
    Fearful,      // Runs from players
    Neutral       // Ignores players
}

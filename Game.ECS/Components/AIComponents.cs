using Arch.Core;
using Game.DTOs.Npc;
using Game.ECS.Services.Snapshot.Data;

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

// 3. Intenção de Navegação (Desacopla "Querer ir" de "Como ir")
// O BehaviorSystem define o Destination, o MovementSystem decide como chegar lá.
public struct NavigationAgent
{
    public Position TargetPosition;
    public Position CurrentPosition;
    public float Speed;
    public float StoppingDistance;
    public bool IsPathPending;
}

/// <summary>
/// Cached navigation path for an entity.
/// Steps excludes the current position; NextIndex points to the next step to follow.
/// </summary>
public struct NavigationPath
{
    public Position? TargetPosition;
    public Position[] Waypoints;
    public int CurrentWaypointIndex;
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
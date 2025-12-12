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

// 3. Intenção de Navegação (Desacopla "Querer ir" de "Como ir")
// O BehaviorSystem define o Destination, o MovementSystem decide como chegar lá.
public struct NavigationAgent
{
    public Position? Destination; 
    public float StoppingDistance;
    public bool IsPathPending; // Flag para pedir recalculo de path
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
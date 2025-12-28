namespace Game.Domain.AI.Enums;

/// <summary>
/// Estado de IA do NPC.
/// </summary>
public enum NpcAIState : byte
{
    Idle = 0,
    Wandering = 1,
    Patrolling = 2,
    Chasing = 3,
    Attacking = 4,
    Returning = 5,
    Fleeing = 6,
    Dead = 7
}
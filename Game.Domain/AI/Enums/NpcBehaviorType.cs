namespace Game.Domain.AI.Enums;

/// <summary>
/// Comportamento do NPC.
/// </summary>
public enum NpcBehaviorType : byte
{
    Static = 0,
    Stationary = 0, // Alias
    Wander = 1,
    Patrol = 2,
    Guard = 3,
    Follow = 4,
    Flee = 5
}
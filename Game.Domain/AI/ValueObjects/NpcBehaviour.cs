using Game.Domain.AI.Enums;

namespace Game.Domain.AI.ValueObjects;

/// <summary>
/// Comportamento de NPC.
/// </summary>
public struct NpcBehavior
{
    public NpcBehaviorType Type;
    public NpcSubType SubType;
    public int WanderRadius;
    public int AggroRange;
    public int LeashRange;
}
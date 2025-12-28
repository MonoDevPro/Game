using Game.Domain.AI.Enums;

namespace Game.Domain.AI.ValueObjects;

/// <summary>
/// Estado de IA do NPC.
/// </summary>
public struct NpcAI
{
    public NpcAIState State;
    public int TargetEntityId;
    public long StateChangeTick;
    public long NextActionTick;
}
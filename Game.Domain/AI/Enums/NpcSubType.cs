namespace Game.Domain.AI.Enums;

/// <summary>
/// Subtipo de NPC.
/// </summary>
public enum NpcSubType : byte
{
    None = 0,
    Friendly = 1,
    Neutral = 2,
    Hostile = 3,
    Boss = 4,
    Elite = 5
}
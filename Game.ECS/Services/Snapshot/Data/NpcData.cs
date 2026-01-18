using Game.DTOs.Npc;
using MemoryPack;

namespace Game.ECS.Services.Snapshot.Data;

[MemoryPackable]
public readonly partial record struct NpcData(
    int NpcId, int NetworkId, int MapId, string Name,
    int X, int Y, int Z, int DirX, int DirY,
    int Hp, int MaxHp, float HpRegen,
    int Mp, int MaxMp, float MpRegen,
    int PhysicalAttack, int MagicAttack,
    int PhysicalDefense, int MagicDefense,
    BehaviorType BehaviorType,
    float VisionRange,
    float AttackRange,
    float LeashRange,
    float PatrolRadius,
    float IdleDurationMin,
    float IdleDurationMax
);

[MemoryPackable]
public readonly partial record struct NpcSpawnPacket(NpcData[] Npcs);

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
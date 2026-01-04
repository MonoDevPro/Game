using Game.DTOs.Npc;

namespace Game.Server.Npc;

public readonly record struct NpcSpawnPoint(
    int TemplateId,
    int MapId,
    sbyte Floor,
    int X,
    int Y
);

/// <summary>
/// Template base para criação de NPCs.
/// </summary>
public class NpcTemplate
{
    // Identidade
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;

    // Estatísticas
    public float MovementSpeed { get; init; }
    public float AttackSpeed { get; init; }
    public int PhysicalAttack { get; init; }
    public int MagicAttack { get; init; }
    public int PhysicalDefense { get; init; }
    public int MagicDefense { get; init; }

    // Vitais
    public int CurrentHp { get; init; }
    public int MaxHp { get; init; }
    public int CurrentMp { get; init; }
    public int MaxMp { get; init; }
    public float HpRegen { get; init; }
    public float MpRegen { get; init; }

    // Comportamento
    public BehaviorType BehaviorType { get; init; }
    public float VisionRange { get; init; }
    public float AttackRange { get; init; }
    public float LeashRange { get; init; }
    public float PatrolRadius { get; init; }
    public float IdleDurationMin { get; init; }
    public float IdleDurationMax { get; init; }
}

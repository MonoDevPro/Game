namespace GameECS.Modules.Entities.Shared.Data;

/// <summary>
/// Template para criação de NPCs.
/// </summary>
public sealed class NpcTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public EntityType Type { get; init; } = EntityType.Monster;
    public NpcSubType SubType { get; init; } = NpcSubType.Hostile;
    public int Level { get; init; } = 1;
    public int BaseHealth { get; init; } = 100;
    public int BaseDamage { get; init; } = 10;
    public int BaseDefense { get; init; } = 5;
    public int AggroRange { get; init; } = 8;
    public int AttackRange { get; init; } = 1;
    public float AttackSpeed { get; init; } = 1.0f;
    public float MovementSpeed { get; init; } = 1.0f;
    public NpcBehaviorType DefaultBehavior { get; init; } = NpcBehaviorType.Wander;
    public int WanderRadius { get; init; } = 5;
    public int RespawnDelayTicks { get; init; } = 300;
    public bool CanAttack { get; init; } = true;
}

/// <summary>
/// Template para criação de Pets.
/// </summary>
public sealed class PetTemplate
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public int BaseHealth { get; init; } = 100;
    public int BaseDamage { get; init; } = 10;
    public int AttackRange { get; init; } = 1;
    public float AttackSpeed { get; init; } = 1.0f;
    public float MovementSpeed { get; init; } = 1.2f;
    public PetMode DefaultMode { get; init; } = PetMode.Follow;
    public int FollowDistance { get; init; } = 3;
}

/// <summary>
/// Provider de templates de NPC.
/// </summary>
public interface INpcTemplateProvider
{
    NpcTemplate? Get(string id);
    IReadOnlyList<NpcTemplate> GetAll();
}

/// <summary>
/// Provider de templates de Pet.
/// </summary>
public interface IPetTemplateProvider
{
    PetTemplate? Get(string id);
    IReadOnlyList<PetTemplate> GetAll();
}

/// <summary>
/// Provider padrão com templates em código.
/// </summary>
public sealed class DefaultNpcTemplateProvider : INpcTemplateProvider
{
    private readonly Dictionary<string, NpcTemplate> _templates;

    public DefaultNpcTemplateProvider()
    {
        _templates = new Dictionary<string, NpcTemplate>
        {
            ["rat"] = new NpcTemplate
            {
                Id = "rat", Name = "Rat", Type = EntityType.Monster, SubType = NpcSubType.Hostile,
                Level = 1, BaseHealth = 25, BaseDamage = 5, BaseDefense = 2,
                AggroRange = 5, DefaultBehavior = NpcBehaviorType.Wander, WanderRadius = 4
            },
            ["wolf"] = new NpcTemplate
            {
                Id = "wolf", Name = "Wolf", Type = EntityType.Monster, SubType = NpcSubType.Hostile,
                Level = 5, BaseHealth = 80, BaseDamage = 15, BaseDefense = 8,
                AggroRange = 8, AttackSpeed = 1.2f, DefaultBehavior = NpcBehaviorType.Wander
            },
            ["skeleton"] = new NpcTemplate
            {
                Id = "skeleton", Name = "Skeleton", Type = EntityType.Monster, SubType = NpcSubType.Hostile,
                Level = 10, BaseHealth = 150, BaseDamage = 25, BaseDefense = 15,
                AggroRange = 10, DefaultBehavior = NpcBehaviorType.Guard
            },
            ["merchant"] = new NpcTemplate
            {
                Id = "merchant", Name = "Merchant", Type = EntityType.Npc, SubType = NpcSubType.Friendly,
                Level = 1, BaseHealth = 1000, BaseDamage = 0, BaseDefense = 100,
                AggroRange = 0, DefaultBehavior = NpcBehaviorType.Static, CanAttack = false
            },
            ["guard"] = new NpcTemplate
            {
                Id = "guard", Name = "Guard", Type = EntityType.Npc, SubType = NpcSubType.Friendly,
                Level = 50, BaseHealth = 5000, BaseDamage = 200, BaseDefense = 150,
                AggroRange = 8, DefaultBehavior = NpcBehaviorType.Guard
            },
            ["boss_dragon"] = new NpcTemplate
            {
                Id = "boss_dragon", Name = "Dragon Lord", Type = EntityType.Monster, SubType = NpcSubType.Boss,
                Level = 100, BaseHealth = 50000, BaseDamage = 500, BaseDefense = 300,
                AggroRange = 15, AttackRange = 3, AttackSpeed = 0.6f, DefaultBehavior = NpcBehaviorType.Guard
            }
        };
    }

    public NpcTemplate? Get(string id) => _templates.GetValueOrDefault(id);
    public IReadOnlyList<NpcTemplate> GetAll() => _templates.Values.ToList();
}

/// <summary>
/// Provider padrão de templates de Pet.
/// </summary>
public sealed class DefaultPetTemplateProvider : IPetTemplateProvider
{
    private readonly Dictionary<string, PetTemplate> _templates;

    public DefaultPetTemplateProvider()
    {
        _templates = new Dictionary<string, PetTemplate>
        {
            ["wolf_pet"] = new PetTemplate
            {
                Id = "wolf_pet", Name = "Wolf", BaseHealth = 200, BaseDamage = 20,
                AttackRange = 1, FollowDistance = 3
            },
            ["bear_pet"] = new PetTemplate
            {
                Id = "bear_pet", Name = "Bear", BaseHealth = 400, BaseDamage = 35,
                AttackRange = 2, AttackSpeed = 0.8f, FollowDistance = 2
            }
        };
    }

    public PetTemplate? Get(string id) => _templates.GetValueOrDefault(id);
    public IReadOnlyList<PetTemplate> GetAll() => _templates.Values.ToList();
}

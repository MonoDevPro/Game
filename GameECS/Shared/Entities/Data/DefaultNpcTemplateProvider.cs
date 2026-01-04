using Game.Domain.AI.Data;
using Game.Domain.AI.Enums;
using Game.Domain.AI.Interfaces;
using Game.Domain.Commons.Enums;

namespace GameECS.Shared.Entities.Data;

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

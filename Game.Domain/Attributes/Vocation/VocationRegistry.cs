using Game.Domain.ValueObjects.Attributes;
using Game.Domain.Attributes.Vocation.ValueObjects;
using Game.Domain.Enums;

namespace Game.Domain.Attributes.Vocation;

/// <summary>
/// Registro centralizado de todas as vocações com seus metadados.
/// </summary>
public static class VocationRegistry
{
    private static readonly Dictionary<VocationType, VocationInfo> Vocations = new();

    static VocationRegistry()
    {
        RegisterBaseVocations();
        RegisterPromotedVocations();
    }

    private static void RegisterBaseVocations()
    {
        // WARRIOR - Tank/Melee balanceado
        Register(new VocationInfo(
            Type: VocationType.Warrior,
            Name: "Guerreiro",
            Description: "Combatente corpo-a-corpo versátil com boa defesa e dano.",
            Tier: VocationTier.Base,
            Archetype: VocationArchetype.MeleeDps,
            BaseVocation: null,
            PromotionLevel: 0,
            BaseStats: new BaseStats(14, 10, 6, 12, 8),  // STR, DEX, INT, CON, SPR
            GrowthModifiers: new StatsModifier(1.2, 1.0, 0.6, 1.1, 0.8),
            CombatProfile: VocationCombatProfile.Melee
        ) { Promotions = [VocationType.Knight, VocationType.Berserker] });

        // ARCHER - DPS Ranged
        Register(new VocationInfo(
            Type: VocationType.Archer,
            Name: "Arqueiro",
            Description: "Especialista em combate à distância com alta precisão.",
            Tier: VocationTier.Base,
            Archetype: VocationArchetype.RangedDps,
            BaseVocation: null,
            PromotionLevel: 0,
            BaseStats: new BaseStats(10, 14, 8, 10, 8),
            GrowthModifiers: new StatsModifier(0.9, 1.3, 0.7, 0.9, 0.8),
            CombatProfile: VocationCombatProfile.Ranged
        ) { Promotions = [VocationType.Ranger, VocationType.Assassin] });

        // MAGE - DPS Mágico
        Register(new VocationInfo(
            Type: VocationType.Mage,
            Name: "Mago",
            Description: "Mestre das artes arcanas com poder destrutivo devastador.",
            Tier: VocationTier.Base,
            Archetype: VocationArchetype.MagicDps,
            BaseVocation: null,
            PromotionLevel: 0,
            BaseStats: new BaseStats(6, 8, 16, 8, 12),
            GrowthModifiers: new StatsModifier(0.5, 0.7, 1.4, 0.7, 1.1),
            CombatProfile: VocationCombatProfile.Magic
        ) { Promotions = [VocationType.Sorcerer, VocationType.Warlock] });

        // CLERIC - Suporte/Cura
        Register(new VocationInfo(
            Type: VocationType.Cleric,
            Name: "Clérigo",
            Description: "Devoto sagrado capaz de curar aliados e banir o mal.",
            Tier: VocationTier.Base,
            Archetype: VocationArchetype.Healer,
            BaseVocation: null,
            PromotionLevel: 0,
            BaseStats: new BaseStats(8, 8, 12, 10, 14),
            GrowthModifiers: new StatsModifier(0.7, 0.7, 1.0, 0.9, 1.3),
            CombatProfile: VocationCombatProfile.Hybrid with { PrimaryDamageType = DamageType.Magical }
        ) { Promotions = [VocationType.Priest, VocationType.Paladin] });
    }

    private static void RegisterPromotedVocations()
    {
        // === WARRIOR PROMOTIONS ===
        
        // KNIGHT - Tank definitivo
        Register(new VocationInfo(
            Type: VocationType.Knight,
            Name: "Cavaleiro",
            Description: "Defensor implacável, especialista em proteger aliados.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.Tank,
            BaseVocation: VocationType.Warrior,
            PromotionLevel: 50,
            BaseStats: new BaseStats(16, 10, 6, 18, 10),
            GrowthModifiers: new StatsModifier(1.1, 0.9, 0.5, 1.4, 0.9),
            CombatProfile: VocationCombatProfile.Melee with { BaseCriticalChance = 3f }
        ));

        // BERSERKER - DPS Melee puro
        Register(new VocationInfo(
            Type: VocationType.Berserker,
            Name: "Berserker",
            Description: "Guerreiro furioso que sacrifica defesa por dano brutal.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.MeleeDps,
            BaseVocation: VocationType.Warrior,
            PromotionLevel: 50,
            BaseStats: new BaseStats(20, 12, 4, 10, 6),
            GrowthModifiers: new StatsModifier(1.5, 1.1, 0.4, 0.8, 0.6),
            CombatProfile: VocationCombatProfile.Melee with { BaseAttackSpeed = 1.3f, BaseCriticalDamage = 200f }
        ));

        // === ARCHER PROMOTIONS ===
        
        // RANGER - Controle e utilitário
        Register(new VocationInfo(
            Type: VocationType.Ranger,
            Name: "Patrulheiro",
            Description: "Mestre da natureza com habilidades de controle e rastreamento.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.RangedDps,
            BaseVocation: VocationType.Archer,
            PromotionLevel: 50,
            BaseStats: new BaseStats(12, 16, 10, 12, 10),
            GrowthModifiers: new StatsModifier(1.0, 1.2, 0.9, 1.0, 0.9),
            CombatProfile: VocationCombatProfile.Ranged with { BaseAttackRange = 10 }
        ));

        // ASSASSIN - Burst crítico
        Register(new VocationInfo(
            Type: VocationType.Assassin,
            Name: "Assassino",
            Description: "Executor das sombras com dano crítico devastador.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.MeleeDps,
            BaseVocation: VocationType.Archer,
            PromotionLevel: 50,
            BaseStats: new BaseStats(14, 20, 6, 8, 6),
            GrowthModifiers: new StatsModifier(1.1, 1.5, 0.5, 0.7, 0.6),
            CombatProfile: new VocationCombatProfile(1, 1.5f, 25f, 250f, 0, DamageType.Physical)
        ));

        // === MAGE PROMOTIONS ===
        
        // SORCERER - DPS mágico máximo
        Register(new VocationInfo(
            Type: VocationType.Sorcerer,
            Name: "Feiticeiro",
            Description: "Mestre supremo da destruição arcana.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.MagicDps,
            BaseVocation: VocationType.Mage,
            PromotionLevel: 50,
            BaseStats: new BaseStats(4, 8, 22, 6, 12),
            GrowthModifiers: new StatsModifier(0.4, 0.6, 1.6, 0.5, 1.0),
            CombatProfile: VocationCombatProfile.Magic with { BaseCriticalChance = 15f, ManaCostPerAttack = 15 }
        ));

        // WARLOCK - DoT e Debuffs
        Register(new VocationInfo(
            Type: VocationType.Warlock,
            Name: "Bruxo",
            Description: "Manipulador de maldições e magias sombrias.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.MagicDps,
            BaseVocation: VocationType.Mage,
            PromotionLevel: 50,
            BaseStats: new BaseStats(6, 8, 18, 8, 14),
            GrowthModifiers: new StatsModifier(0.5, 0.6, 1.3, 0.7, 1.2),
            CombatProfile: VocationCombatProfile.Magic with { BaseAttackRange = 7 }
        ));

        // === CLERIC PROMOTIONS ===
        
        // PRIEST - Cura máxima
        Register(new VocationInfo(
            Type: VocationType.Priest,
            Name: "Sacerdote",
            Description: "Curandeiro supremo devotado à preservação da vida.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.Healer,
            BaseVocation: VocationType.Cleric,
            PromotionLevel: 50,
            BaseStats: new BaseStats(6, 8, 14, 10, 20),
            GrowthModifiers: new StatsModifier(0.5, 0.6, 1.1, 0.8, 1.5),
            CombatProfile: VocationCombatProfile.Magic with { BaseAttackRange = 8 }
        ));

        // PALADIN - Tank/Suporte híbrido
        Register(new VocationInfo(
            Type: VocationType.Paladin,
            Name: "Paladino",
            Description: "Cavaleiro sagrado que combina fé e aço.",
            Tier: VocationTier.Promoted,
            Archetype: VocationArchetype.Hybrid,
            BaseVocation: VocationType.Cleric,
            PromotionLevel: 50,
            BaseStats: new BaseStats(14, 8, 10, 14, 14),
            GrowthModifiers: new StatsModifier(1.1, 0.7, 0.9, 1.2, 1.1),
            CombatProfile: VocationCombatProfile.Hybrid
        ));
    }

    private static void Register(VocationInfo info) => Vocations[info.Type] = info;

    public static VocationInfo Get(VocationType type) =>
        Vocations.TryGetValue(type, out var info) 
            ? info 
            : throw new ArgumentException($"Vocação não registrada: {type}");

    public static bool TryGet(VocationType type, out VocationInfo? info) =>
        Vocations.TryGetValue(type, out info);

    public static IEnumerable<VocationInfo> GetAll() => Vocations.Values;

    public static IEnumerable<VocationInfo> GetByTier(VocationTier tier) =>
        Vocations.Values.Where(v => v.Tier == tier);

    public static IEnumerable<VocationInfo> GetByArchetype(VocationArchetype archetype) =>
        Vocations.Values.Where(v => v.Archetype == archetype);

    public static IEnumerable<VocationInfo> GetStarterVocations() =>
        GetByTier(VocationTier.Base);

    public static IEnumerable<VocationInfo> GetPromotionsFor(VocationType baseVocation) =>
        Vocations.Values.Where(v => v.BaseVocation == baseVocation);
}
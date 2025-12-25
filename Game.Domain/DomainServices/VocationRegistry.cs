using Game.Domain.ValueObjects.Attributes;
using Game.Domain.Attributes.Vocation.ValueObjects;
using Game.Domain.Enums;
using Game.Domain.ValueObjects.Vocation;

namespace Game.Domain.DomainServices;

/// <summary>
/// Registro centralizado de todas as vocações com seus metadados.
/// </summary>
public static class VocationRegistry
{
    private static readonly Dictionary<VocationType, VocationInfo> Vocations = new();

    static VocationRegistry()
    {
        RegisterBaseVocations();
    }

    private static void RegisterBaseVocations()
    {
        // WARRIOR - Tank/Melee balanceado
        Register(new VocationInfo(
            Type: VocationType.Warrior,
            Name: "Guerreiro",
            Description: "Combatente corpo-a-corpo versátil com boa defesa e dano.",
            Archetype: VocationArchetype.Melee,
            BaseStats: new BaseStats(14, 10, 6, 12, 8), // STR, DEX, INT, CON, SPR
            GrowthModifiers: new BaseStats(1.3, 0.9, 0.6, 1.2, 0.8), // STR, DEX, INT, CON, SPR
            CombatProfile: VocationCombatProfile.Melee));

        // ARCHER - DPS Ranged
        Register(new VocationInfo(
            Type: VocationType.Archer,
            Name: "Arqueiro",
            Description: "Especialista em combate à distância com alta precisão.",
            Archetype: VocationArchetype.Ranged,
            BaseStats: new BaseStats(10, 14, 8, 10, 8),
            GrowthModifiers: new BaseStats(0.9, 1.3, 0.7, 0.9, 0.8),
            CombatProfile: VocationCombatProfile.Ranged));

        // MAGE - DPS Mágico
        Register(new VocationInfo(
            Type: VocationType.Mage,
            Name: "Mago",
            Description: "Mestre das artes arcanas com poder destrutivo devastador.",
            Archetype: VocationArchetype.Magic,
            BaseStats: new BaseStats(6, 8, 16, 8, 12),
            GrowthModifiers: new BaseStats(0.5, 0.7, 1.4, 0.7, 1.1),
            CombatProfile: VocationCombatProfile.Magic));

        // CLERIC - Suporte/Cura
        Register(new VocationInfo(
            Type: VocationType.Cleric,
            Name: "Clérigo",
            Description: "Devoto sagrado capaz de curar aliados e banir o mal.",
            Archetype: VocationArchetype.Hybrid,
            BaseStats: new BaseStats(8, 8, 12, 10, 14),
            GrowthModifiers: new BaseStats(0.7, 0.7, 1.0, 0.9, 1.3),
            CombatProfile: VocationCombatProfile.Hybrid));
    }

    private static void Register(VocationInfo info) => Vocations[info.Type] = info;

    public static VocationInfo Get(VocationType type) =>
        Vocations.TryGetValue(type, out var info) 
            ? info 
            : throw new ArgumentException($"Vocação não registrada: {type}");

    public static bool TryGet(VocationType type, out VocationInfo? info) =>
        Vocations.TryGetValue(type, out info);

    public static IEnumerable<VocationInfo> GetAll() => Vocations.Values;

    public static IEnumerable<VocationInfo> GetByArchetype(VocationArchetype archetype) =>
        Vocations.Values.Where(v => v.Archetype == archetype);

    public static IEnumerable<VocationInfo> GetStarterVocations() =>
        GetAll();
}
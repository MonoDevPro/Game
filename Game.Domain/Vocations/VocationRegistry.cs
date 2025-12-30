using Game.Domain.Commons.Enums;
using Game.Domain.Commons.ValueObjects.Attributes;
using Game.Domain.Vocations.ValueObjects;

namespace Game.Domain.Vocations;

/// <summary>
/// Registro centralizado de todas as vocações com seus metadados.
/// GrowthModifiers seguem GameConstants.Scaling.GROWTH_SCALE (10 => 1.0).
/// </summary>
public static class VocationRegistry
{
    private static readonly Dictionary<VocationType, VocationInfo> Vocations = new();

    static VocationRegistry() => RegisterBaseVocations();

    private static void RegisterBaseVocations()
    {
        // WARRIOR - Tank/Melee balanceado
        Register(new VocationInfo(
            Type: VocationType.Warrior,
            Name: "Guerreiro",
            Description: "Combatente corpo-a-corpo versátil com boa defesa e dano.",
            Archetype: VocationArchetype.Melee,
            BaseStats: new BaseStats(14, 10, 6, 12, 8), // STR, DEX, INT, CON, SPR
            GrowthModifiers: new BaseStats(13, 9, 5, 12, 7)));

        // ARCHER - DPS Ranged
        Register(new VocationInfo(
            Type: VocationType.Archer,
            Name: "Arqueiro",
            Description: "Especialista em combate à distância com alta precisão.",
            Archetype: VocationArchetype.Ranged,
            BaseStats: new BaseStats(10, 14, 8, 10, 8),
            GrowthModifiers: new BaseStats(9, 13, 7, 9, 8)));

        // MAGE - DPS Mágico
        Register(new VocationInfo(
            Type: VocationType.Mage,
            Name: "Mago",
            Description: "Mestre das artes arcanas com poder destrutivo devastador.",
            Archetype: VocationArchetype.Magic,
            BaseStats: new BaseStats(6, 8, 16, 8, 12),
            GrowthModifiers: new BaseStats(5, 7, 14, 7, 11)));

        // CLERIC - Suporte/Cura
        Register(new VocationInfo(
            Type: VocationType.Cleric,
            Name: "Clérigo",
            Description: "Devoto sagrado capaz de curar aliados e banir o mal.",
            Archetype: VocationArchetype.Hybrid,
            BaseStats: new BaseStats(8, 8, 12, 10, 14),
            GrowthModifiers: new BaseStats(7, 7, 10, 9, 13)));
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

    public static IEnumerable<VocationInfo> GetStarterVocations() => GetAll();
}
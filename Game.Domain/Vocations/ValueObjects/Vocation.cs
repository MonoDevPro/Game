using System.Runtime.InteropServices;
using Game.Domain.Commons.Enums;

namespace Game.Domain.Vocations.ValueObjects;

/// <summary>
/// Vocação do personagem.
/// Component ECS simples para representar a classe do personagem.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Vocation(
    VocationType Type = VocationType.None, 
    VocationArchetype Archetype = VocationArchetype.None)
{
    public static Vocation Create(VocationType type)
    {
        return new Vocation(
            type, type switch {
                VocationType.Warrior => VocationArchetype.Melee,
                VocationType.Archer => VocationArchetype.Ranged,
                VocationType.Mage => VocationArchetype.Magic,
                VocationType.Cleric => VocationArchetype.Hybrid,
                _ => VocationArchetype.None });
    }
}
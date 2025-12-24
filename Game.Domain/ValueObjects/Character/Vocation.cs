using System.Runtime.InteropServices;
using Game.Domain.Enums;

namespace Game.Domain.ValueObjects.Character;

/// <summary>
/// Vocação do personagem.
/// Component ECS simples para representar a classe do personagem.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Vocation(VocationType Type)
{
    public static Vocation None => new(VocationType.None);
    public static Vocation Warrior => new(VocationType.Warrior);
    public static Vocation Archer => new(VocationType.Archer);
    public static Vocation Mage => new(VocationType.Mage);
    public static Vocation Cleric => new(VocationType.Cleric);
    
    public static implicit operator VocationType(Vocation vocation) => vocation.Type;
    public static implicit operator Vocation(VocationType type) => new(type);
}

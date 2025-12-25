using System.Runtime.InteropServices;
using Game.Domain.Enums;

namespace Game.Domain.ValueObjects.Character;

/// <summary>
/// Gênero do personagem.
/// Component ECS simples para representar o gênero.
/// </summary>
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct Gender(GenderType Type)
{
    public static Gender Male => new(GenderType.Male);
    public static Gender Female => new(GenderType.Female);
    public static Gender Unknown => new(GenderType.Unknown);
}

using GameECS.Shared.Combat.Data;
using GameECS.Shared.Entities.Data;

namespace GameECS.Shared.Entities.Components;

/// <summary>
/// Gênero do jogador.
/// </summary>
public struct Gender { public GenderType Type; }

/// <summary>
/// Componente de vocação do jogador.
/// </summary>
public struct Vocation { public VocationType Type; }

/// <summary>
/// Gênero da entidade.
/// </summary>
public enum GenderType : byte { Unknown = 0, Male = 1, Female = 2 }

/// <summary>
/// Tipos de vocação disponíveis no jogo.
/// </summary>
public enum VocationType : byte { Unknown = 0, Warrior = 1, Mage = 2, Archer = 3 }

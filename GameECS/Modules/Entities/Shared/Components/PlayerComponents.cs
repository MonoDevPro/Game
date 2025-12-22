namespace GameECS.Modules.Entities.Shared.Components;

/// <summary>
/// Gênero da entidade.
/// </summary>
public enum Gender : byte
{
    Male = 0,
    Female = 1
}

/// <summary>
/// Tipo de vocação da entidade.
/// </summary>
public enum VocationType : byte
{
    None = 0,
    Warrior = 1,
    Mage = 2,
    Archer = 3,
    Priest = 4
}

/// <summary>
/// Componente de vocação do jogador.
/// </summary>
public struct PlayerVocation
{
    public VocationType Type;
    public byte Level;
}

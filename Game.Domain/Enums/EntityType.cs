namespace Game.Domain.Enums;

/// <summary>
/// Tipos de entidade no jogo.
/// </summary>
public enum EntityType : byte
{
    None = 0,
    Player = 1,
    Npc = 2,
    Monster = 3,
    Pet = 4,
    Projectile = 5,
    Item = 6,
    Interactive = 7
}
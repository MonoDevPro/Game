
using Game.Domain.Enums;

namespace Game.ECS.Navigation.Data;

/// <summary>
/// Pacote de teleporte (posição instantânea).
/// </summary>
public struct TeleportMessage
{
    public int EntityId;
    public short X;
    public short Y;
    public short Z;
    public DirectionEnum FacingDirection;
}
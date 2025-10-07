using Game.Domain.Enums;
using Game.Domain.VOs;

namespace Game.Server.Players;

public readonly record struct PlayerNetworkState(int NetworkId, Coordinate Position, DirectionEnum Facing, uint Tick);

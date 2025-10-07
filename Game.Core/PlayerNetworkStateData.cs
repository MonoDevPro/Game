using Game.Domain.Enums;
using Game.Domain.VOs;

namespace Game.Core;

public readonly record struct PlayerNetworkStateData(int NetworkId, Coordinate Position, DirectionEnum Facing, uint Tick);

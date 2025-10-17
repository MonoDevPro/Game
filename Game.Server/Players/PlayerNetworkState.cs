using Game.Domain.Enums;
using Game.ECS.Components.Primitive;

namespace Game.Server.Players;

public readonly record struct PlayerNetworkState(int NetworkId, Coordinate Position, FacingEnum Facing, uint Tick);

using Arch.Core;
using Game.Infrastructure.ArchECS.Commons.Components;

namespace Game.Infrastructure.ArchECS.Events;

public readonly record struct MoveEvent(Entity Entity, Position TargetPosition, int TargetFloor);
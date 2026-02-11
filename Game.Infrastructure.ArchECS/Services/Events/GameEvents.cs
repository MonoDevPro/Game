using Arch.Core;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;

namespace Game.Infrastructure.ArchECS.Services.Events;

public readonly record struct MoveEvent(Entity Entity, Position TargetPosition, int TargetFloor);
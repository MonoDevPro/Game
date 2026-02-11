using Arch.Core;
using Game.Infrastructure.ArchECS.Services.Navigation.Components;

namespace Game.Infrastructure.ArchECS.Services.Events;

public readonly record struct SpawnEvent(Entity Entity, Position SpawnPosition, int SpawnFloor);

public readonly record struct DespawnEvent(Entity Entity);

public readonly record struct MoveEvent(Entity Entity, Position TargetPosition, int TargetFloor);
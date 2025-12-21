using Game.ECS.Shared.Components.Navigation;

namespace Game.ECS.Server.Modules.Navigation.Components;

public readonly record struct TeleportRequest(
    GridPosition Position, 
    MovementDirection Facing, 
    PathPriority Priority
);
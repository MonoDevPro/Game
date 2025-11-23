using Arch.Core;
using Game.ECS.Components;
using Game.ECS.Services;

namespace Game.ECS.Logic;

public static class MovementLogic
{
    public enum MovementResult
    {
        None,           // sem movimento (zero direction / speed)
        OutOfBounds,
        BlockedByMap,
        BlockedByEntity,
        Allowed
    }

    /// <summary>
    /// Calcula o novo position e avalia se o movimento é permitido.
    /// Não realiza side-effects.
    /// </summary>
    public static MovementResult TryComputeStep(
        in Entity entity,
        in Position current,
        in Floor floor,
        in Velocity vel,
        in Movement movement,
        float deltaTime,
        IMapGrid? grid,
        IMapSpatial? spatial,
        out SpatialPosition newSpatialPosition)
    {
        // compute candidate
        newSpatialPosition = new SpatialPosition(
            current.X, 
            current.Y, 
            floor.Level);

        if (vel is { X: 0, Y: 0 }) return MovementResult.None;
        if (vel.Speed <= 0f) return MovementResult.None;

        if (movement.Timer < SimulationConfig.CellSize) return MovementResult.None; // ainda não passou célula
        
        // compute candidate
        newSpatialPosition = new SpatialPosition(
            current.X + vel.X, 
            current.Y + vel.Y, 
            floor.Level);
        
        if (grid != null && !grid.InBounds(newSpatialPosition)) return MovementResult.OutOfBounds;
        if (grid != null && grid.IsBlocked(newSpatialPosition)) return MovementResult.BlockedByMap;

        if (spatial != null && spatial.TryGetFirstAt(newSpatialPosition, out var occupant) 
                            && occupant != default && occupant != Entity.Null && occupant != entity)
        {
            // aqui, MovementLogic não conhece a entidade que está movendo — caller decide se occupant == mover
            // We'll return BlockedByEntity and let caller handle occupant comparison if needed.
            return MovementResult.BlockedByEntity;
        }

        return MovementResult.Allowed;
    }
    
    public static float ComputeCellsPerSecond(in Walkable walkable, in InputFlags flags)
    {
        float speed = walkable.BaseSpeed + walkable.CurrentModifier;
        if (flags.HasFlag(InputFlags.Sprint))
            speed *= 1.5f;
        return speed;
    }
    
    public static (sbyte x, sbyte y) NormalizeInput(sbyte inputX, sbyte inputY)
    {
        sbyte nx = inputX switch { < 0 => -1, > 0 => 1, _ => 0 };
        sbyte ny = inputY switch { < 0 => -1, > 0 => 1, _ => 0 };
        return (nx, ny);
    }
}
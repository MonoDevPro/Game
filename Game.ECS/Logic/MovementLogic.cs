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
        in Velocity vel,
        in Movement movement,
        float deltaTime,
        IMapGrid? grid,
        IMapSpatial? spatial,
        out Position newPosition)
    {
        newPosition = current;

        if (vel is { DirectionX: 0, DirectionY: 0 }) return MovementResult.None;
        if (vel.Speed <= 0f) return MovementResult.None;

        if (movement.Timer < SimulationConfig.CellSize) return MovementResult.None; // ainda não passou célula

        // compute candidate
        newPosition = new Position
        {
            X = current.X + vel.DirectionX,
            Y = current.Y + vel.DirectionY,
            Z = current.Z
        };

        if (grid != null && !grid.InBounds(newPosition)) return MovementResult.OutOfBounds;
        if (grid != null && grid.IsBlocked(newPosition)) return MovementResult.BlockedByMap;

        if (spatial != null && spatial.TryGetFirstAt(newPosition, out var occupant) && occupant != default && occupant != Entity.Null && occupant != entity)
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
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons.ValueObjects.Identitys;
using Game.Domain.Commons.ValueObjects.Map;
using Game.Domain.Navigation;
using Game.Domain.Navigation.Core;
using Game.Domain.Navigation.ValueObjects;
using GameECS.Server.Entities.Components;

namespace GameECS.Server.Entities.Systems;

/// <summary>
/// Sistema de movimento baseado em grid com ocupacao e ticks.
/// </summary>
public sealed partial class MovementSystem(World world, NavigationGrid grid) : BaseSystem<World, long>(world)
{
    [Query]
    [All<Identity, GridPosition, MovementAction, AgentConfig, MovementInput>, None<Dead>]
    private void Update([Data] in long tick, ref Identity identity, ref GridPosition position, ref MovementAction movement, ref AgentConfig config, ref MovementInput input)
    {
        if (movement.IsMoving)
        {
            if (!movement.ShouldComplete(tick))
                return;

            position = movement.TargetCell;
            movement.Complete();

            if (input.HasInput && position.X == input.TargetX && position.Y == input.TargetY)
                input.Clear();

            return;
        }

        if (!input.HasInput)
            return;

        TryStartMovement(ref identity, ref position, ref movement, ref config, ref input, tick);
    }

    public bool TryStartMovement(Entity entity, in long tick)
    {
        if (!world.IsAlive(entity))
            return false;

        ref var identity = ref world.Get<Identity>(entity);
        ref var position = ref world.Get<GridPosition>(entity);
        ref var movement = ref world.Get<MovementAction>(entity);
        ref var config = ref world.Get<AgentConfig>(entity);
        ref var input = ref world.Get<MovementInput>(entity);

        if (movement.IsMoving || !input.HasInput)
            return false;

        return TryStartMovement(ref identity, ref position, ref movement, ref config, ref input, tick);
    }

    private bool TryStartMovement(
        ref Identity identity,
        ref GridPosition position,
        ref MovementAction movement,
        ref AgentConfig config,
        ref MovementInput input,
        in long tick)
    {
        if (input.TargetX == position.X && input.TargetY == position.Y)
        {
            input.Clear();
            return false;
        }

        var step = ComputeStep(position, new GridPosition(input.TargetX, input.TargetY), config.AllowDiagonal);
        if (step == GridPosition.Zero)
        {
            input.Clear();
            return false;
        }

        var next = new GridPosition(position.X + step.X, position.Y + step.Y);
        if (!grid.IsWalkableAndFree(next.X, next.Y))
        {
            if (++input.RetryCount >= config.MaxRetries)
                input.Clear();
            return false;
        }

        if (!grid.TryMoveOccupancy(position, next, identity.UniqueId))
        {
            if (++input.RetryCount >= config.MaxRetries)
                input.Clear();
            return false;
        }

        movement.Start(position, next, tick, config.GetMoveTicks(step.X != 0 && step.Y != 0));
        movement.Direction = DirectionExtensions.FromPositions(position, next);
        return true;
    }

    private static GridPosition ComputeStep(GridPosition current, GridPosition target, bool allowDiagonal)
    {
        int dx = Math.Sign(target.X - current.X);
        int dy = Math.Sign(target.Y - current.Y);

        if (!allowDiagonal && dx != 0 && dy != 0)
        {
            if (Math.Abs(target.X - current.X) >= Math.Abs(target.Y - current.Y))
                dy = 0;
            else
                dx = 0;
        }

        return new GridPosition(dx, dy);
    }
}

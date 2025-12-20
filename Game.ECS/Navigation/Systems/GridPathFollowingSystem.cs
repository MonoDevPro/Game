using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Game.ECS.Navigation.Components;
using Game.ECS.Navigation.Core;

namespace Game.ECS.Navigation.Systems;

/// <summary>
/// Sistema que faz entidades seguirem caminhos no grid.
/// Movimento discreto célula-a-célula.
/// </summary>
public sealed class GridPathFollowingSystem(World world, NavigationGrid grid) : BaseSystem<World, float>(world)
{
    public override void Update(in float deltaTime)
    {
        ProcessPathFollowing(World, deltaTime);
    }

    private void ProcessPathFollowing(World world, float deltaTime)
    {
        var query = new QueryDescription()
            .WithAll<GridPosition, GridMovement, GridPathBuffer, PathState, GridMovementSettings, GridNavigationAgent>();

        world.Query(in query, (Entity entity,
            ref GridPosition pos,
            ref GridMovement movement,
            ref GridPathBuffer path,
            ref PathState state,
            ref GridMovementSettings settings) =>
        {
            // Só processa se tem caminho pronto ou está seguindo
            if (state.Status != PathStatus.Ready && state.Status != PathStatus.Following)
                return;

            // Se está no meio de um movimento, continua
            if (movement.IsMoving)
            {
                UpdateMovement(ref movement, ref pos, deltaTime);
                
                if (movement.IsComplete)
                {
                    CompleteMovement(world, entity, ref pos, ref movement);
                }
                return;
            }

            // Verifica se chegou ao destino
            if (! path.IsValid || path.IsComplete)
            {
                CompleteNavigation(world, entity, ref state, ref movement);
                return;
            }

            state.Status = PathStatus.Following;

            // Obtém próximo waypoint
            var nextPos = path.GetCurrentWaypointAsPosition(grid. Width);
            
            if (nextPos.X < 0)
            {
                // Waypoint inválido
                CompleteNavigation(world, entity, ref state, ref movement);
                return;
            }

            // Já está no waypoint atual? 
            if (pos == nextPos)
            {
                path. AdvanceWaypoint();
                
                if (path. IsComplete)
                {
                    CompleteNavigation(world, entity, ref state, ref movement);
                }
                return;
            }

            // Verifica se próxima célula está walkable
            if (! grid.IsWalkable(nextPos.X, nextPos.Y))
            {
                // Caminho bloqueado - precisa recalcular
                state.Status = PathStatus.Failed;
                state. FailReason = PathFailReason.GoalBlocked;
                movement.Reset();

                if (world.Has<IsMoving>(entity))
                    world.Remove<IsMoving>(entity);

                // Adiciona tag de espera
                if (! world.Has<WaitingToMove>(entity))
                    world.Add(entity, new WaitingToMove { WaitTime = 0 });

                return;
            }

            // Inicia movimento para próxima célula
            StartMovement(world, entity, ref pos, ref movement, nextPos, ref settings);
            path.AdvanceWaypoint();
        });
    }

    [MethodImpl(MethodImplOptions. AggressiveInlining)]
    private static void UpdateMovement(ref GridMovement movement, ref GridPosition pos, float deltaTime)
    {
        movement.Progress += deltaTime / movement.Duration;

        if (movement.Progress >= 1.0f)
        {
            movement.Progress = 1.0f;
            pos = movement.To;
        }
    }

    private static void StartMovement(
        World world,
        Entity entity,
        ref GridPosition pos,
        ref GridMovement movement,
        GridPosition target,
        ref GridMovementSettings settings)
    {
        bool isDiagonal = pos.X != target. X && pos.Y != target.Y;
        float duration = settings.GetMoveDuration(isDiagonal);

        movement.StartMove(pos, target, duration);

        if (! world.Has<IsMoving>(entity))
            world.Add<IsMoving>(entity);

        // Remove tag de espera se existir
        if (world.Has<WaitingToMove>(entity))
            world.Remove<WaitingToMove>(entity);

        if (world.Has<ReachedDestination>(entity))
            world.Remove<ReachedDestination>(entity);
    }

    private static void CompleteMovement(
        World world,
        Entity entity,
        ref GridPosition pos,
        ref GridMovement movement)
    {
        pos = movement.To;
        movement.IsMoving = false;
        movement.Progress = 0;
    }

    private static void CompleteNavigation(
        World world,
        Entity entity,
        ref PathState state,
        ref GridMovement movement)
    {
        state.Status = PathStatus.Completed;
        movement.Reset();

        if (world.Has<IsMoving>(entity))
            world.Remove<IsMoving>(entity);

        if (! world.Has<ReachedDestination>(entity))
            world.Add<ReachedDestination>(entity);
    }
}

// Extension para MethodImpl
file static class MethodImplOptionsExtensions
{
    public const System.Runtime.CompilerServices. MethodImplOptions AggressiveInlining = 
        System.Runtime.CompilerServices.MethodImplOptions.AggressiveInlining;
}
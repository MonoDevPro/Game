using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Abstractions;
using Game.Domain.VOs;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Systems.Common;

namespace Game.ECS.Systems;

public sealed partial class PlayerInputSystem(World world) : GameSystem(world)
{
    [Query]
    [All<PlayerInput, Velocity, Direction, MovementSpeed, PlayerControlled>]
    [None<Dead>]
    private void ProcessInputMovement(Entity e, ref PlayerInput input, ref Velocity vel, ref Direction dir, in MovementSpeed speed)
    {
        // Se não há input de movimento, zera a velocidade e para.
        if (input.Movement.X == 0 && input.Movement.Y == 0)
            return;
        
        // Calcula velocidade (células por segundo)
        float cellsPerSecond = speed.BaseSpeed * speed.CurrentModifier;
        if ((input.Flags & InputFlags.Sprint) != 0)
        {
            cellsPerSecond *= 1.5f;
            input.Flags &= ~InputFlags.Sprint;
        }
        
        if (cellsPerSecond <= 0)
            return; // Sem movimento possível
        
        // Altera a direção conforme o input
        if (dir.Value.X != input.Movement.X || dir.Value.Y != input.Movement.Y)
        {
            dir.Value = new Coordinate(Math.Sign(dir.Value.X), Math.Sign(dir.Value.Y));
            World.MarkNetworkDirty(e, SyncFlags.Direction);
        }
        
        var moveDirection = new FCoordinate(input.Movement.X, input.Movement.Y).Normalized;
        vel.Value = moveDirection * cellsPerSecond;
        World.MarkNetworkDirty(e, SyncFlags.Velocity);
        
        // Zera o input de movimento após processar
        input.Movement = new GridOffset(0, 0);
    }
}
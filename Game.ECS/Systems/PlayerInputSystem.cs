using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
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
    private void ProcessInput(Entity entity, ref PlayerInput input, ref Velocity vel, ref Direction dir, in MovementSpeed speed)
    {
        // 1. Calcula velocidade (células por segundo)
        float cellsPerSecond = speed.BaseSpeed * speed.CurrentModifier;
        
        if ((input.Flags & InputFlags.Sprint) != 0)
            cellsPerSecond *= 1.5f;
        
        // 2. Só processa se houver input
        if (input.Movement is { X: 0, Y: 0 })
            return;
        
        // 3. Normaliza diagonal para mesma velocidade que reto
        if (input.Movement.X != 0 && input.Movement.Y != 0)
        {
            float diagonalSpeed = cellsPerSecond / MathF.Sqrt(2);
            vel.Value = new FCoordinate(
                input.Movement.X * diagonalSpeed,
                input.Movement.Y * diagonalSpeed);
        }
        else
        {
            vel.Value = new FCoordinate(
                input.Movement.X * cellsPerSecond,
                input.Movement.Y * cellsPerSecond);
        }
        
        // 4. Atualiza direção
        var newDir = new Coordinate(input.Movement.X, input.Movement.Y);
        if (newDir != dir.Value)
        {
            dir.Value = newDir;
            World.MarkNetworkDirty(entity, SyncFlags.Direction);
        }
        
        input.Flags &= ~InputFlags.Sprint; // Consome o sprint (é um toggle por frame)
        input.Movement = Coordinate.Zero; // Consome o movimento (é por frame)
    }
}
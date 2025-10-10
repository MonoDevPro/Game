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
        
        // 2. Aplica velocidade SEM normalização (grid = movimento discreto)
        // Movimento diagonal é intencionalmente mais lento que permitir
        // ou você quer que diagonal = 1 célula em vez de 2?
        
        // OPÇÃO A: Diagonal move em ambos os eixos (mais lento, mais realista)
        // vel.Value = new FCoordinate(
        //     input.Movement.X * cellsPerSecond,
        //     input.Movement.Y * cellsPerSecond);
        
        // OPÇÃO B: Diagonal = só 1 eixo prioritário (mais rápido, mais arcade)
        if (input.Movement.X != 0 && input.Movement.Y != 0)
        {
            // Diagonal = mesma velocidade que reto
            float diagonalSpeed = cellsPerSecond / MathF.Sqrt(2);
            vel.Value = new FCoordinate(
                input.Movement.X * diagonalSpeed,
                input.Movement.Y * diagonalSpeed);
        }
        else
        {
            // Reto = velocidade normal
            vel.Value = new FCoordinate(
                input.Movement.X * cellsPerSecond,
                input.Movement.Y * cellsPerSecond);
        }
        
        // 3. Atualiza direção apenas quando há movimento
        if (input.Movement.X != 0 || input.Movement.Y != 0)
        {
            var newDir = new Coordinate(input.Movement.X, input.Movement.Y);
            if (newDir != dir.Value)
            {
                dir.Value = newDir;
                World.MarkNetworkDirty(entity, SyncFlags.Direction);
            }
        }
        else
        {
            // Sem input = parado
            vel.Value = FCoordinate.Zero;
        }
    }
}
using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Abstractions;
using Game.Domain.VOs;
using Game.ECS.Components;
using Game.ECS.Extensions;
using Game.ECS.Systems.Common;

namespace Game.ECS.Systems;

public sealed partial class MovementSystem(World world, MapService map) : GameSystem(world)
{
    [Query]
    [All<Position, Velocity, MoveAccumulator>]
    private void MoveEntity(in Entity entity, ref Position pos, ref Velocity vel, ref MoveAccumulator acc, [Data] float deltaTime)
    {
        if (deltaTime <= 0f) return;

        // Se não há velocidade, retorna cedo
        if (vel.Value is { X: 0f, Y: 0f })
            return;

        // 1. Acumula movimento (células flutuantes)
        var frameMove = vel.Value * deltaTime;
        acc.Value = new FCoordinate(
            acc.Value.X + frameMove.X,
            acc.Value.Y + frameMove.Y);
        
        // 2. Extrai passos inteiros (truncate preserva sinal)
        int stepsX = (int)MathF.Truncate(acc.Value.X);
        int stepsY = (int)MathF.Truncate(acc.Value.Y);

        // 3. Se nenhum passo completo, retorna (ainda acumulando)
        if (stepsX == 0 && stepsY == 0)
            return;
        
        vel.Value = FCoordinate.Zero;
        
        // 4. Consome os passos do acumulador
        acc.Value = new FCoordinate(
            acc.Value.X - stepsX,
            acc.Value.Y - stepsY);

        var startPos = pos.Value;
        
        // 5. Aplica movimento step-by-step
        // Para grid, geralmente vai mover 1 célula por vez
        int iterations = Math.Max(Math.Abs(stepsX), Math.Abs(stepsY));
        int remainingX = stepsX;
        int remainingY = stepsY;

        for (int i = 0; i < iterations; i++)
        {
            int dx = 0, dy = 0;
            
            if (remainingX != 0)
            {
                dx = Math.Sign(remainingX);
                remainingX -= dx;
            }
            
            if (remainingY != 0)
            {
                dy = Math.Sign(remainingY);
                remainingY -= dy;
            }

            var candidate = new Coordinate(pos.Value.X + dx, pos.Value.Y + dy);

            // Tenta movimento diagonal primeiro
            if (map.InBounds(candidate) && !map.IsBlocked(candidate))
            {
                pos.Value = candidate;
            }
            else
            {
                // Se bloqueado, tenta deslizar nas paredes
                bool moved = false;
                
                // Tenta só X
                if (dx != 0)
                {
                    var candX = new Coordinate(pos.Value.X + dx, pos.Value.Y);
                    if (map.InBounds(candX) && !map.IsBlocked(candX))
                    {
                        pos.Value = candX;
                        moved = true;
                    }
                }

                // Se não moveu em X, tenta só Y
                if (!moved && dy != 0)
                {
                    var candY = new Coordinate(pos.Value.X, pos.Value.Y + dy);
                    if (map.InBounds(candY) && !map.IsBlocked(candY))
                    {
                        pos.Value = candY;
                        moved = true;
                    }
                }

                // Se não conseguiu mover, para de processar steps
                if (!moved)
                {
                    // IMPORTANTE: Zera o acumulador para não ficar "empurrando" a parede
                    acc.Value = FCoordinate.Zero;
                    break;
                }
            }
        }

        // 6. Se moveu, marca como dirty
        if (pos.Value != startPos)
        {
            Console.WriteLine($"[MOVE] Entity {entity.Id}: {startPos} → {pos.Value}");
            World.MarkNetworkDirty(entity, SyncFlags.Position);
        }
    }
}
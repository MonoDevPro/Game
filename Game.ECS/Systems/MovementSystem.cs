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
    private void MoveEntity(Entity e, ref Position pos, ref Velocity vel, ref MoveAccumulator acc, ref Direction dir, [Data] in float deltaTime)
    {
        if (deltaTime <= 0f) return;

        // 1) acumula movimento (células flutuantes) por eixo
        var frameMove = vel.Value * deltaTime; // Vector2F
        var totalX = acc.Value.X + frameMove.X;
        var totalY = acc.Value.Y + frameMove.Y;

        // 2) extrai passos inteiros (truncacao -> preserva sinal)
        int stepsX = (int)MathF.Truncate(totalX);
        int stepsY = (int)MathF.Truncate(totalY);

        // atualiza acumulador com a fração remanescente
        acc.Value = new FCoordinate(totalX - stepsX, totalY - stepsY);

        // se nenhum passo inteiro, ainda podemos atualizar facing se precisarmos
        if (stepsX == 0 && stepsY == 0)
        {
            if (frameMove.X != 0f || frameMove.Y != 0f)
            {
                dir.Value = new Coordinate(
                    X: frameMove.X > 0f ? 1 : (frameMove.X < 0f ? -1 : 0),
                    Y: frameMove.Y > 0f ? 1 : (frameMove.Y < 0f ? -1 : 0));
                World.MarkNetworkDirty(e, SyncFlags.Direction);
            }
            return;
        }

        var start = pos.Value;
        var remainingX = stepsX;
        var remainingY = stepsY;
        
        // Vamos aplicar os passos um a um, suportando diagonal e checando colisão a cada passo.
        // Número de iterações = max(|stepsX|, |stepsY|)
        int iterations = Math.Max(Math.Abs(stepsX), Math.Abs(stepsY));
        int movedX = 0, movedY = 0;

        for (int i = 0; i < iterations; i++)
        {
            int dx = 0, dy = 0;
            if (remainingX != 0)
            {
                dx = Math.Sign(remainingX); // +1 ou -1
                remainingX -= Math.Sign(remainingX);
            }
            if (remainingY != 0)
            {
                dy = Math.Sign(remainingY);
                remainingY -= Math.Sign(remainingY);
            }

            var candidate = new Coordinate(pos.Value.X + dx, pos.Value.Y + dy);

            // tenta o movimento diagonal/combinação primeiro
            if (map.InBounds(candidate) && !map.IsBlocked(candidate))
            {
                pos.Value = candidate;
                movedX += dx;
                movedY += dy;
            }
            else
            {
                // se bloqueado na combinação, tente mover só X
                if (dx != 0)
                {
                    var candX = new Coordinate(pos.Value.X + dx, pos.Value.Y);
                    if (map.InBounds(candX) && !map.IsBlocked(candX))
                    {
                        pos.Value = candX;
                        movedX += dx;
                        continue;
                    }
                }

                // tente só Y
                if (dy != 0)
                {
                    var candY = new Coordinate(pos.Value.X, pos.Value.Y + dy);
                    if (map.InBounds(candY) && !map.IsBlocked(candY))
                    {
                        pos.Value = candY;
                        movedY += dy;
                        continue;
                    }
                }

                // nenhum movimento possível neste sub-passo -> pare (pode manter as frações acumuladas)
                break;
            }
        }

        // se posição mudou, marque para sincronização de rede
        if (pos.Value.X != start.X || pos.Value.Y != start.Y)
        {
            World.MarkNetworkDirty(e, SyncFlags.Movement);
        }
    }
}
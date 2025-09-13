using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Shared;

namespace Simulation.Core.ECS.Server.Systems;

/// <summary>
/// Movement system adaptado ao estilo de source generators do Arch:
/// - [Query] + [All]/[None] para iniciar e para atualizar movimentos em progresso.
/// - Preserva flags existentes no StateComponent (opera por bits).
/// - O delta time é injetado pelo gerador via [Data] in float dt.
/// </summary>
public partial class MovementSystem(World world) : BaseSystem<World, float>(world)
{
    // Inicia movimento: entidades com InputComponent e sem MoveAction (ou seja, não estão se movendo)
    [Query]
    [All<InputComponent, Position, MoveStats, StateComponent>]
    [None<MoveAction>]
    private void StartMove(in Entity entity,
        ref Position pos,
        ref InputComponent input,
        ref MoveStats stats,
        ref StateComponent state)
    {
        try
        {
            // não mover se morto
            if ((state.Value & StateFlags.Dead) != 0) return;

            // precisa haver intenção de mover
            if ((input.Intent & IntentFlags.Move) == 0) return;

            // resolve direção a partir dos InputFlags
            var (dx, dy) = ResolveDirection(input.Input);
            if (dx == 0 && dy == 0) return; // sem direção

            // proteção contra speed inválido
            var speed = stats.Speed;
            if (speed <= 0f) return;

            // distância cartesiana entre tiles (1 para cardinal, ~=1.414 para diagonal)
            var distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance <= 0f) return;

            // target tile (inteiro)
            var target = new Position
            {
                X = pos.X + dx,
                Y = pos.Y + dy
            };

            // duração da ação = distância / velocidade (tiles por segundo)
            var duration = distance / speed;
            if (duration <= 0f) duration = 0.0001f;

            var action = new MoveAction
            {
                Start = pos,
                Target = target,
                Elapsed = 0f,
                Duration = duration
            };

            // adiciona a action e atualiza direção/estado (manipula flags por bits)
            World.Add<MoveAction>(entity, action);
            World.Set<Direction>(entity, new Direction { X = dx, Y = dy });

            // set Running bit, clear Idle bit (preserva outros bits)
            var newState = state.Value;
            newState |= StateFlags.Running;
            newState &= ~StateFlags.Idle;
            World.Set<StateComponent>(entity, new StateComponent { Value = newState });
        }
        catch (Exception ex)
        {
            try { Console.WriteLine($"StartMove error: {ex.Message}"); } catch { /* ignored */ }
        }
    }

    // Atualiza movimento em progresso — delta time injetado como [Data] in float dt
    [Query]
    [All<MoveAction, Position, MoveStats, StateComponent>]
    private void UpdateMove(in Entity entity,
        ref MoveAction action,
        ref Position pos,
        ref MoveStats stats,
        ref StateComponent state,
        [Data] in float dt) // <-- alteração aqui
    {
        try
        {
            // se está morto, cancela movimento e seta Idle (preserva outros bits)
            if ((state.Value & StateFlags.Dead) != 0)
            {
                World.Remove<MoveAction>(entity);
                var sDead = state.Value;
                sDead &= ~StateFlags.Running;
                sDead |= StateFlags.Idle;
                
                state = new StateComponent { Value = sDead };
                return;
            }

            // avança tempo usando dt
            var elapsed = action.Elapsed;
            elapsed += dt * stats.Speed; // escala por speed
            if (elapsed < 0f) 
                elapsed = 0f;
            
            World.Set<MoveAction>(entity, action with { Elapsed = elapsed });
            
            var t = action.Duration > 0f ? MathF.Min(1f, action.Elapsed / action.Duration) : 1f;

            // interpola (float) e arredonda para inteiro a atribuir Position
            var fx = Lerp(action.Start.X, action.Target.X, t);
            var fy = Lerp(action.Start.Y, action.Target.Y, t);

            var newPos = new Position
            {
                X = (int)MathF.Round(fx),
                Y = (int)MathF.Round(fy)
            };

            // atualiza somente se mudou
            if (newPos.X != pos.X || newPos.Y != pos.Y)
            {
                World.Set<Position>(entity, newPos);
            }

            if (t >= 1f)
            {
                // movimento completo
                World.Remove<MoveAction>(entity);

                // set Idle bit, clear Running bit (preserva outros bits)
                var s = state.Value;
                s &= ~StateFlags.Running;
                s |= StateFlags.Idle;
                World.Set<StateComponent>(entity, new StateComponent { Value = s });
            }
            else
            {
                // garante flag Running
                if ((state.Value & StateFlags.Running) == 0)
                {
                    var s = state.Value;
                    s |= StateFlags.Running;
                    s &= ~StateFlags.Idle;
                    World.Set<StateComponent>(entity, new StateComponent { Value = s });
                }
            }
        }
        catch (Exception ex)
        {
            try { Console.WriteLine($"UpdateMove error: {ex.Message}"); } catch { }
        }
    }

    // util: resolve InputFlags para direção integral (-1,0,+1)
    private static (int dx, int dy) ResolveDirection(InputFlags flags)
    {
        var dx = 0;
        var dy = 0;

        if ((flags & InputFlags.Left) != 0) dx -= 1;
        if ((flags & InputFlags.Right) != 0) dx += 1;
        if ((flags & InputFlags.Up) != 0) dy += 1;     // ajuste se sua convenção for diferente
        if ((flags & InputFlags.Down) != 0) dy -= 1;

        return (dx, dy);
    }

    private static float Lerp(int a, int b, float t) => a + (b - a) * t;
}
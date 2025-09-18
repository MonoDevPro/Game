using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;
using Simulation.Core.ECS.Pipeline;
using System;

namespace Simulation.Core.ECS.Systems;

[PipelineSystem(SystemStage.Movement, server: true, client: false)]
[DependsOn(typeof(IndexSystem))]
public partial class MovementSystem(World world) : BaseSystem<World, float>(world)
{
    // Inicia movimento: entidades com InputComponent e sem MoveAction (ou seja, não estão se movendo)
    [Query]
    [All<Input, Position, MoveStats, State>]
    [None<MoveAction>]
    private void StartMove(in Entity entity,
        ref Position pos,
        ref Input input,
        ref MoveStats stats,
        ref State state)
    {
        if ((state.Value & StateFlags.Dead) != 0) return; // morto não se move
        if ((input.IntentState & IntentFlags.Move) == 0) return; // sem intenção de mover

        var (dx, dy) = ResolveDirection(input.InputDir);
        if (dx == 0 && dy == 0) return; // sem direção

        var speed = stats.Speed;
        if (speed <= 0f) return;

        // distância cartesiana entre tiles (1 para cardinal, ~1.414 para diagonal)
        var distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance <= 0f) return;

        var target = new Position { X = pos.X + dx, Y = pos.Y + dy };
        var duration = distance / speed;
        if (duration <= 0f) duration = 0.0001f;

        var action = new MoveAction
        {
            Start = pos,
            Target = target,
            Elapsed = 0f,
            Duration = duration
        };

        World.Add<MoveAction, Direction>(entity, action, new Direction { X = dx, Y = dy });

        // set Running bit, clear Idle bit (preserva outros bits) — faz World.Set só se mudar
        var updatedState = ApplyFlags(state, set: StateFlags.Running, clear: StateFlags.Idle);
        if (updatedState.Value != state.Value)
        {
            World.Set<State>(entity, updatedState);
            state = updatedState;
        }
    }

    // Atualiza movimento em progresso — delta time injetado como [Data] in float dt
    [Query]
    [All<MoveAction, Position, MoveStats, State>]
    private void UpdateMove(in Entity entity,
        ref MoveAction action,
        ref Position pos,
        ref MoveStats stats,
        ref State state,
        [Data] in float dt)
    {
        // se está morto, cancela movimento e seta Idle (preserva outros bits)
        if ((state.Value & StateFlags.Dead) != 0)
        {
            World.Remove<MoveAction>(entity);

            var updatedDead = ApplyFlags(state, set: StateFlags.Idle, clear: StateFlags.Running);
            if (updatedDead.Value != state.Value)
            {
                World.Set<State>(entity, updatedDead);
                state = updatedDead;
            }

            return;
        }

        // avança tempo usando dt (elapsed em segundos — NOTA: usar dt, não dt * speed)
        var elapsed = action.Elapsed + dt;
        if (elapsed < 0f) elapsed = 0f;

        World.Set<MoveAction>(entity, action with { Elapsed = elapsed });

        var t = action.Duration > 0f ? MathF.Min(1f, elapsed / action.Duration) : 1f;

        // interpola (float) e arredonda para inteiro a atribuir Position
        var fx = Lerp(action.Start.X, action.Target.X, t);
        var fy = Lerp(action.Start.Y, action.Target.Y, t);

        var newPos = new Position
        {
            X = (int)MathF.Round(fx),
            Y = (int)MathF.Round(fy)
        };

        if (newPos.X != pos.X || newPos.Y != pos.Y)
            World.Set<Position>(entity, newPos);

        if (t >= 1f)
        {
            // movimento completo
            World.Remove<MoveAction>(entity);

            var finishedState = ApplyFlags(state, set: StateFlags.Idle, clear: StateFlags.Running);
            if (finishedState.Value != state.Value)
            {
                World.Set<State>(entity, finishedState);
                state = finishedState;
            }
        }
        else
        {
            // garante flag Running (somente se ainda não estiver setada)
            if ((state.Value & StateFlags.Running) == 0)
            {
                var runningState = ApplyFlags(state, set: StateFlags.Running, clear: StateFlags.Idle);
                World.Set<State>(entity, runningState);
                state = runningState;
            }
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

    // ------- helpers de flags --------
    private static State ApplyFlags(State current, StateFlags set, StateFlags clear)
    {
        var newVal = (current.Value | set) & ~clear;
        return new State { Value = newVal };
    }
}

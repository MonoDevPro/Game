using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;

namespace Simulation.Core.Server.ECS.Systems;

public partial class MoveSystem(World world) : BaseSystem<World, float>(world)
{
    
    // Inicia um passo de movimento (1 tile) a partir da intenção.
    [Query]
    [All<MoveIntent, MoveStats>]
    [None<MoveTarget, AttackTarget>]
    private void StartMove(in Entity entity, ref Position pos, ref MoveIntent intent, ref MoveStats stats)
    {
        var dx = intent.Direction.X;
        var dy = intent.Direction.Y;
        if ((dx | dy) == 0) return;           // sem direção
        if (stats.Speed <= 0f) return;        // sem velocidade

        // Distância cartesiana entre tiles (1 para cardinais, ~1.414 para diagonais)
        var distance = MathF.Sqrt(dx * dx + dy * dy);
        if (distance <= 0f) return;

        var target = new Position(pos.X + dx, pos.Y + dy);
        var duration = distance / stats.Speed;                // Speed em tiles/seg
        if (duration <= 0f) duration = 0.0001f;

        World.Add<MoveTarget, MoveTimer>(entity, 
            new MoveTarget(pos, target),
            new MoveTimer { Elapsed = 0f, Duration = duration });

        // Consome a intenção
        World.Remove<MoveIntent>(entity);
    }

    // Interpola posição durante o movimento (inteiros via arredondamento).
    [Query]
    [All<MoveTarget, MoveTimer>]
    private void UpdateMove(ref Position pos, ref MoveTarget mt, ref MoveTimer timer, [Data] in float dt)
    {
        timer = timer with { Elapsed = MathF.Max(0f, timer.Elapsed + dt) };
        var t = timer.Duration > 0f ? Math.Clamp(timer.Elapsed / timer.Duration, 0f, 1f) : 1f;

        var fx = mt.Start.X + (mt.Target.X - mt.Start.X) * t;
        var fy = mt.Start.Y + (mt.Target.Y - mt.Start.Y) * t;

        var newX = (int)MathF.Round(fx);
        var newY = (int)MathF.Round(fy);

        if (newX != pos.X || newY != pos.Y)
            pos = new Position(newX, newY);
    }

    [Query]
    [All<MoveTarget, MoveTimer>]
    private void CompleteMove(in Entity entity, ref MoveTarget mt, ref MoveTimer timer, ref Position pos)
    {
        if (timer.Elapsed < timer.Duration) return;

        pos = mt.Target;

        World.Remove<MoveTarget, MoveTimer>(entity);
    }
}
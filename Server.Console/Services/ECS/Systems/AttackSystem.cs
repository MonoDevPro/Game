using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Simulation.Core.ECS.Components;

namespace Server.Console.Services.ECS.Systems;

public partial class AttackSystem(World world) : BaseSystem<World, float>(world)
{
// Inicia o cast do ataque se não estiver atacando nem em cooldown.
    [Query]
    [All<AttackIntent, AttackStats>]
    [None<AttackCooldown, MoveTarget>]
    private void StartAttack(in Entity entity, ref AttackIntent intent, ref Position pos, ref AttackStats stats, ref PlayerState state)
    {
        var dx = intent.Direction.X;
        var dy = intent.Direction.Y;
        if ((dx | dy) == 0) return;

        // Alvo final no grid, em linha reta pela direção, a uma distância de AttackRange tiles.
        var target = new Position(
            pos.X + dx * stats.AttackRange,
            pos.Y + dy * stats.AttackRange
        );

        var cast = stats.CastTime;
        if (cast <= 0f) cast = 0.0001f;

        World.Add<AttackTarget, AttackTimer>(entity, 
            new AttackTarget(pos, target),
            new AttackTimer { Elapsed = 0f, Duration = cast });
        
        state = new PlayerState(Flags: state.Flags | StateFlags.Attacking);

        // Consome a intenção
        World.Remove<AttackIntent>(entity);
    }

    // Atualiza o cast; ao concluir, aplica dano e inicia cooldown.
    [Query]
    [All<AttackTarget, AttackTimer>]
    private void UpdateAttack(in Entity entity, ref AttackTimer timer, ref AttackTarget atk, ref AttackStats stats,
        [Data] in float dt, ref PlayerState state)
    {
        timer = timer with { Elapsed = MathF.Max(0f, timer.Elapsed + dt) };
        if (timer.Elapsed < timer.Duration) return;
        
        state = new PlayerState(Flags: state.Flags & ~StateFlags.Attacking);

        // Cast completo: resolve dano
        ResolveDamage(entity, atk, stats);

        // Inicia cooldown
        World.Add(entity, new AttackCooldown { CooldownRemaining = stats.Cooldown });

        // Limpa componentes da ação
        World.Remove<AttackTimer, AttackTarget>(entity);
    }

    // Aplica dano a uma entidade na célula alvo exata (tile-based).
    private void ResolveDamage(in Entity attacker, in AttackTarget atk, in AttackStats stats)
    {
        var targetX = atk.Target.X;
        var targetY = atk.Target.Y;
        // TODO: Implement This.
    }
    
    [Query]
    [All<AttackCooldown>]
    [None<AttackTarget>]
    private void TickCooldown(in Entity entity, ref AttackCooldown cd, [Data] in float dt)
    {
        var remaining = MathF.Max(0f, cd.CooldownRemaining - dt);
        if (remaining <= 0f)
            World.Remove<AttackCooldown>(entity);
        else
            cd = new AttackCooldown(CooldownRemaining: remaining);
    }
}
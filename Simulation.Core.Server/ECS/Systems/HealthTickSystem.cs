using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Components;

namespace Simulation.Core.Server.ECS.Systems;

/// <summary>
/// Sistema de teste no servidor que reduz lentamente a vida dos jogadores para validar sync de Health.
/// </summary>
public sealed class HealthTickSystem(World world) : BaseSystem<World, float>(world)
{
    private float _timer;

    public override void Update(in float dt)
    {
        _timer += dt;
        if (_timer < 5f) return;
        _timer = 0f;

        var q = world.Query(new QueryDescription().WithAll<PlayerId, Health>());
        foreach (ref var chunk in q.GetChunkIterator())
        {
            ref var healthFirst = ref chunk.GetFirst<Health>();
            foreach (var i in chunk)
            {
                ref var h = ref System.Runtime.CompilerServices.Unsafe.Add(ref healthFirst, i);
                var newVal = Math.Max(0, h.Current - 1);
                if (newVal != h.Current)
                    h = h with { Current = newVal };
            }
        }
    }
}
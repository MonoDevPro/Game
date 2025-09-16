using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Pipeline;

namespace Simulation.Core.ECS.Server.Systems;

/// <summary>
/// Placeholder para geração de snapshots periódicos do estado do mundo.
/// Futuro: exportar delta/snapshot para rollback, replay ou depuração.
/// </summary>
 [PipelineSystem(SystemStage.Post, -10, server:true, client:false)]
 [DependsOn(typeof(Simulation.Core.ECS.Server.Systems.EntitySaveSystem), typeof(Simulation.Core.ECS.Shared.Systems.EntityDestructorSystem))]
public sealed class SnapshotSystem(World world) : BaseSystem<World, float>(world)
{
    private ulong _tick;
    public override void Update(in float t)
    {
        _tick++;
        // TODO: A cada N ticks, capturar subconjunto de componentes para compressão.
    }
}

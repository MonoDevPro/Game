using Arch.Core;
using Arch.System;
using Simulation.Core.ECS.Pipeline;

namespace Simulation.Core.ECS.Client.Systems;

/// <summary>
/// Placeholder para predição de movimento/estado do cliente.
/// Futuro: aplicar inputs locais, reconciliar contra estado autoritativo.
/// </summary>
[PipelineSystem(SystemStage.Logic, -10, server:false, client:true)]
[DependsOn(typeof(Simulation.Core.ECS.Shared.Systems.NetworkSystem), typeof(Simulation.Core.ECS.Shared.Systems.EntityIndexSystem))]
public sealed class PredictionSystem(World world) : BaseSystem<World, float>(world)
{
    public override void Update(in float t)
    {
        // TODO: Aplicar predição (interpolar/extrapolar) antes de MovementSystem.
    }
}

using Arch.Core;
using Arch.System;
using Arch.System.SourceGenerator;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.Commons.ValueObjects.Identitys;
using Game.Domain.Commons.ValueObjects.Map;
using GameECS.Core;

namespace GameECS.Systems;

/// <summary>
/// Sistema de atualização de Area of Interest.
/// </summary>
public sealed partial class AOIUpdateSystem(World world, AOIManager aoiManager) : BaseSystem<World, long>(world)
{
    [Query]
    [All<Identity, GridPosition, VisibilityConfig, AreaOfInterest>, None<Dead>]
    private void Update([Data] in long tick, in Entity entity, ref Identity identity, ref GridPosition position, ref VisibilityConfig config, ref AreaOfInterest aoi)
    {
        // Só atualiza no rate configurado
        if (tick - aoi.LastUpdateTick < config.UpdateRate) return;

        var result = aoiManager.UpdateVisibility(entity, position, config);
        aoi.LastUpdateTick = tick;

        // Aqui você poderia emitir eventos para o cliente
        // OnEntitiesEnteredView(entity, result.EnteredView);
        // OnEntitiesLeftView(entity, result.LeftViewIds);
    }
}

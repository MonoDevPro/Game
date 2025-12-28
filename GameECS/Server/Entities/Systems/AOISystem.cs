using Arch.Core;
using Game.Domain.AOI.ValueObjects;
using Game.Domain.Combat.ValueObjects;
using Game.Domain.ValueObjects.Identitys;
using Game.Domain.ValueObjects.Map;
using GameECS.Core;

namespace GameECS.Systems;

/// <summary>
/// Sistema de atualização de Area of Interest.
/// </summary>
public sealed class AOIUpdateSystem(World world, AOIManager aoiManager) : IDisposable
{
    private readonly QueryDescription _aoiQuery = new QueryDescription()
        .WithAll<Identity, GridPosition, VisibilityConfig, AreaOfInterest>()
        .WithNone<Dead>();

    public void Update(long tick)
    {
        world.Query(in _aoiQuery, (Entity entity, ref Identity identity, ref GridPosition position, ref VisibilityConfig config, ref AreaOfInterest aoi) =>
        {
            // Só atualiza no rate configurado
            if (tick - aoi.LastUpdateTick < config.UpdateRate) return;

            var result = aoiManager.UpdateVisibility(entity, position, config);
            aoi.LastUpdateTick = tick;

            // Aqui você poderia emitir eventos para o cliente
            // OnEntitiesEnteredView(entity, result.EnteredView);
            // OnEntitiesLeftView(entity, result.LeftViewIds);
        });
    }

    public void Dispose() { }
}

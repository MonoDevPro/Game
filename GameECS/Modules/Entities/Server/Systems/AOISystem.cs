using Arch.Core;
using GameECS.Modules.Combat.Shared.Components;
using GameECS.Modules.Entities.Server.Core;
using GameECS.Modules.Entities.Shared.Components;
using GameECS.Modules.Navigation.Shared.Components;

namespace GameECS.Modules.Entities.Server.Systems;

/// <summary>
/// Sistema de atualização de Area of Interest.
/// </summary>
public sealed class AOIUpdateSystem : IDisposable
{
    private readonly World _world;
    private readonly AOIManager _aoiManager;
    private readonly QueryDescription _aoiQuery;

    public AOIUpdateSystem(World world, AOIManager aoiManager)
    {
        _world = world;
        _aoiManager = aoiManager;
        _aoiQuery = new QueryDescription()
            .WithAll<EntityIdentity, GridPosition, VisibilityConfig, AreaOfInterest>()
            .WithNone<Dead>();
    }

    public void Update(long tick)
    {
        _world.Query(in _aoiQuery, (Entity entity, ref EntityIdentity identity, ref GridPosition position, ref VisibilityConfig config, ref AreaOfInterest aoi) =>
        {
            // Só atualiza no rate configurado
            if (tick - aoi.LastUpdateTick < config.UpdateRate) return;

            var result = _aoiManager.UpdateVisibility(entity, position, config);
            aoi.LastUpdateTick = tick;

            // Aqui você poderia emitir eventos para o cliente
            // OnEntitiesEnteredView(entity, result.EnteredView);
            // OnEntitiesLeftView(entity, result.LeftViewIds);
        });
    }

    public void Dispose() { }
}

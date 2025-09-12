using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.Models;
using Simulation.Core.Persistence.Contracts;

namespace Server.Persistence.Staging;

public class MapStagingArea(IBackgroundTaskQueue saveQueue) : IMapStagingArea
{
    private readonly ConcurrentQueue<MapModel> _pendingMapLoaded = new();

    public void StageMapLoaded(MapModel model)
    {
        _pendingMapLoaded.Enqueue(model);
    }

    public bool TryDequeueMapLoaded(out MapModel? data) 
        => _pendingMapLoaded.TryDequeue(out data);

    public void StageSave(MapModel model)
    {
        saveQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            var repo = sp.GetRequiredService<IRepositoryAsync<int, MapModel>>();
            
            var existing = await repo.GetAsync(model.MapId, ct);
            
            await repo.UpdateAsync(model.MapId, model, ct);
        });
    }
}
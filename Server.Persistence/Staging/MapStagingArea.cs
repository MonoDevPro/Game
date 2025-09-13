using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.Persistence.Contracts;

namespace Server.Persistence.Staging;

public class MapStagingArea(IBackgroundTaskQueue saveQueue) : IMapStagingArea
{
    private readonly ConcurrentQueue<MapData> _pendingMapLoaded = new();

    public void StageMapLoaded(MapData model)
    {
        _pendingMapLoaded.Enqueue(model);
    }

    public bool TryDequeueMapLoaded(out MapData data) 
        => _pendingMapLoaded.TryDequeue(out data);

    public void StageSave(MapData data)
    {
        saveQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            // Resolve o repositório específico para mapas
            var repo = sp.GetRequiredService<IMapRepository>();
            await repo.AddFromDataAsync(data, ct);
        });
    }
}
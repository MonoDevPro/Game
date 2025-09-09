using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Server.Persistence.Contracts;
using Simulation.Core.Shared.Templates;

namespace Simulation.Core.Server.Staging;

public class MapStagingArea(IBackgroundTaskQueue saveQueue) : IMapStagingArea
{
    private readonly ConcurrentQueue<MapData> _pendingMapLoaded = new();

    public void StageMapLoaded(MapData data)
    {
        _pendingMapLoaded.Enqueue(data);
    }

    public bool TryDequeueMapLoaded(out MapData? data) 
        => _pendingMapLoaded.TryDequeue(out data);

    public void StageSave(MapData data)
    {
        saveQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            var repo = sp.GetRequiredService<IRepositoryAsync<int, MapData>>();
            
            var existing = await repo.GetAsync(data.MapId, ct);
            
            await repo.UpdateAsync(data.MapId, data, ct);
        });
    }
}
using Simulation.Core.ECS.Data;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Models;

namespace Client.Console;

public class MapRepository : IMapRepository
{
    private readonly Dictionary<int, MapModel> _store = new();
    public Task<MapData?> GetMapAsync(int id, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        _store.TryGetValue(id, out var model);
            
        if (model == null) return Task.FromResult<MapData?>(null);

        MapData? data = MapData.FromModel(model);
            
        return Task.FromResult(data);
    }
    public Task AddFromDataAsync(MapData data, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
            
        var model = data.ToModel();
            
        _store[model.Id] = model;

        return Task.CompletedTask;
    }

}
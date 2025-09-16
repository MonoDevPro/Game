using Server.Persistence.Context;
using Simulation.Core.ECS.Staging.Map;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Models;

namespace Server.Persistence.Repositories;

public class MapRepository(SimulationDbContext context) : EFCoreRepository<int, MapModel>(context), IMapRepository
{
    private readonly SimulationDbContext _context = context;

    /// <summary>
    /// Converte um MapData para um MapModel e adiciona-o ao DbContext.
    /// </summary>
    public async Task AddFromDataAsync(MapData data, CancellationToken ct = default)
    {
        var mapModel = data.ToModel();
        await _context.MapModels.AddAsync(mapModel, ct);
        await _context.SaveChangesAsync(ct);
    }
}
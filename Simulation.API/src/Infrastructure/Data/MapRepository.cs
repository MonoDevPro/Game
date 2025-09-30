using Application.Abstractions;
using AutoMapper;
using GameWeb.Application.Maps.Services;
using GameWeb.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace GameWeb.Infrastructure.Data
{
    public class MapRepository : EfRepoBase<Map>, IMapRepository
    {
        private readonly ILogger<MapRepository> _logger;

        public MapRepository(ApplicationDbContext db, IMapper mapper, ILogger<MapRepository> logger)
            : base(db, mapper)
        {
            _logger = logger;
        }

        public async Task<Map?> GetMapEntityAsync(int id, CancellationToken ct = default)
        {
            // tracking = true so caller can modify & save if desired
            return await GetAsync(m => m.Id == id, asNoTracking: false, ct);
        }

        public async Task<MapData?> GetMapAsync(int id, CancellationToken ct = default)
        {
            // projected DTO (no-tracking, database-side projection)
            return await GetProjectedAsync<MapData>(m => m.Id == id, ct);
        }

        public async Task SaveMapAsync(Map map, CancellationToken ct = default)
        {
            if (map == null) throw new ArgumentNullException(nameof(map));

            if (map.Id == 0)
            {
                await AddAsync(map, ct);
            }
            else
            {
                // Update: mark entity as modified (will attach if needed)
                Update(map);
            }

            await SaveChangesAsync(ct);
            _logger.LogInformation("Saved map {MapId} (Name: {MapName})", map.Id, map.Name);
        }

        public async Task DeleteMapAsync(int id, CancellationToken ct = default)
        {
            var entity = await GetAsync(m => m.Id == id, asNoTracking: false, ct);
            if (entity == null) return;

            Remove(entity);
            await SaveChangesAsync(ct);
            _logger.LogInformation("Deleted map {MapId}", id);
        }

        public async Task<IEnumerable<MapData>> GetAllMapsAsync(CancellationToken ct = default)
        {
            // Use projection to DTO to avoid loading full entities into memory
            var list = await ListProjectedAsync<MapData>(predicate: null, ct: ct);
            return list;
        }
    }
}

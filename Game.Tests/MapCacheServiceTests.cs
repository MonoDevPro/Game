using System.Threading.Tasks;
using Game.Domain.Entities;
using Game.Persistence;
using Game.Persistence.Interfaces;
using Game.Server.Simulation.Maps;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests;

public class MapCacheServiceTests
{
    [Fact]
    public async Task GetMapAsync_CachesAndInvalidates()
    {
        // Use shared in-memory SQLite so scopes share the same DB.
        var connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<GameDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddLogging(builder => builder.AddProvider(NullLoggerProvider.Instance));
        services.AddSingleton<IMapCacheService, MapCacheService>();

        var sp = services.BuildServiceProvider();

        // Ensure schema and seed.
        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            await ctx.Database.EnsureCreatedAsync();
        }

        var cache = sp.GetRequiredService<IMapCacheService>();

        var map1 = await cache.GetMapAsync(1);
        var map2 = await cache.GetMapAsync(1);
        Assert.Equal(map1.Id, map2.Id);

        await cache.InvalidateAsync(1);
        var map3 = await cache.GetMapAsync(1);
        Assert.Equal(map1.Id, map3.Id);

        await connection.CloseAsync();
    }
}
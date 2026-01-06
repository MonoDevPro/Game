using System.Threading.Tasks;
using Game.Domain.Entities;
using Game.Persistence;
using Game.Persistence.Interfaces;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Game.Tests;

public class MapRepositoryTests
{
    [Fact]
    public async Task CanLoadSeededMap_ById()
    {
        var connection = new SqliteConnection("Data Source=:memory:;Cache=Shared");
        await connection.OpenAsync();

        var services = new ServiceCollection();
        services.AddDbContext<GameDbContext>(options => options.UseSqlite(connection));
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        var sp = services.BuildServiceProvider();

        using (var scope = sp.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<GameDbContext>();
            await ctx.Database.EnsureCreatedAsync();

            var uow = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
            var map = await uow.Maps.GetByIdAsync(1, tracking: false);

            Assert.NotNull(map);
            Assert.Equal(1, map!.Id);
            Assert.False(string.IsNullOrWhiteSpace(map.Name));
        }

        await connection.CloseAsync();
    }
}

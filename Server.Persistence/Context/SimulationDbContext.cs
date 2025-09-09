using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Simulation.Core.Shared.Templates;

namespace Server.Persistence.Context;

public class SimulationDbContext(DbContextOptions<SimulationDbContext> options) : DbContext(options)
{
    public DbSet<PlayerData> PlayerTemplates { get; set; }
    public DbSet<MapData> MapTemplates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        base.OnModelCreating(modelBuilder);
    }
}
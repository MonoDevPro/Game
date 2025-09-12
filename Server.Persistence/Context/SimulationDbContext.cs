using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Simulation.Core.Models;

namespace Server.Persistence.Context;

public class SimulationDbContext(DbContextOptions<SimulationDbContext> options) : DbContext(options)
{
    public DbSet<PlayerModel> PlayerModels { get; set; }
    public DbSet<MapModel> MapModels { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        
        base.OnModelCreating(modelBuilder);
    }
}
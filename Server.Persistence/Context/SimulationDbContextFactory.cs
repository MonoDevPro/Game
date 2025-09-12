using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Server.Persistence.Context
{
    public class SimulationDbContextFactory : IDesignTimeDbContextFactory<SimulationDbContext>
    {
        public SimulationDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<SimulationDbContext>();

            // ⚠️ Ajuste a connection string conforme o seu banco
            optionsBuilder.UseSqlite("Data Source=GameDb.db;");

            return new SimulationDbContext(optionsBuilder.Options);
        }
    }
}
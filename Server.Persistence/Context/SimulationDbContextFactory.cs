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
            optionsBuilder.UseSqlite("Server=(localdb)\\mssqllocaldb;Database=GameDb;Trusted_Connection=True;");

            return new SimulationDbContext(optionsBuilder.Options);
        }
    }
}
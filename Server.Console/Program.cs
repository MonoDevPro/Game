using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Server.Console;
using Server.Persistence;
using Server.Persistence.Context;
using Server.Persistence.Seeds;
using Simulation.Core.ECS;
using Simulation.Core.ECS.Server;
using Simulation.Core.Options;

// 1. Configurar o Host da Aplicação
var host = Host.CreateDefaultBuilder(args)
    .ConfigureServices((context, services) =>
    {
        // 2. Adicionar os serviços de persistência e outros
        services.AddPersistence(context.Configuration);
        
        // Registar o SimulationBuilder e as Options
        services.AddSingleton<ISimulationBuilder<float>, ServerSimulationBuilder>();
        services.Configure<WorldOptions>(context.Configuration.GetSection(WorldOptions.SectionName));
        services.Configure<SpatialOptions>(context.Configuration.GetSection(SpatialOptions.SectionName));
        services.Configure<NetworkOptions>(context.Configuration.GetSection(NetworkOptions.SectionName));
        
        // 3. Adicionar a nossa lógica principal de jogo como um Hosted Service
        services.AddHostedService<GameServerHost>();
    })
    .Build();

// 4. Aplicar Migrações e Seeding da Base de Dados
await SeedDatabaseAsync(host.Services);

// 5. Executar o Host
await host.RunAsync();


// Função auxiliar para executar o seeder no arranque
static async Task SeedDatabaseAsync(IServiceProvider services)
{
    // Cria um "scope" para resolver serviços com tempo de vida 'Scoped' como o DbContext
    using var scope = services.CreateScope();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<SimulationDbContext>();
        // Esta linha irá criar a base de dados e aplicar as migrações
        await DataSeeder.SeedDatabaseAsync(dbContext);
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Ocorreu um erro ao aplicar as migrações ou ao semear a base de dados.");
        throw;
    }
}

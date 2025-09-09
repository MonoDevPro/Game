using Microsoft.EntityFrameworkCore;
using Server.Persistence.Context;
using Simulation.Core.Shared.Templates;

namespace Server.Persistence.Seeds;

public static class DataSeeder
{
    private static List<MapData> GetMapSeed()
    {
        var maps = new List<MapData>();
        for (int id = 0; id < 4; id++)
        {
            maps.Add(new MapData
                { MapId = id, Name = $"Default Map {id}", Width = 30, Height = 30, UsePadded = false, BorderBlocked = true });
        }
        foreach (var map in maps)
        {
            int size = map.Width * map.Height;
            map.TilesRowMajor = new TileType[size];
            map.CollisionRowMajor = new byte[size];

            for (int i = 0; i < size; i++)
            {
                map.TilesRowMajor[i] = TileType.Floor;
                map.CollisionRowMajor[i] = 0;
            }
        }
        return maps;
    }
    
    private static List<PlayerData> GetPlayerSeed()
    {
        var players = new List<PlayerData>
        {
            new() { Id = 1, Name = "Filipe", MapId = 1, PosX = 5, PosY = 5, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 2, Name = "Filipe", MapId = 1, PosX = 8, PosY = 8, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 2, Name = "Rodorfo", MapId = 1, PosX = 8, PosY = 8, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 2, Name = "Rodorfo", MapId = 1, PosX = 8, PosY = 8, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
        };
        return players;
    }
    
    // Este método pode ser chamado na inicialização da sua aplicação
    public static async Task SeedDatabaseAsync(SimulationDbContext context)
    {
        // Garante que o banco de dados foi criado pela migration
        await context.Database.MigrateAsync();

        // Verifica se já existem mapas para não duplicar os dados
        if (!await context.MapTemplates.AnyAsync())
        {
            context.MapTemplates.AddRange(GetMapSeed());
            await context.SaveChangesAsync();
            Console.WriteLine("--> Database seeded with initial MapTemplates.");
        }
        else
            Console.WriteLine("--> Database already has data. No seeding needed.");

        // Você pode adicionar outras chamadas de seed aqui
        if (!await context.PlayerTemplates.AnyAsync())
        {
            context.PlayerTemplates.AddRange(GetPlayerSeed());
            await context.SaveChangesAsync();
            Console.WriteLine("--> Database seeded with initial PlayerTemplates.");
        }
        else
            Console.WriteLine("--> Database already has PlayerTemplates. No seeding needed.");
    }
}
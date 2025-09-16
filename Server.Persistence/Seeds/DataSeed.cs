using Microsoft.EntityFrameworkCore;
using Server.Persistence.Context;
using Simulation.Core.Persistence.Models;
using BCrypt.Net;

namespace Server.Persistence.Seeds;

public static class DataSeeder
{
    private static List<MapModel> GetMapSeed()
    {
        var maps = new List<MapModel>();
        for (int id = 1; id < 4; id++)
        {
            maps.Add(new MapModel
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
    
    private static List<PlayerModel> GetPlayerSeed()
    {
        var players = new List<PlayerModel>
        {
            new() { Id = 1, Name = "Filipe", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Filipe"), MapId = 1, PosX = 5, PosY = 5, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 2, Name = "Rodorfo", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Rodorfo"), MapId = 2, PosX = 8, PosY = 8, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 3, Name = "Radouken", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Radouken"), MapId = 3, PosX = 10, PosY = 10, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
        };
        return players;
    }
    
    // Este método pode ser chamado na inicialização da sua aplicação
    public static async Task SeedDatabaseAsync(SimulationDbContext context)
    {
        // Garante que o banco de dados foi criado pela migration
        await context.Database.MigrateAsync();

        // Verifica se já existem mapas para não duplicar os dados
        if (!await context.MapModels.AnyAsync())
        {
            context.MapModels.AddRange(GetMapSeed());
            await context.SaveChangesAsync();
            Console.WriteLine("--> Database seeded with initial MapTemplates.");
        }
        else
            Console.WriteLine("--> Database already has data. No seeding needed.");

        // Você pode adicionar outras chamadas de seed aqui
        if (!await context.PlayerModels.AnyAsync())
        {
            context.PlayerModels.AddRange(GetPlayerSeed());
            await context.SaveChangesAsync();
            Console.WriteLine("--> Database seeded with initial PlayerTemplates.");
        }
        else
            Console.WriteLine("--> Database already has PlayerTemplates. No seeding needed.");
    }
}
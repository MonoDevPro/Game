using Simulation.Core.ECS.Components;
using Simulation.Core.Persistence.Models;

namespace Client.Console;

public static class DataSeeder
{
    public static List<MapData> GetMapSeeds()
    {
        var maps = new List<MapModel>();
            maps.Add(new MapModel
                { Id = 1, Name = "Default Map {1}", Width = 30, Height = 30, UsePadded = false, BorderBlocked = true });
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
        return maps.Select(MapData.FromModel).ToList();
    }
    
    public static List<PlayerData> GetPlayerSeeds()
    {
        var players = new List<PlayerModel>
        {
            new() { Id = 1, Name = "Filipe", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Filipe"), PosX = 5, PosY = 5, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 2, Name = "Rodorfo", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Rodorfo"), PosX = 8, PosY = 8, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 3, Name = "Radouken", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Radouken"), PosX = 10, PosY = 10, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
        };
        
        return players.Select(PlayerData.FromModel).ToList();
    }
}
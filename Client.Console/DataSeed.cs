using Simulation.Core.Persistence.Models;

namespace Client.Console;

public static class DataSeeder
{
    internal static List<MapModel> GetMapSeed()
    {
        var maps = new List<MapModel>();
        for (int id = 1; id < 4; id++)
        {
            maps.Add(new MapModel
                { Id = id, Name = $"Default Map {id}", Width = 30, Height = 30, UsePadded = false, BorderBlocked = true });
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
    
    internal static List<PlayerModel> GetPlayerSeed()
    {
        var players = new List<PlayerModel>
        {
            new() { Id = 1, Name = "Filipe", PosX = 5, PosY = 5, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 2, Name = "Rodorfo", PosX = 8, PosY = 8, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
            new() { Id = 3, Name = "Radouken", PosX = 10, PosY = 10, MoveSpeed = 1.0f, AttackCastTime = 1.0f, AttackCooldown = 1.0f },
        };
        return players;
    }
}
using Game.Domain;
using Microsoft.EntityFrameworkCore;

namespace Game.Infrastructure.EfCore;

public static class DbInitializer
{
    public static async Task EnsureCreatedAndSeedAsync(GameDbContext db, CancellationToken ct = default)
    {
        await db.Database.EnsureCreatedAsync(ct);

        if (await db.Accounts.AnyAsync(ct))
        {
            return;
        }

        var accounts = new AccountRow[]
        {
            new()
            {
                Username = "demo",
                PasswordHash = PasswordHasher.Hash("demo")
            },
            new()
            {
                Username = "player1",
                PasswordHash = PasswordHasher.Hash("password1")
            }
        };
        db.Accounts.AddRange(accounts);
        await db.SaveChangesAsync(ct);

        var characters = new CharacterRow[]
        {
            new CharacterRow
            {
                AccountId = accounts[0].Id, Name = "Warrior", Gender = (byte)Gender.Male, X = 10, Y = 10, 
                DirX = 0, DirY = 1,
                Vocation = (byte)Vocation.Warrior,
                Level = 10,
                Experience = 5000,
                Strength = 20,
                Endurance = 15,
                Agility = 10,
                Intelligence = 5,
                Willpower = 10,
                HealthPoints = 200,
                ManaPoints = 50
            },
            new CharacterRow
            {
                AccountId = accounts[0].Id, Name = "Mage", Gender = (byte)Gender.Male, X = 5, Y = 5, 
                DirX = 0, DirY = 1,
                Vocation = (byte)Vocation.Mage,
                Level = 10,
                Experience = 5000,
                Strength = 5,
                Endurance = 10,
                Agility = 10,
                Intelligence = 25,
                Willpower = 20,
                HealthPoints = 100,
                ManaPoints = 200
            },

            new CharacterRow
            {
                AccountId = accounts[1].Id, Name = "Player1_Warrior", Gender = (byte)Gender.Female, X = 15, Y = 15,
                DirX = 0, DirY = 1,
                Vocation = (byte)Vocation.Warrior,
                Level = 8,
                Experience = 3000,
                Strength = 18,
                Endurance = 14,
                Agility = 12,
                Intelligence = 6,
                Willpower = 11,
                HealthPoints = 180,
                ManaPoints = 40
            },
            new CharacterRow
            {
                AccountId = accounts[1].Id, Name = "Player1_Mage", Gender = (byte)Gender.Female, X = 20, Y = 20,
                DirX = 0, DirY = 1,
                Vocation = (byte)Vocation.Mage,
                Level = 8,
                Experience = 3000,
                Strength = 6,
                Endurance = 11,
                Agility = 11,
                Intelligence = 22,
                Willpower = 18,
                HealthPoints = 110,
                ManaPoints = 180
            },
        };
        db.Characters.AddRange(characters);
        await db.SaveChangesAsync(ct);
    }
}
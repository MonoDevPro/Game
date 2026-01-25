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

        var account = new AccountRow
        {
            Username = "demo",
            PasswordHash = PasswordHasher.Hash("demo")
        };
        db.Accounts.Add(account);
        await db.SaveChangesAsync(ct);

        var characters = new CharacterRow[]
        {
            new CharacterRow { AccountId = account.Id, Name = "Warrior", Gender = (byte)Gender.Male, X = 10, Y = 10, Direction = (byte)Direction.South },
            new CharacterRow { AccountId = account.Id, Name = "Mage", Gender = (byte)Gender.Male, X = 5, Y = 5, Direction = (byte)Direction.South },
        };
        db.Characters.AddRange(characters);
        await db.SaveChangesAsync(ct);
        
        db.CharacterVocations.AddRange(new CharacterVocationRow[]
        {
            new CharacterVocationRow
            {
                CharacterId = characters[0].Id,
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
            new CharacterVocationRow
            {
                CharacterId = characters[1].Id,
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
            }
        });
        
    }
}

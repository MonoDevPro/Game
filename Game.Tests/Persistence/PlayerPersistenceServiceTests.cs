using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Persistence;
using Game.Server.Players;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests.Persistence;

public class PlayerPersistenceServiceTests
{
    private static DbContextOptions<GameDbContext> CreateOptions()
    {
        return new DbContextOptionsBuilder<GameDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task PersistAsync_UpdatesCharacterStatsAndInventory()
    {
        var options = CreateOptions();

        await using (var arrangeContext = new GameDbContext(options))
        {
            var account = new Account
            {
                Username = "player1",
                Email = "player1@test.com",
                PasswordHash = "hash",
                PasswordSalt = new byte[] { 0x01 }
            };

            var character = new Character
            {
                Name = "Hero",
                Gender = Gender.Unknown,
                Vocation = VocationType.Warrior,
                DirectionEnum = DirectionEnum.South,
                Account = account,
                Inventory = new Inventory
                {
                    Slots = new List<InventorySlot>
                    {
                        new InventorySlot { SlotIndex = 0, Quantity = 1, ItemId = 100 },
                        new InventorySlot { SlotIndex = 1, Quantity = 2, ItemId = 200 }
                    }
                },
                Stats = new Stats
                {
                    Level = 3,
                    Experience = 100,
                    BaseStrength = 5,
                    BaseDexterity = 5,
                    BaseIntelligence = 5,
                    BaseConstitution = 5,
                    BaseSpirit = 5,
                    CurrentHp = 80,
                    CurrentMp = 40
                }
            };

            arrangeContext.Characters.Add(character);
            await arrangeContext.SaveChangesAsync();
        }

        Character snapshot;
        await using (var snapshotContext = new GameDbContext(options))
        {
            var original = await snapshotContext.Characters
                .Include(c => c.Stats)
                .Include(c => c.Inventory)
                    .ThenInclude(i => i.Slots)
                .FirstAsync();

            snapshot = new Character
            {
                Id = original.Id,
                AccountId = original.AccountId,
                Name = original.Name,
                Gender = original.Gender,
                Vocation = original.Vocation,
                PositionX = 10,
                PositionY = 15,
                DirectionEnum = DirectionEnum.North,
                Inventory = new Inventory
                {
                    Id = original.Inventory.Id,
                    CharacterId = original.Id,
                    Slots = new List<InventorySlot>
                    {
                        new InventorySlot
                        {
                            SlotIndex = 0,
                            Quantity = 5,
                            ItemId = original.Inventory.Slots.First().ItemId,
                            InventoryId = original.Inventory.Id
                        },
                        new InventorySlot
                        {
                            SlotIndex = 2,
                            Quantity = 1,
                            ItemId = 300,
                            InventoryId = original.Inventory.Id
                        }
                    }
                },
                Stats = new Stats
                {
                    Id = original.Stats.Id,
                    CharacterId = original.Id,
                    Level = original.Stats.Level + 1,
                    Experience = original.Stats.Experience + 50,
                    BaseStrength = original.Stats.BaseStrength,
                    BaseDexterity = original.Stats.BaseDexterity,
                    BaseIntelligence = original.Stats.BaseIntelligence,
                    BaseConstitution = original.Stats.BaseConstitution,
                    BaseSpirit = original.Stats.BaseSpirit,
                    CurrentHp = 55,
                    CurrentMp = 12
                }
            };
        }

        await using (var actContext = new GameDbContext(options))
        {
            var service = new PlayerPersistenceService(actContext, NullLogger<PlayerPersistenceService>.Instance);
            await service.PersistAsync(snapshot, CancellationToken.None);
        }

        await using var assertContext = new GameDbContext(options);
        var persisted = await assertContext.Characters
            .Include(c => c.Stats)
            .Include(c => c.Inventory)
                .ThenInclude(i => i.Slots)
            .FirstAsync();

        persisted.PositionX.Should().Be(10);
        persisted.PositionY.Should().Be(15);
        persisted.DirectionEnum.Should().Be(DirectionEnum.North);

        persisted.Stats.Level.Should().Be(snapshot.Stats!.Level);
        persisted.Stats.Experience.Should().Be(snapshot.Stats!.Experience);
        persisted.Stats.CurrentHp.Should().Be(snapshot.Stats!.CurrentHp);
        persisted.Stats.CurrentMp.Should().Be(snapshot.Stats!.CurrentMp);

        persisted.Inventory.Slots.Should().HaveCount(2);
        persisted.Inventory.Slots.Should().ContainSingle(s => s.SlotIndex == 0 && s.Quantity == 5);
        persisted.Inventory.Slots.Should().ContainSingle(s => s.SlotIndex == 2 && s.ItemId == 300);
        persisted.Inventory.Slots.Should().NotContain(s => s.SlotIndex == 1);
    }
}

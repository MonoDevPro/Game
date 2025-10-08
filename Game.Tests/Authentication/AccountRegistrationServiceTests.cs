using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Game.Core.Security;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Persistence;
using Game.Server.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests.Authentication;

public class AccountRegistrationServiceTests
{
    private static DbContextOptions<GameDbContext> CreateOptions() => new DbContextOptionsBuilder<GameDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options;

    [Fact]
    public async Task RegisterAsync_CreatesAccountAndCharacter()
    {
        var options = CreateOptions();

        await using var context = new GameDbContext(options);
        var hasher = new PasswordHasher();
        var service = new AccountRegistrationService(context, hasher, NullLogger<AccountRegistrationService>.Instance);

        var result = await service.RegisterAsync(
            "newPlayer",
            "player@example.com",
            "secret",
            "HeroOne",
            Gender.Unknown,
            VocationType.Warrior,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Account.Should().NotBeNull();
        result.Character.Should().NotBeNull();

        var storedAccount = await context.Accounts
            .Include(a => a.Characters)
            .ThenInclude(c => c.Inventory)
            .Include(a => a.Characters)
            .ThenInclude(c => c.Stats)
            .FirstOrDefaultAsync(a => a.Username == "newPlayer");

        storedAccount.Should().NotBeNull();
        storedAccount!.Characters.Should().HaveCount(1);

        var character = storedAccount.Characters.First();
        character.Inventory.Should().NotBeNull();
        character.Stats.Should().NotBeNull();
        character.PositionX.Should().Be(5);
        character.PositionY.Should().Be(5);
    }

    [Fact]
    public async Task RegisterAsync_DetectsDuplicateUsername()
    {
        var options = CreateOptions();

        await using (var seedContext = new GameDbContext(options))
        {
            seedContext.Accounts.Add(new Account
            {
                Username = "existing",
                Email = "existing@example.com",
                PasswordHash = "hash",
                PasswordSalt = new byte[] { 0x01 },
                IsActive = true
            });

            await seedContext.SaveChangesAsync();
        }

        await using var context = new GameDbContext(options);
        var hasher = new PasswordHasher();
        var service = new AccountRegistrationService(context, hasher, NullLogger<AccountRegistrationService>.Instance);

        var result = await service.RegisterAsync(
            "existing",
            "player@example.com",
            "secret",
            "HeroTwo",
            Gender.Unknown,
            VocationType.Warrior,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Message.Should().NotBeNullOrWhiteSpace();
    }
}

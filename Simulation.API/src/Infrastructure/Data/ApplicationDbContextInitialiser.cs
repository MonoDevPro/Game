using AutoMapper;
using GameWeb.Application.Common.Options;
using GameWeb.Application.Maps.Models;
using GameWeb.Domain.Constants;
using GameWeb.Domain.Entities;
using GameWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GameWeb.Infrastructure.Data;

public static class InitialiserExtensions
{
    public static async Task InitialiseDatabaseAsync(this WebApplication app)
    {
        using var scope = app.Services.CreateScope();

        var initialiser = scope.ServiceProvider.GetRequiredService<ApplicationDbContextInitialiser>();

        await initialiser.InitialiseAsync();
        await initialiser.SeedAsync();
    }
}

public class ApplicationDbContextInitialiser(
    ILogger<ApplicationDbContextInitialiser> logger,
    ApplicationDbContext context,
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IMapper map)
{
    public async Task InitialiseAsync()
    {
        try
        {
            // See https://jasontaylor.dev/ef-core-database-initialisation-strategies
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while initialising the database.");
            throw;
        }
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An error occurred while seeding the database.");
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // 1) Seed maps from appsettings (idempotent / upsert)
        var configured = new List<MapDto>();
        configured.Add(new MapDto
        {
            Id = 1,
            Name = "Default Map 1",
            Width = 100,
            Height = 100,
            BorderBlocked = true,
            UsePadded = false,
            CollisionRowMajor = new byte[100 * 100],
            TilesRowMajor = new TileType[100 * 100],
        });
        configured.Add(new MapDto
        {
            Id = 2,
            Name = "Default Map 2",
            Width = 100,
            Height = 100,
            BorderBlocked = true,
            UsePadded = false,
            CollisionRowMajor = new byte[100 * 100],
            TilesRowMajor = new TileType[100 * 100],
        });

        foreach (var m in configured)
        {
            var existing = await context.Maps.FindAsync(m.Id);
            if (existing == null)
                context.Maps.Add(map.Map<Map>(m));
        }

        await context.SaveChangesAsync();

        // 2) Default roles & users (mantém seu código original)
        var administratorRole = new IdentityRole(Roles.Administrator);
        if (roleManager.Roles.All(r => r.Name != administratorRole.Name))
            await roleManager.CreateAsync(administratorRole);

        var administrator = new ApplicationUser { UserName = "administrator@localhost", Email = "administrator@localhost" };

        if (userManager.Users.All(u => u.UserName != administrator.UserName))
        {
            await userManager.CreateAsync(administrator, "Administrator1!");
            if (!string.IsNullOrWhiteSpace(administratorRole.Name))
                await userManager.AddToRolesAsync(administrator, new [] { administratorRole.Name });
        }
    }
}

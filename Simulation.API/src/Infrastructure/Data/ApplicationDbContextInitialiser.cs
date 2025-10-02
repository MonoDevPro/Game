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
    IOptions<MapOptions> mapOptions,
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
        var configured = mapOptions.Value.Maps ?? Array.Empty<MapInfo>();

        foreach (var m in configured)
        {
            var existing = await context.Maps.FindAsync(m.Id);
            if (existing == null)
            {
                // Criar novo mapa com valores iniciais. Ajuste Width/Height conforme necessário.
                var dto = new MapDto
                {
                    Id = m.Id,
                    Name = m.Name,
                    Width = m.Width,
                    Height = m.Height,
                    BorderBlocked = m.BorderBlocked,
                    UsePadded = m.UsePadded,
                    CollisionRowMajor = new byte[m.Width * m.Height],
                    TilesRowMajor = new TileType[m.Width * m.Height],
                };

                context.Maps.Add(map.Map<Map>(dto));
            }
            else
            {
                // Política conservadora: atualiza somente dados "não-destrutivos"
                var changed = false;
                if (existing.Name != m.Name) { existing.Name = m.Name; changed = true; }
                if (existing.Width != m.Width)  { existing.Width = m.Width; changed = true; }
                if (existing.Height != m.Height){ existing.Height = m.Height; changed = true; }
                if (existing.UsePadded != m.UsePadded) { existing.UsePadded = m.UsePadded; changed = true; }
                if (existing.BorderBlocked != m.BorderBlocked) { existing.BorderBlocked = m.BorderBlocked; changed = true; }

                // Se você alterar Width/Height, considere também redefinir Collision/Tiles se isto fizer sentido.
                if (changed)
                    context.Maps.Update(existing);
            }
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

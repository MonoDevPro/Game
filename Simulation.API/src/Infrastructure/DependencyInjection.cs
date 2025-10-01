using GameWeb.Application.Common.Interfaces;
using GameWeb.Domain.Constants;
using GameWeb.Infrastructure.Data;
using GameWeb.Infrastructure.Data.Interceptors;
using GameWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using GameWeb.Infrastructure.Identity.Contracts;

namespace GameWeb.Infrastructure;

public static class DependencyInjection
{
    public static void AddInfrastructureServices(this IHostApplicationBuilder builder)
    {
        var connectionString = builder.Configuration.GetConnectionString("GameWebDb");
        Guard.Against.Null(connectionString, message: "Connection string 'GameWebDb' not found.");
        
        builder.Services.AddSingleton(TimeProvider.System);

        builder.Services.AddScoped<ISaveChangesInterceptor, AuditableEntityInterceptor>();
        builder.Services.AddScoped<ISaveChangesInterceptor, DispatchDomainEventsInterceptor>();

        builder.Services.AddDbContext<ApplicationDbContext>((sp, options) =>
        {
            options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());
            options.UseSqlite(connectionString);
        });
        
        builder.Services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());
        
        builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();

        builder.Services.AddScoped<ApplicationDbContextInitialiser>();
        
        // Authentication & Authorization
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var key = jwtSection["Key"];
        var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(string.IsNullOrWhiteSpace(key) ? "dev_secret_change_me" : key!));

        builder.Services
            .AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(issuer),
                    ValidIssuer = issuer,
                    ValidateAudience = !string.IsNullOrWhiteSpace(audience),
                    ValidAudience = audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = signingKey,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1)
                };
            });

        builder.Services.AddAuthorizationBuilder();

        builder.Services
            .AddIdentityCore<ApplicationUser>(options =>
            {
                // Optional: dev-friendly password rules; adjust for prod
                options.Password.RequireDigit = false;
                options.Password.RequireLowercase = false;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequiredLength = 6;
                options.User.RequireUniqueEmail = true;
            })
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddClaimsPrincipalFactory<CustomClaimsPrincipalFactory>()
            .AddUserValidator<CustomUserNameValidator>()
            .AddSignInManager();

        builder.Services.AddTransient<IIdentityService, IdentityService>();
        builder.Services.AddTransient<ITokenService, JwtTokenService>();

        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(Policies.CanDeletePlayers, policy => policy.RequireRole(Roles.Administrator));
            options.AddPolicy(Policies.CanReadPlayers, policy => policy.RequireRole(Roles.Administrator));
        });
    }
}

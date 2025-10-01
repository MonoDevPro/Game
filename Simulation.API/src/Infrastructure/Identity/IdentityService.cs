using System.Security.Claims;
using GameWeb.Application.Common;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Common.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;

namespace GameWeb.Infrastructure.Identity;

/// <summary>
/// Provides services for managing user identity, roles, and claims.
/// </summary>
public class IdentityService(
    UserManager<ApplicationUser> userManager,
    IUserClaimsPrincipalFactory<ApplicationUser> userClaimsPrincipalFactory,
    IAuthorizationService authorizationService)
    : IIdentityService
{
    /// <inheritdoc />
    public async Task<string?> GetUserNameAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user?.UserName;
    }

    /// <inheritdoc />
    public async Task<(Result Result, string UserId)> CreateUserAsync(string userName, string email, string password, CancellationToken cancellationToken = default)
    {
        var user = new ApplicationUser
        {
            UserName = userName,
            Email = email,
        };

        // A cancellationToken é passada para os métodos do UserManager, se eles a suportarem.
        // O CreateAsync padrão não suporta, mas em implementações customizadas poderia.
        var result = await userManager.CreateAsync(user, password);

        return (result.ToApplicationResult(), user.Id);
    }

    /// <inheritdoc />
    public async Task<bool> IsInRoleAsync(string userId, string role, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        return user != null && await userManager.IsInRoleAsync(user, role);
    }

    /// <inheritdoc />
    public async Task<bool> AuthorizeAsync(string userId, string policyName, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return false;

        var principal = await userClaimsPrincipalFactory.CreateAsync(user);
        var result = await authorizationService.AuthorizeAsync(principal, policyName);

        return result.Succeeded;
    }

    /// <inheritdoc />
    public async Task<Result> DeleteUserAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        
        // Operação idempotente: se o usuário não existe, o estado desejado (não existir) já foi alcançado.
        return user != null ? await DeleteUserAsync(user, cancellationToken) : Result.Success();
    }
    
    public async Task<Result> DeleteUserAsync(ApplicationUser user, CancellationToken cancellationToken = default)
    {
        var result = await userManager.DeleteAsync(user);
        return result.ToApplicationResult();
    }
}

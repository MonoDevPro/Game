using System.Security.Claims;
using GameWeb.Application.Common;
using GameWeb.Application.Common.Interfaces;
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
    
    /// <inheritdoc />
    public async Task<string?> GetClaimValueAsync(string userId, string claimType, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null) return null;

        var claims = await userManager.GetClaimsAsync(user);
        return claims.FirstOrDefault(c => c.Type == claimType)?.Value;
    }

    /// <summary>
    /// Adds or replaces a claim for a specified user.
    /// If a claim with the same type already exists, it will be removed before the new one is added.
    /// </summary>
    public async Task<Result> SetClaimAsync(string userId, string claimType, string claimValue, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
            return Result.Failure([$"User with ID '{userId}' not found."]);

        // Primeiro, removemos claims existentes do mesmo tipo para evitar duplicatas.
        var existingClaims = await userManager.GetClaimsAsync(user);
        var claimToRemove = existingClaims.FirstOrDefault(c => c.Type == claimType);
        
        if (claimToRemove != null)
        {
            var removeResult = await userManager.RemoveClaimAsync(user, claimToRemove);
            if (!removeResult.Succeeded)
                return removeResult.ToApplicationResult();
        }

        var addResult = await userManager.AddClaimAsync(user, new Claim(claimType, claimValue));
        return addResult.ToApplicationResult();
    }

    /// <inheritdoc />
    public async Task<Result> RemoveClaimAsync(string userId, string claimType, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            // Idempotente: se o usuário não existe, a claim também não.
            return Result.Success();
        }

        var claims = await userManager.GetClaimsAsync(user);
        var claimToRemove = claims.FirstOrDefault(c => c.Type == claimType);
        
        if (claimToRemove != null)
        {
            var result = await userManager.RemoveClaimAsync(user, claimToRemove);
            return result.ToApplicationResult();
        }

        // Idempotente: se a claim não existe, o estado desejado já foi alcançado.
        return Result.Success();
    }
    
    /// <inheritdoc />
    public async Task<IList<Claim>> GetUserClaimsAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user == null)
        {
            return new List<Claim>();
        }

        return await userManager.GetClaimsAsync(user);
    }
}

// Nota: A interface IIdentityService também precisaria ser atualizada
// para incluir os parâmetros CancellationToken.

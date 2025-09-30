using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Application.Abstractions.Options;
using GameWeb.Application.Common.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace GameWeb.Infrastructure.Identity;

public class JwtTokenService(
    IOptions<JwtOptions> options,
    UserManager<ApplicationUser> userManager,
    IUserClaimsPrincipalFactory<ApplicationUser> claimsFactory,
    TimeProvider timeProvider) : ITokenService
{
    private const string DevFallbackKey = "dev_secret_change_me";

    public async Task<string> IssueAccessTokenAsync(string userId, CancellationToken cancellationToken = default)
    {
        var user = await userManager.FindByIdAsync(userId);
        if (user is null)
        {
            throw new InvalidOperationException($"User not found: {userId}");
        }

        var principal = await claimsFactory.CreateAsync(user);
        var claims = principal.Claims.ToList();

        // Ensure standard registered claims
        if (!claims.Any(c => c.Type == JwtRegisteredClaimNames.Sub))
            claims.Add(new Claim(JwtRegisteredClaimNames.Sub, user.Id));
        if (!claims.Any(c => c.Type == JwtRegisteredClaimNames.UniqueName))
            claims.Add(new Claim(JwtRegisteredClaimNames.UniqueName, user.UserName ?? user.Email ?? user.Id));
        if (!claims.Any(c => c.Type == ClaimTypes.NameIdentifier))
            claims.Add(new Claim(ClaimTypes.NameIdentifier, user.Id));
        if (!claims.Any(c => c.Type == ClaimTypes.Name) && !string.IsNullOrEmpty(user.UserName))
            claims.Add(new Claim(ClaimTypes.Name, user.UserName!));
        if (!claims.Any(c => c.Type == ClaimTypes.Email) && !string.IsNullOrEmpty(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email!));

        // Add role claims explicitly to token
        var roles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            if (!claims.Any(c => c.Type == ClaimTypes.Role && c.Value == role))
                claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var keyStr = string.IsNullOrWhiteSpace(options.Value.Key) ? DevFallbackKey : options.Value.Key;
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var now = timeProvider.GetUtcNow().UtcDateTime;
        var expires = now.AddMinutes(options.Value.AccessTokenLifetimeMinutes);

        var token = new JwtSecurityToken(
            issuer: options.Value.Issuer,
            audience: options.Value.Audience,
            claims: claims,
            notBefore: now,
            expires: expires,
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}

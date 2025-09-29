using System.Security.Claims;
using Application.Models;
using Application.Models.Models;
using GameWeb.Application.Auth.Commands.Register;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace GameWeb.Web.Endpoints;

public class Auth : EndpointGroupBase
{
    public override string? GroupName => "Auth";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapPost("/register", Register)
            .AllowAnonymous()
            .WithSummary("Register a new user and receive a JWT access token");

        group.MapPost("/login", Login)
            .AllowAnonymous()
            .WithSummary("Login with username or email and password to receive a JWT access token");

        group.MapGet("/me", Me)
            .RequireAuthorization()
            .WithSummary("Get info about the current authenticated user");

        group.MapPost("/refresh", Refresh)
            .RequireAuthorization()
            .WithSummary("Issue a fresh JWT for the current user (e.g., after selecting an active character)");
    }

    private async Task<IResult> Register(
        RegisterCommand request,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(request, ct);
        return TypedResults.Ok(result);
    }

    private async Task<IResult> Login(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager,
        ITokenService tokenService,
        CancellationToken ct)
    {
        ApplicationUser? user = await userManager.FindByNameAsync(request.UserNameOrEmail);
        if (user is null)
            user = await userManager.FindByEmailAsync(request.UserNameOrEmail);
        if (user is null)
            return TypedResults.Unauthorized();

        var signIn = await signInManager.CheckPasswordSignInAsync(user, request.Password, lockoutOnFailure: false);
        if (!signIn.Succeeded)
            return TypedResults.Unauthorized();

        var token = await tokenService.IssueAccessTokenAsync(user.Id, ct);
        return TypedResults.Ok(new AuthResponse(token));
    }

    private IResult Me(ClaimsPrincipal user)
    {
        var id = user.FindFirstValue(ClaimTypes.NameIdentifier);
        var name = user.Identity?.Name ?? user.FindFirstValue(ClaimTypes.Name) ?? user.FindFirstValue("unique_name");
        var email = user.FindFirstValue(ClaimTypes.Email);
        var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

        return TypedResults.Ok(new
        {
            id,
            name,
            email,
            roles
        });
    }

    private async Task<IResult> Refresh(
        ClaimsPrincipal user,
        ITokenService tokenService,
        CancellationToken ct)
    {
        var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
            return TypedResults.Unauthorized();

        var token = await tokenService.IssueAccessTokenAsync(userId, ct);
        return TypedResults.Ok(new AuthResponse(token));
    }
}

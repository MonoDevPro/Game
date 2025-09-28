using System.Security.Claims;
using Application.Models;
using Application.Models.Models;
using Application.Models.Options;
using GameWeb.Application.Auth.Commands.Register;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace GameWeb.Web.Endpoints;

public class Options : EndpointGroupBase
{
    public override string? GroupName => "Options";

    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetOptions)
            .AllowAnonymous()
            .WithSummary("Register a new user and receive a JWT access token");
    }

    private Task<ConfigDto> GetOptions(
        IOptions<AuthorityOptions> authority,
        IOptions<NetworkOptions> network,
        IOptions<WorldOptions> world,
        CancellationToken ct)
    {
        var configDto = new ConfigDto(
            authority.Value,
            network.Value,
            world.Value);

        return Task.FromResult<ConfigDto>(configDto);
    }
}

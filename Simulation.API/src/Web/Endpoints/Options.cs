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
    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetClientOptions,"/client")
            .AllowAnonymous()
            .WithSummary("Get the client configuration options.");
    }

    private Task<IResult> GetClientOptions(
        IOptions<AuthorityOptions> authority,
        IOptions<NetworkOptions> network,
        IOptions<WorldOptions> world,
        CancellationToken ct)
    { 
        var configDto = new ConfigDto(
            authority.Value,
            network.Value,
            world.Value);

        return Task.FromResult<IResult>(TypedResults.Ok(configDto));
    }
}

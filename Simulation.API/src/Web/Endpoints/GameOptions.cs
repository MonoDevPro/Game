using System.Security.Cryptography;
using GameWeb.Application.Common.Options;
using GameWeb.Application.Maps.Models;
using GameWeb.Application.Maps.Queries;
using Microsoft.Extensions.Options;
using MemoryPack;

namespace GameWeb.Web.Endpoints;

public class Options : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetOptions)
            .AllowAnonymous()
            .WithSummary("Get the configuration options.");
    }

    private Task<IResult> GetOptions(
        IOptions<NetworkOptions> network,
        IOptions<WorldOptions> world,
        IOptions<MapOptions> map,
        CancellationToken ct)
    {
        var configDto = new OptionsDto(network.Value, world.Value, map.Value);
        return Task.FromResult<IResult>(TypedResults.Ok(configDto));
    }
}

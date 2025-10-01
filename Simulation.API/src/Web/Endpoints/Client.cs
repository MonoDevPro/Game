using System.Security.Cryptography;
using GameWeb.Application.Common.Options;
using GameWeb.Application.Maps.Models;
using GameWeb.Application.Maps.Queries;
using Microsoft.Extensions.Options;
using MemoryPack;

namespace GameWeb.Web.Endpoints;

public class Client : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetClientOptions, "/options")
            .AllowAnonymous()
            .WithSummary("Get the client configuration options.");

        group.MapGet(GetMapMeta, "/map/{id:int}/meta")
            .AllowAnonymous()
            .WithSummary("Get map metadata (size, format, checksum).");

        group.MapGet(GetMapBinary, "/map/{id:int}")
            .AllowAnonymous()
            .WithSummary("Download map as MemoryPack binary (application/octet-stream).");
    }

    private Task<IResult> GetClientOptions(
        IOptions<AuthorityOptions> authority,
        IOptions<NetworkOptions> network,
        IOptions<WorldOptions> world,
        CancellationToken ct)
    {
        var configDto = new OptionsDto(authority.Value, network.Value, world.Value);
        return Task.FromResult<IResult>(TypedResults.Ok(configDto));
    }

    // --- map metadata ---
    private async Task<IResult> GetMapMeta(
        int id,
        ISender sender,
        CancellationToken ct)
    {
        var data = await sender.Send<MapDto>(new GetMapQuery(id), ct);
        // compute checksum quickly (e.g., SHA256 of binary MemoryPack)
        var bytes = MemoryPackSerializer.Serialize(data);
        var sha = ComputeSha256(bytes);
        var meta = new
        {
            data.Id,
            data.Name,
            data.Width,
            data.Height,
            Format = "memorypack",
            Size = bytes.Length,
            Checksum = sha,
            ETag = $"\"{sha}\""
        };
        return TypedResults.Ok(meta);
    }

    // --- download binary (MemoryPack) ---
    private async Task<IResult> GetMapBinary(
        int id,
        ISender sender,
        HttpRequest req,
        CancellationToken ct)
    {
        var data = await sender.Send<MapDto>(new GetMapQuery(id), ct);

        var bytes = MemoryPackSerializer.Serialize(data);
        var sha = ComputeSha256(bytes);
        var etag = $"\"{sha}\"";

        // If-None-Match handling (cache)
        if (req.Headers.TryGetValue("If-None-Match", out var ifNone) && ifNone.ToString().Contains(sha))
        {
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);
        }

        var headers = new HeaderDictionary
        {
            ["Content-Type"] = "application/octet-stream",
            ["Content-Length"] = bytes.Length.ToString(),
            ["ETag"] = etag,
            ["Cache-Control"] = "public, max-age=3600"
        };

        return TypedResults.File(bytes, "application/octet-stream", $"map-{id}.mpack", enableRangeProcessing: false);
    }

    // helper
    private static string ComputeSha256(byte[] bytes)
    {
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}

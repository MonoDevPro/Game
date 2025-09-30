using System.Security.Cryptography;
using Application.Abstractions.Options;
using GameWeb.Application.Common.Interfaces;
using GameWeb.Application.Maps.Services;
using MemoryPack;
using Microsoft.Extensions.Options;

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
        var configDto = new ConfigDto(authority.Value, network.Value, world.Value);
        return Task.FromResult<IResult>(TypedResults.Ok(configDto));
    }

    // --- map metadata ---
    private async Task<IResult> GetMapMeta(
        int id,
        IMapRepository maps,          // injetar seu serviço/repositório real
        CancellationToken ct)
    {
        var map = await maps.GetMapAsync(id, ct);
        if (map is null) return TypedResults.NotFound();
        var data = map;

        // compute checksum quickly (e.g., SHA256 of binary MemoryPack)
        var bytes = MemoryPackSerializer.Serialize(map);
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
        HttpRequest req,
        IMapRepository maps,
        CancellationToken ct)
    {
        var map = await maps.GetMapAsync(id, ct);
        if (map is null) return TypedResults.NotFound();

        var bytes = MemoryPackSerializer.Serialize(map);
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

using System.Security.Cryptography;
using GameWeb.Application.Common.Options;
using GameWeb.Application.Maps.Models;
using GameWeb.Application.Maps.Queries;
using MemoryPack;
using Microsoft.Extensions.Options;

namespace GameWeb.Web.Endpoints;

public class Maps : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder group)
    {
        group.MapGet(GetMapMeta, "/{id:int}/meta")
            .AllowAnonymous()
            .WithSummary("Get map metadata (size, format, checksum).");

        group.MapGet(GetMapBinary, "/{id:int}")
            .AllowAnonymous()
            .WithSummary("Download map as MemoryPack binary (application/octet-stream).");
    }

    // --- map metadata ---
    private async Task<IResult> GetMapMeta(int id, ISender sender, HttpRequest req, CancellationToken ct)
    {
        var data = await sender.Send<MapDto>(new GetMapQuery(id), ct);
        var bytes = MemoryPackSerializer.Serialize(data);
        var sha = ComputeSha256(bytes);
        var etag = $"\"{sha}\"";

        // adiciona header ETag na resposta
        req.HttpContext.Response.Headers["ETag"] = etag;
        req.HttpContext.Response.Headers["Cache-Control"] = "public, max-age=3600";

        var meta = new MapMetaDto(
            data.Id,
            data.Name,
            data.Width,
            data.Height,
            "memorypack",
            bytes.Length,
            sha,
            etag);

        return TypedResults.Ok(meta);
    }

    // --- download binary (MemoryPack) ---
    private async Task<IResult> GetMapBinary(
        int id,
        ISender sender,
        HttpRequest req,
        CancellationToken ct)
    {
        if (id < 1)
            return TypedResults.NotFound($"Map ID {id} is out of range.");
        
        var data = await sender.Send<MapDto>(new GetMapQuery(id), ct);

        var bytes = MemoryPackSerializer.Serialize(data);
        var sha = ComputeSha256(bytes);
        var etag = $"\"{sha}\"";

        // If-None-Match handling (cache)
        if (req.Headers.TryGetValue("If-None-Match", out var ifNone) && ifNone.ToString().Contains(sha))
            return TypedResults.StatusCode(StatusCodes.Status304NotModified);

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

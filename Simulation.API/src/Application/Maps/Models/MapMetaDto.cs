namespace GameWeb.Application.Maps.Models;

// DTO para metadata
public record MapMetaDto(
    int Id,
    string Name,
    int Width,
    int Height,
    string Format,
    int Size,
    string Checksum,
    string ETag
);

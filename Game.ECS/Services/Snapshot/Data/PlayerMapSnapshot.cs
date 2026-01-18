using Game.DTOs.Map;
using MemoryPack;

namespace Game.ECS.Services.Snapshot.Data;

[MemoryPackable]
public readonly partial record struct PlayerMapSnapshot(
    MapMetadataDto MapData);
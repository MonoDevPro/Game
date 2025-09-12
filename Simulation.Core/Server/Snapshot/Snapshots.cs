using MemoryPack;
using Simulation.Abstractions.Network;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Templates;

namespace Simulation.Core.Server.Snapshot;

[MemoryPackable]
public partial record struct MapSnapshotPacket : IPacket
{
    public int PlayerId { get; set; }
    public int MapId { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    // raw tile data (serialize as you keep it); null or empty if not applicable
    public TileType[]? Tiles { get; set; }
    public PlayerSnapshot[] Players { get; set; }
}

[MemoryPackable]
public partial record struct PlayerSnapshot
{
    public int PlayerId { get; set; }
    public string Name { get; set; }
    public Gender Gender { get; set; }
    public Vocation Vocation { get; set; }
    public Position Position { get; set; }
    public Direction Direction { get; set; }
    public Health Health { get; set; }
    public MoveStats MoveStats { get; set; }
    public AttackStats AttackStats { get; set; }
}
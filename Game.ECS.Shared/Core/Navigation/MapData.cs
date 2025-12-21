using Game.ECS.Shared.Components.Navigation;
using MemoryPack;

namespace Game.ECS.Shared.Core.Navigation;

/// <summary>
/// Dados completos do mapa para serialização. 
/// </summary>
[MemoryPackable]
public partial class MapData
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public float CellSize { get; set; } = 1f;
    
    /// <summary>
    /// Walkability data (RLE encoded para economia de espaço).
    /// </summary>
    public byte[] WalkabilityData { get; set; } = Array.Empty<byte>();
    
    /// <summary>
    /// Spawns do mapa.
    /// </summary>
    public SpawnPointData[] SpawnPoints { get; set; } = Array.Empty<SpawnPointData>();
    
    /// <summary>
    /// Zonas especiais (safe zones, pvp, etc).
    /// </summary>
    public ZoneData[] Zones { get; set; } = Array.Empty<ZoneData>();
    
    /// <summary>
    /// Portais/Teleportes.
    /// </summary>
    public PortalData[] Portals { get; set; } = Array.Empty<PortalData>();
    
    /// <summary>
    /// Metadados customizados.
    /// </summary>
    public Dictionary<string, string> Metadata { get; set; } = new();

    /// <summary>
    /// Versão do mapa (para cache/invalidação).
    /// </summary>
    public uint Version { get; set; }
    
    /// <summary>
    /// Checksum para validação de integridade.
    /// </summary>
    public uint Checksum { get; set; }
}

[MemoryPackable]
public partial struct SpawnPointData
{
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public SpawnType Type { get; set; }
    public string Tag { get; set; } // Ex: "player_start", "boss_1", "mob_group_a"
    
    public SpawnPointData(int x, int y, SpawnType type, string tag = "")
    {
        X = (ushort)x;
        Y = (ushort)y;
        Type = type;
        Tag = tag;
    }
    
    public GridPosition ToGridPosition() => new(X, Y);
}

public enum SpawnType :  byte
{
    Player = 0,
    Monster = 1,
    Npc = 2,
    Boss = 3,
    Item = 4,
    Event = 5
}

[MemoryPackable]
public partial struct ZoneData
{
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }
    public ZoneType Type { get; set; }
    public string Tag { get; set; }
    
    public ZoneData(int x, int y, int width, int height, ZoneType type, string tag = "")
    {
        X = (ushort)x;
        Y = (ushort)y;
        Width = (ushort)width;
        Height = (ushort)height;
        Type = type;
        Tag = tag;
    }
    
    public bool Contains(int px, int py) 
        => px >= X && px < X + Width && py >= Y && py < Y + Height;
    
    public bool Contains(GridPosition pos) => Contains(pos.X, pos.Y);
}

public enum ZoneType :  byte
{
    SafeZone = 0,
    PvpZone = 1,
    RestrictedZone = 2,
    BossRoom = 3,
    SecretArea = 4,
    Trigger = 5
}

[MemoryPackable]
public partial struct PortalData
{
    public ushort X { get; set; }
    public ushort Y { get; set; }
    public string TargetMapId { get; set; }
    public ushort TargetX { get; set; }
    public ushort TargetY { get; set; }
    public bool RequiresInteraction { get; set; }
    public string Tag { get; set; }
    
    public PortalData(int x, int y, string targetMapId, int targetX, int targetY, 
        bool requiresInteraction = false, string tag = "")
    {
        X = (ushort)x;
        Y = (ushort)y;
        TargetMapId = targetMapId;
        TargetX = (ushort)targetX;
        TargetY = (ushort)targetY;
        RequiresInteraction = requiresInteraction;
        Tag = tag;
    }
    
    public GridPosition Position => new(X, Y);
    public GridPosition TargetPosition => new(TargetX, TargetY);
}
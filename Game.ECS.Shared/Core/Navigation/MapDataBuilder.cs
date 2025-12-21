namespace Game.ECS. Shared.Core. Navigation;

/// <summary>
/// Builder fluent para criar MapData.
/// </summary>
public class MapDataBuilder
{
    private readonly MapData _map = new();
    private readonly List<SpawnPointData> _spawns = new();
    private readonly List<ZoneData> _zones = new();
    private readonly List<PortalData> _portals = new();

    public MapDataBuilder WithId(string id)
    {
        _map.Id = id;
        return this;
    }

    public MapDataBuilder WithName(string name)
    {
        _map.Name = name;
        return this;
    }

    public MapDataBuilder WithSize(int width, int height, float cellSize = 1f)
    {
        _map.Width = (ushort)width;
        _map.Height = (ushort)height;
        _map. CellSize = cellSize;
        return this;
    }

    public MapDataBuilder WithGrid(NavigationGrid grid)
    {
        _map.Width = (ushort)grid.Width;
        _map.Height = (ushort)grid.Height;
        _map.CellSize = grid. CellSize;
        _map.WalkabilityData = RleEncode(grid.ToBytes());
        return this;
    }

    public MapDataBuilder AddSpawn(int x, int y, SpawnType type, string tag = "")
    {
        _spawns.Add(new SpawnPointData(x, y, type, tag));
        return this;
    }

    public MapDataBuilder AddZone(int x, int y, int width, int height, 
        ZoneType type, string tag = "")
    {
        _zones. Add(new ZoneData(x, y, width, height, type, tag));
        return this;
    }

    public MapDataBuilder AddPortal(int x, int y, string targetMapId, 
        int targetX, int targetY, bool requiresInteraction = false, string tag = "")
    {
        _portals.Add(new PortalData(x, y, targetMapId, targetX, targetY, 
            requiresInteraction, tag));
        return this;
    }

    public MapDataBuilder WithMetadata(string key, string value)
    {
        _map.Metadata[key] = value;
        return this;
    }

    public MapDataBuilder WithVersion(uint version)
    {
        _map.Version = version;
        return this;
    }

    public MapData Build()
    {
        _map.SpawnPoints = _spawns.ToArray();
        _map. Zones = _zones.ToArray();
        _map. Portals = _portals.ToArray();
        
        // Garante walkability data vazio se não foi definido
        if (_map.WalkabilityData. Length == 0 && _map.Width > 0 && _map.Height > 0)
        {
            // Grid totalmente walkable por padrão
            var fullWalkable = new byte[_map. Width * _map. Height];
            Array.Fill(fullWalkable, (byte)1);
            _map.WalkabilityData = RleEncode(fullWalkable);
        }

        return _map;
    }

    private static byte[] RleEncode(byte[] data)
    {
        using var ms = new MemoryStream();
        int i = 0;
        while (i < data.Length)
        {
            byte value = data[i];
            int count = 1;
            while (i + count < data.Length && data[i + count] == value && count < 255)
                count++;
            ms.WriteByte((byte)count);
            ms.WriteByte(value);
            i += count;
        }
        return ms. ToArray();
    }
}
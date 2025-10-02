namespace GameWeb.Application.Common.Options;

public class MapOptions
{
    public const string SectionName = "Map";
    
    public int MapCount => Maps.Length;
    public MapInfo[] Maps { get; set; } = [];
    
    public override string ToString()
    {
        return $"MapCount={MapCount}";
    }
}

public class MapInfo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Width { get; set;  }
    public int Height { get; set; }
    public bool UsePadded { get; set; }
    public bool BorderBlocked { get; set; }
}

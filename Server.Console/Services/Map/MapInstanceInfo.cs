namespace Server.Console.Services.Map;

public class MapInstanceInfo
{
    public const string SectionName = "MapInstance";
    
    public int MapId { get; set; } = 1;
    
    public override string ToString()
    {
        return $"MapId={MapId}";
    }
}

namespace GameWeb.Application.Common.Options;
public class WorldOptions
{
    public const string SectionName = "World";
    
    public int ChunkSizeInBytes { get; set; } = 16_384;
    public int MinimumAmountOfEntitiesPerChunk { get; set; } = 100;
    public int ArchetypeCapacity { get; set; } = 2;
    public int EntityCapacity { get; set; } = 64;
    
    public override string ToString()
    {
        return $"ChunkSizeInBytes={ChunkSizeInBytes}, " +
               $"MinimumAmountOfEntitiesPerChunk={MinimumAmountOfEntitiesPerChunk}, " +
               $"ArchetypeCapacity={ArchetypeCapacity}, " +
               $"EntityCapacity={EntityCapacity}";
    }
}

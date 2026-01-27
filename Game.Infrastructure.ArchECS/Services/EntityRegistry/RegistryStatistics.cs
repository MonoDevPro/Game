namespace Game.Infrastructure.ArchECS.Services.EntityRegistry;

public struct RegistryStatistics
{
    public int TotalEntities;
    public Dictionary<EntityDomain, int> DomainCounts;

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"Total Entities: {TotalEntities}");
        sb.AppendLine("Domain Statistics:");
        
        foreach (var kvp in DomainCounts.OrderByDescending(x => x.Value))
        {
            sb.AppendLine($"  {kvp.Key}: {kvp.Value} entities");
        }
        
        return sb.ToString();
    }
}
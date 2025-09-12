namespace Simulation.Core.Shared.Options;

public class NetworkOptions
{
    public static string SectionName = "Network";
    
    public string ServerAddress { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 7777;
    public string ConnectionKey { get; set; } = "default_key";
    
    
    public override string ToString()
    {
        return $"ServerAddress={ServerAddress}, " +
               $"ServerPort={ServerPort}, ConnectionKey={ConnectionKey}]";
    }
}
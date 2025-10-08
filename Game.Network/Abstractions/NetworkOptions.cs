namespace Game.Abstractions.Network;

public class NetworkOptions
{
    public bool IsServer = true;
    public string ServerAddress { get; set; } = "127.0.0.1";
    public int ServerPort { get; set; } = 7777;
    public string ConnectionKey { get; set; } = "default_key";
    public int PingIntervalMs { get; set; } = 2000; // em milissegundos
    public int DisconnectTimeoutMs { get; set; } = 5000; // em milissegundos
    
    public override string ToString()
    {
        return $"ServerAddress={ServerAddress}, " +
               $"ServerPort={ServerPort}, ConnectionKey={ConnectionKey}, " +
               $"DisconnectTimeoutMs={DisconnectTimeoutMs}";
    }
}

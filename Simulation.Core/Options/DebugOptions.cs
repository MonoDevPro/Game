namespace Simulation.Core.Options;

public class DebugOptions
{
    public static string SectionName = "Debug";
    
    public bool EnablePacketDebugging { get; set; } = false;
    public bool LogPacketContents { get; set; } = false;
    public bool LogPacketTiming { get; set; } = false;
    public bool LogPacketErrors { get; set; } = true;
    public DebugLevel PacketDebugLevel { get; set; } = DebugLevel.Info;
    
    public override string ToString()
    {
        return $"EnablePacketDebugging={EnablePacketDebugging}, " +
               $"LogPacketContents={LogPacketContents}, " +
               $"LogPacketTiming={LogPacketTiming}, " +
               $"LogPacketErrors={LogPacketErrors}, " +
               $"PacketDebugLevel={PacketDebugLevel}";
    }
}

public enum DebugLevel
{
    None = 0,
    Error = 1,
    Warning = 2,
    Info = 3,
    Verbose = 4
}
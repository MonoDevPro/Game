using System.Diagnostics;
using Arch.Core;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared.Options;

namespace Simulation.Core.Shared.Network;

public static class DebugPacketProcessor
{
    private static DebugOptions? _debugOptions;
    private static readonly Dictionary<string, int> PacketCounts = new();
    private static readonly Dictionary<string, long> PacketTimings = new();
    
    public static void Initialize(DebugOptions debugOptions)
    {
        _debugOptions = debugOptions;
        LogDebug("DebugPacketProcessor initialized", DebugLevel.Info);
    }

    public static void Process(World world, PlayerIndexSystem playerIndex, LiteNetLib.NetPacketReader reader)
    {
        if (_debugOptions?.EnablePacketDebugging != true)
        {
            // Use original processor when debugging is disabled
            PacketProcessor.Process(world, playerIndex, reader);
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        var availableBytes = reader.AvailableBytes;

        try
        {
            if (availableBytes == 0)
            {
                LogDebug("Received empty packet", DebugLevel.Verbose);
                return;
            }

            // Peek at packet type without advancing position
            var packetType = (PacketType)reader.PeekByte();
            var packetTypeName = packetType.ToString();

            LogDebug($"Processing packet: {packetTypeName} ({availableBytes} bytes)", DebugLevel.Info);

            if (_debugOptions.LogPacketContents)
            {
                LogPacketContents(reader, packetTypeName);
            }

            // Process normally
            PacketProcessor.Process(world, playerIndex, reader);

            stopwatch.Stop();

            // Track statistics
            UpdatePacketStatistics(packetTypeName, stopwatch.ElapsedTicks);

            if (_debugOptions.LogPacketTiming)
            {
                LogDebug($"Packet {packetTypeName} processed in {stopwatch.ElapsedMilliseconds}ms", DebugLevel.Verbose);
            }

            LogDebug($"Successfully processed packet: {packetTypeName}", DebugLevel.Verbose);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            
            if (_debugOptions.LogPacketErrors)
            {
                LogError($"Error processing packet: {ex.Message}", ex);
            }

            throw; // Re-throw to maintain existing error behavior
        }
    }

    private static void LogPacketContents(LiteNetLib.NetPacketReader reader, string packetTypeName)
    {
        try
        {
            var remainingBytes = reader.AvailableBytes;
            LogDebug($"Packet {packetTypeName} has {remainingBytes} bytes remaining", DebugLevel.Verbose);
        }
        catch (Exception ex)
        {
            LogError($"Failed to log packet contents for {packetTypeName}: {ex.Message}", ex);
        }
    }

    private static void UpdatePacketStatistics(string packetType, long elapsedTicks)
    {
        PacketCounts[packetType] = PacketCounts.GetValueOrDefault(packetType, 0) + 1;
        PacketTimings[packetType] = PacketTimings.GetValueOrDefault(packetType, 0L) + elapsedTicks;
    }

    public static void LogStatistics()
    {
        if (_debugOptions?.EnablePacketDebugging != true) return;

        LogDebug("=== Packet Processing Statistics ===", DebugLevel.Info);
        
        foreach (var kvp in PacketCounts.OrderByDescending(x => x.Value))
        {
            var packetType = kvp.Key;
            var count = kvp.Value;
            var totalTicks = PacketTimings.GetValueOrDefault(packetType, 0L);
            var avgTicks = count > 0 ? totalTicks / count : 0;
            var avgMs = TimeSpan.FromTicks(avgTicks).TotalMilliseconds;

            LogDebug($"{packetType}: {count} packets, avg {avgMs:F2}ms", DebugLevel.Info);
        }
        
        LogDebug("====================================", DebugLevel.Info);
    }

    public static void ClearStatistics()
    {
        PacketCounts.Clear();
        PacketTimings.Clear();
        LogDebug("Packet statistics cleared", DebugLevel.Info);
    }

    private static void LogDebug(string message, DebugLevel level)
    {
        if (_debugOptions == null || level > _debugOptions.PacketDebugLevel) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper();
        Console.WriteLine($"[{timestamp}] [{levelStr}] [PKT] {message}");
    }

    private static void LogError(string message, Exception? exception = null)
    {
        if (_debugOptions?.LogPacketErrors != true) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        Console.WriteLine($"[{timestamp}] [ERROR] [PKT] {message}");
        
        if (exception != null)
        {
            Console.WriteLine($"[{timestamp}] [ERROR] [PKT] Exception: {exception}");
        }
    }
}
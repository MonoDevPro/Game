using System.Diagnostics;
using Arch.Core;
using LiteNetLib;
using Simulation.Core.ECS.Server.Systems.Indexes;
using Simulation.Core.Options;
using Simulation.Generated.Network;

namespace Simulation.Core.Network;

public static class DebugPacketProcessor
{
    private static DebugOptions? _debugOptions;
    private static readonly Dictionary<string, int> PacketCounts = new();
    private static readonly Dictionary<string, long> PacketTimings = new();
    private static readonly object FileLock = new object();
    private static string? _logFilePath;
    
    public static void Initialize(DebugOptions debugOptions)
    {
        _debugOptions = debugOptions;
        
        // Set up log file path for server debug logging
        _logFilePath = Path.Combine(Directory.GetCurrentDirectory(), "server_console_debug.log");
        
        LogDebug("DebugPacketProcessor initialized", DebugLevel.Info);
        LogDebug($"Debug log file: {_logFilePath}", DebugLevel.Info);
    }

    public static void Process(World world, IPlayerIndex playerIndex, NetPacketReader reader)
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
        var logMessage = $"[{timestamp}] [{levelStr}] [PKT] {message}";
        
        // Always write to console
        Console.WriteLine(logMessage);
        
        // Also write to log file if path is set
        WriteToLogFile(logMessage);
    }

    private static void LogError(string message, Exception? exception = null)
    {
        if (_debugOptions?.LogPacketErrors != true) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var logMessage = $"[{timestamp}] [ERROR] [PKT] {message}";
        
        // Write to console
        Console.WriteLine(logMessage);
        WriteToLogFile(logMessage);
        
        if (exception != null)
        {
            var exceptionMessage = $"[{timestamp}] [ERROR] [PKT] Exception: {exception}";
            Console.WriteLine(exceptionMessage);
            WriteToLogFile(exceptionMessage);
        }
    }

    public static void LogConnectionEvent(string message, bool isConnection)
    {
        if (_debugOptions?.EnablePacketDebugging != true) return;
        
        var eventType = isConnection ? "CONNECT" : "DISCONNECT";
        LogDebug($"[{eventType}] {message}", DebugLevel.Info);
    }

    private static void WriteToLogFile(string message)
    {
        if (string.IsNullOrEmpty(_logFilePath)) return;

        try
        {
            lock (FileLock)
            {
                File.AppendAllText(_logFilePath, message + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            // Don't let file logging errors break the application
            Console.WriteLine($"[WARNING] Failed to write to debug log file: {ex.Message}");
        }
    }
}
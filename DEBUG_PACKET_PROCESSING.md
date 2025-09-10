# Debug Packet Processing

This document describes the debug packet processing functionality that has been added to the game networking system.

## Overview

The debug packet processing system provides comprehensive logging, monitoring, and error handling for network packet processing. It wraps the existing generated `PacketProcessor` with additional debugging capabilities that can be enabled/disabled as needed.

## Features

- **Configurable debug levels**: Control verbosity of debug output
- **Packet processing timing**: Measure and log packet processing performance
- **Error handling**: Catch and log packet processing errors
- **Statistics tracking**: Track packet counts and processing times
- **Content logging**: Optional logging of packet contents for deep debugging

## Configuration

Debug packet processing is configured through the `DebugOptions` class:

```csharp
var debugOptions = new DebugOptions
{
    EnablePacketDebugging = true,        // Enable/disable debug functionality
    LogPacketContents = false,           // Log packet content details (verbose)
    LogPacketTiming = true,              // Log packet processing times
    LogPacketErrors = true,              // Log packet processing errors
    PacketDebugLevel = DebugLevel.Info   // Control log verbosity
};
```

### Debug Levels

- `None`: No debug output
- `Error`: Only error messages
- `Warning`: Errors and warnings
- `Info`: Errors, warnings, and informational messages
- `Verbose`: All debug output including detailed packet information

## Usage

### 1. Initialize Debug Functionality

```csharp
// Create and configure debug options
var debugOptions = new DebugOptions { ... };

// Initialize the NetworkManager
var networkManager = new NetworkManager(world, playerIndexSystem);

// Enable debug functionality
networkManager.InitializeDebug(debugOptions);
```

### 2. Monitor Packet Statistics

```csharp
// Log current packet statistics
networkManager.LogPacketStatistics();

// Clear accumulated statistics
networkManager.ClearPacketStatistics();
```

### 3. Example Integration (Server)

```csharp
Console.Title = "SERVER";
var world = World.Create();

// Configure debug options
var debugOptions = new DebugOptions
{
    EnablePacketDebugging = true,
    LogPacketTiming = true,
    PacketDebugLevel = DebugLevel.Info
};

var playerIndexSystem = new PlayerIndexSystem(world);
var networkManager = new NetworkManager(world, playerIndexSystem);

// Initialize debug functionality
networkManager.InitializeDebug(debugOptions);

// Start server and systems...
networkManager.StartServer(7777, "MyConnectionKey");

// Periodic statistics logging
var statsTimer = 0;
while (true) {
    networkManager.PollEvents();
    systems.Update(0.016f);
    
    // Log statistics every 10 seconds
    statsTimer++;
    if (statsTimer >= 666) {
        networkManager.LogPacketStatistics();
        statsTimer = 0;
    }
    
    Thread.Sleep(15);
}
```

## Debug Output Examples

### Initialization
```
[2025-09-10 00:51:39.452] [INFO] [PKT] DebugPacketProcessor initialized
```

### Packet Processing
```
[2025-09-10 00:51:40.123] [INFO] [PKT] Processing packet: MoveIntentUpdate (24 bytes)
[2025-09-10 00:51:40.124] [VERBOSE] [PKT] Packet MoveIntentUpdate processed in 1.2ms
[2025-09-10 00:51:40.124] [VERBOSE] [PKT] Successfully processed packet: MoveIntentUpdate
```

### Statistics Report
```
[2025-09-10 00:51:49.538] [INFO] [PKT] === Packet Processing Statistics ===
[2025-09-10 00:51:49.540] [INFO] [PKT] MoveIntentUpdate: 150 packets, avg 1.25ms
[2025-09-10 00:51:49.540] [INFO] [PKT] PositionUpdate: 300 packets, avg 0.85ms
[2025-09-10 00:51:49.541] [INFO] [PKT] ====================================
```

### Error Handling
```
[2025-09-10 00:51:45.123] [ERROR] [PKT] Error processing packet: Invalid packet format
[2025-09-10 00:51:45.124] [ERROR] [PKT] Exception: System.ArgumentException: Invalid packet data
```

## Implementation Details

The debug packet processing system works by:

1. **Wrapping the existing PacketProcessor**: The `DebugPacketProcessor` acts as a wrapper around the generated `PacketProcessor.Process` method
2. **Conditional processing**: When debugging is disabled, packets are processed normally with no overhead
3. **Performance monitoring**: Uses `Stopwatch` to measure packet processing times
4. **Statistics collection**: Maintains dictionaries of packet counts and timing information
5. **Error isolation**: Catches exceptions during packet processing and logs them while maintaining normal error handling behavior

## Performance Considerations

- When `EnablePacketDebugging = false`, there is minimal performance overhead
- Debug logging uses console output, which may impact performance in high-throughput scenarios
- Statistics collection uses in-memory dictionaries that grow over time
- Consider clearing statistics periodically in long-running applications

## Thread Safety

The debug packet processor is designed to be called from the main networking thread and should not be accessed concurrently from multiple threads without additional synchronization.
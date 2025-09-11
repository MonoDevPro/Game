using Arch.Core;
using Microsoft.Extensions.DependencyInjection;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Options;

Console.Title = "SERVER";

var serviceCollection = new ServiceCollection();

// Configure debug options
var debugOptions = new DebugOptions
{
    EnablePacketDebugging = true,
    LogPacketContents = false,
    LogPacketTiming = true,
    LogPacketErrors = true,
    PacketDebugLevel = DebugLevel.Info
};

// Sistemas
var playerIndexSystem = new PlayerIndexSystem(world);
var networkManager = new NetworkManager(world, playerIndexSystem);

// Initialize debug functionality
networkManager.InitializeDebug(debugOptions);

networkManager.StartServer(7777, "MinhaChaveDeProducao");
systems.Initialize();
Console.WriteLine("Server started with debug packet processing enabled.");
Console.WriteLine($"Debug settings: {debugOptions}");

var listener = networkManager.Listener;
listener.PeerConnectedEvent += peer => {
    Console.WriteLine($"[Server] Peer connected: {peer.Id}");
    world.Create(new PlayerId { Value = peer.Id }, new Position { X = 10, Y = 10 }, new MoveStats { Speed = 1.0f }, new StateComponent { Value = StateFlags.Idle });
};

var statsTimer = 0;
while (true) {
    systems.Update(0.016f);
    
    // Log packet statistics every 10 seconds
    statsTimer++;
    if (statsTimer >= 666) // ~10 seconds at 15ms sleep
    {
        networkManager.LogPacketStatistics();
        statsTimer = 0;
    }
    
    Thread.Sleep(15);
}
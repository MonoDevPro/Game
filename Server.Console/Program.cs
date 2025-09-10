using Arch.Core;
using Arch.System;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Network;
using Simulation.Generated.Network;
using Simulation.Core.Shared.Options;

Console.Title = "SERVER";
var world = World.Create();

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

var systems = new Group<float>("Game Systems",
    playerIndexSystem, // Indexa os jogadores para a rede
    new MovementSystem(world)
    // TODO: new GeneratedServerSyncSystem(world, networkManager) // Envia atualizações
);

networkManager.StartServer(7777, "MinhaChaveDeProducao");
systems.Initialize();
Console.WriteLine("Server started with debug packet processing enabled.");
Console.WriteLine($"Debug settings: {debugOptions}");

var listener = networkManager.Listener;
listener.PeerConnectedEvent += peer => {
    Console.WriteLine($"[Server] Peer connected: {peer.Id}");
    world.Create(new PlayerId { Value = peer.Id }, new Position { X = 10, Y = 10 });
};

var statsTimer = 0;
while (true) {
    networkManager.PollEvents();
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
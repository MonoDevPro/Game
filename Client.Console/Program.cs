using Arch.Core;
using Arch.System;
using Simulation.Core.Client.Systems;
using Simulation.Core.Server.Systems; // Reutiliza o PlayerIndexSystem
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Network;
using Simulation.Generated.Network;
using Simulation.Core.Shared.Options;

Console.Title = "CLIENT";
var world = World.Create();

// Configure debug options for client (less verbose than server)
var debugOptions = new DebugOptions
{
    EnablePacketDebugging = true,
    LogPacketContents = false,
    LogPacketTiming = false,
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
    new RenderSystem(world)
    // TODO: new GeneratedClientIntentSystem(world, networkManager)
);

networkManager.StartClient("127.0.0.1", 7777, "MinhaChaveDeProducao");
systems.Initialize();
Console.WriteLine("Client Started with debug packet processing enabled.");
Console.WriteLine("Pressione 'D' para mover, 'S' para estatísticas.");

var listener = networkManager.Listener;
Entity myPlayerEntity = Entity.Null;
listener.PeerConnectedEvent += peer => {
    myPlayerEntity = world.Create(new PlayerId { Value = peer.RemoteId }); // Cria a entidade local
    Console.WriteLine($"[Client] Connected. My PlayerId is {peer.RemoteId}.");
};

while (true) {
    networkManager.PollEvents();
    if (Console.KeyAvailable) {
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.D && !myPlayerEntity.Equals(Entity.Null)) {
            Console.WriteLine("[Client] Sending move intent...");
            // TODO: world.Add(myPlayerEntity, new MoveIntent { Direction = new Direction { X = 1, Y = 0 } });
            Console.WriteLine("[Client] Move intent placeholder - would send movement command");
        }
        else if (key == ConsoleKey.S) {
            Console.WriteLine("[Client] Showing packet statistics:");
            networkManager.LogPacketStatistics();
        }
    }
    systems.Update(0.016f);
    Thread.Sleep(15);
}
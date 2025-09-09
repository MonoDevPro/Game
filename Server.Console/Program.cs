using Arch.Core;
using Arch.System;
using Simulation.Core.Server.Systems;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Network.Generated;

Console.Title = "SERVER";
var world = World.Create();

// Sistemas
var playerIndexSystem = new PlayerIndexSystem(world);
var networkManager = new NetworkManager(world, playerIndexSystem);
var systems = new Group<float>("Game Systems",
    playerIndexSystem, // Indexa os jogadores para a rede
    new MovementSystem(world),
    new GeneratedServerSyncSystem(world, networkManager) // Envia atualizações
);

networkManager.StartServer(7777, "MinhaChaveDeProducao");
systems.Initialize();
Console.WriteLine("Server started.");

var listener = networkManager.Listener;
listener.PeerConnectedEvent += peer => {
    Console.WriteLine($"[Server] Peer connected: {peer.Id}");
    world.Create(new PlayerId { Value = peer.Id }, new Position { X = 10, Y = 10 });
};

while (true) {
    networkManager.PollEvents();
    systems.Update(0.016f);
    Thread.Sleep(15);
}
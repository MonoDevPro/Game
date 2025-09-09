using Arch.Core;
using Arch.System;
using Simulation.Core.Client.Systems;
using Simulation.Core.Server.Systems; // Reutiliza o PlayerIndexSystem
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Network.Generated;

Console.Title = "CLIENT";
var world = World.Create();

// Sistemas
var playerIndexSystem = new PlayerIndexSystem(world);
var networkManager = new NetworkManager(world, playerIndexSystem);
var systems = new Group<float>("Game Systems",
    playerIndexSystem, // Indexa os jogadores para a rede
    new RenderSystem(world),
    new GeneratedClientIntentSystem(world, networkManager)
);

networkManager.StartClient("127.0.0.1", 7777, "MinhaChaveDeProducao");
systems.Initialize();
Console.WriteLine("Client Started. Pressione 'D' para mover.");

var listener = networkManager.Listener;
Entity myPlayerEntity = Entity.Null;
listener.PeerConnectedEvent += peer => {
    myPlayerEntity = world.Create(new PlayerId { Value = peer.RemoteId }); // Cria a entidade local
    Console.WriteLine($"[Client] Connected. My PlayerId is {peer.RemoteId}.");
};

while (true) {
    networkManager.PollEvents();
    if (Console.KeyAvailable && Console.ReadKey(true).Key == ConsoleKey.D && !myPlayerEntity.Equals(Entity.Null)) {
        Console.WriteLine("[Client] Sending MoveIntent...");
        world.Add(myPlayerEntity, new MoveIntent { Direction = new Direction { X = 1, Y = 0 } });
    }
    systems.Update(0.016f);
    Thread.Sleep(15);
}
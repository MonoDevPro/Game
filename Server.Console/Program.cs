using Arch.Core;
using Arch.System;
using LiteNetLib;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Network.Generated;

// --- Configurações ---
const int ServerPort = 7777;
const string ConnectionKey = "MinhaChaveDeProducao";
Console.Title = "SERVER";

// --- Inicialização ---
var world = World.Create();
var networkManager = new NetworkManager(world);
networkManager.StartServer(ServerPort, ConnectionKey);

// --- Sistemas ECS ---
var systems = new Group<float>("Game Systems",
    // AQUI ESTÁ A MÁGICA: Apenas registramos o sistema que o gerador criou para nós.
    new GeneratedServerSyncSystem(world, networkManager)
    // Adicione seus sistemas de LÓGICA DE JOGO aqui (ex: MovementSystem, CombatSystem)
);
systems.Initialize();
Console.WriteLine($"Server started on port {ServerPort}");

// --- Lógica de Conexão (Exemplo) ---
var listener = networkManager.Listener;
listener.PeerConnectedEvent += peer =>
{
    // Cria uma entidade para o jogador que se conectou
    var playerEntity = world.Create(
        new PlayerId { Value = peer.Id },
        new Position { X = 10, Y = 10 },
        new Health { Current = 100, Max = 100 }
    );
    Console.WriteLine($"Created Entity {playerEntity} for Peer {peer.Id}");
};


// --- Game Loop ---
bool isRunning = true;
Console.CancelKeyPress += (_, _) => isRunning = false;
while (isRunning)
{
    networkManager.PollEvents();
    systems.Update(0.016f); // Atualiza TODOS os sistemas, incluindo o de rede.
    Thread.Sleep(15);
}

// --- Desligamento ---
systems.Dispose();
networkManager.Stop();


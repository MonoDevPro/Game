using Arch.Core;
using Arch.System;
using Simulation.Core.Shared.Components;
using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Network.Generated; // Importa os sistemas gerados!

// --- Configurações ---
const string ServerAddress = "127.0.0.1";
const int ServerPort = 7777;
const string ConnectionKey = "MinhaChaveDeProducao";
Console.Title = "CLIENT";

// --- Inicialização ---
var world = World.Create();

var networkManager = new NetworkManager(world);
networkManager.StartClient(ServerAddress, ServerPort, ConnectionKey);

// --- Entidade do Jogador (Exemplo) ---
// Em um jogo real, a ID viria do servidor. Aqui, usamos um placeholder.

Entity myPlayerEntity = default;

networkManager.Listener.PeerConnectedEvent += peer =>
{
    Console.WriteLine($"[Client] Connected to server: {peer.Id}");
    // Cria uma entidade para o jogador local
    myPlayerEntity = world.Create(
        new PlayerId { Value = peer.Id },
        new Position { X = 0, Y = 0 },
        new Health { Current = 100, Max = 100 }
    );
    
    Console.WriteLine($"Created local player Entity {myPlayerEntity} for Peer {peer.Id}");
};

Entity otherEntity = default;

var networkManager2 = new NetworkManager(world);
networkManager2.Listener.PeerConnectedEvent += peer =>
{
    Console.WriteLine($"[Client] Connected to server: {peer.Id}");
    // Cria uma entidade para o jogador local
    otherEntity = world.Create(
        new PlayerId { Value = peer.Id },
        new Position { X = 0, Y = 0 },
        new Health { Current = 100, Max = 100 }
    );
    
    var o = world.Create(new PlayerId { Value = -1 }); 
    
    Console.WriteLine($"Created local player Entity {otherEntity} for Peer {peer.Id}");
};
networkManager2.StartClient(ServerAddress, ServerPort, ConnectionKey);


// --- Sistemas ECS ---
var systems = new Group<float>("Game Systems",
    // Registra o sistema de cliente gerado automaticamente.
    new GeneratedClientIntentSystem(world, networkManager)
    // Adicione seus sistemas de LÓGICA DE JOGO aqui (ex: InputSystem, RenderSystem)
);
systems.Initialize();

// --- Game Loop & Simulação de Input ---
bool isRunning = true;
Console.CancelKeyPress += (_, _) => isRunning = false;
Console.WriteLine("Client Started. Pressione 'A' para atacar, 'M' para mover. Pressione Ctrl+C para sair.");

while (isRunning)
{
    networkManager.PollEvents();
    networkManager2.PollEvents();

    // Simula input do jogador
    if (Console.KeyAvailable)
    {
        var key = Console.ReadKey(true).Key;
        if (key == ConsoleKey.A)
        {
            Console.WriteLine("Enviando AttackIntent...");
            // Adiciona o componente de intenção na entidade do jogador. O sistema gerado cuidará do resto.
            world.Add(myPlayerEntity, new AttackIntent { Target = otherEntity }); // Ataca a entidade 0 como exemplo
        }
        else if (key == ConsoleKey.M)
        {
            Console.WriteLine("Enviando MoveIntent...");
            world.Add(myPlayerEntity, new MoveIntent { Direction = new Direction { X = 1, Y = 0 } });
        }
    }

    systems.Update(0.016f);
    Thread.Sleep(15);
}

// --- Desligamento ---
systems.Dispose();
networkManager.Stop();
using Arch.Core;
using LiteNetLib;

// Configurações do Servidor
const int ServerPort = 9050;
const string ConnectionKey = "MyMMORPGKey";

Console.WriteLine("Starting Server...");

// Mundo ECS
var world = World.Create();

// Rede
var listener = new EventBasedNetListener();
var server = new NetManager(listener);
server.Start(ServerPort);

Console.WriteLine($"Server started on port {ServerPort}");

listener.ConnectionRequestEvent += request =>
{
    if (server.ConnectedPeersCount < 100 /* max connections */)
        request.AcceptIfKey(ConnectionKey);
    else
        request.Reject();
};

listener.PeerConnectedEvent += peer =>
{
    Console.WriteLine($"Peer connected: {peer.Id}");
    // Exemplo: Criar uma entidade para o novo jogador
    var playerEntity = world.Create();
    // Adicionar componentes iniciais
    // world.Add(playerEntity, new Position { X = 0, Y = 0 }, new Health { Current = 100, Max = 100 });
};

listener.PeerDisconnectedEvent += (peer, info) =>
{
    Console.WriteLine($"Peer disconnected: {peer.Id}. Reason: {info.Reason}");
};

listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
{
    // Aqui o PacketProcessor gerado seria chamado
    // Simulation.Network.Generated.PacketProcessor.Process(fromPeer, dataReader);
    Console.WriteLine($"Received data from {fromPeer.Id}");
    dataReader.Recycle();
};


// Game Loop
bool isRunning = true;
Console.CancelKeyPress += (sender, e) => isRunning = false;

while (isRunning)
{
    server.PollEvents();
    // world.Update(); // Atualiza a lógica do mundo
    // systems.Update(); // Executa os sistemas ECS
    Thread.Sleep(15);
}

server.Stop();
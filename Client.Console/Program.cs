using LiteNetLib;
using System.Net;
using System.Net.Sockets;

// Configurações do Cliente
const string ServerAddress = "127.0.0.1";
const int ServerPort = 9050;
const string ConnectionKey = "MyMMORPGKey";

Console.WriteLine("Starting Client...");

// Mundo ECS
// var world = World.Create();

// Rede
var listener = new EventBasedNetListener();
var client = new NetManager(listener);
client.Start();
client.Connect(ServerAddress, ServerPort, ConnectionKey);

listener.PeerConnectedEvent += peer =>
{
    Console.WriteLine($"Connected to server: {peer.Id}");
};

listener.PeerDisconnectedEvent += (peer, info) =>
{
    Console.WriteLine($"Disconnected from server: {peer.Id}. Reason: {info.Reason}");
};

listener.NetworkReceiveEvent += (fromPeer, dataReader, deliveryMethod, channel) =>
{
    Console.WriteLine("Received data from server.");
    dataReader.Recycle();
};


// Game Loop
bool isRunning = true;
Console.CancelKeyPress += (sender, e) => isRunning = false;

while (isRunning)
{
    client.PollEvents();
    Thread.Sleep(15);
}

client.Stop();
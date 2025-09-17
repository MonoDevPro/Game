using LiteNetLib;
using Microsoft.Extensions.Logging;
using Simulation.Core.Network.Contracts;
using Simulation.Core.Options;
using Simulation.Network.Channel;

namespace Simulation.Network;

public class NetworkManager : INetworkManager
{
    private NetManager Net { get; }
    private NetworkListener Listener { get; }
    private readonly NetworkOptions _options;

    public readonly ChannelProcessorFactory ProcessorFactory;

    public NetworkManager(NetworkOptions options, ILoggerFactory factory)
    {
        _options = options;
        var router = new ChannelRouter();
        Listener = new NetworkListener(router, options, factory.CreateLogger<NetworkListener>());
        Net = new NetManager(Listener) { DisconnectTimeout = options.DisconnectTimeoutMs };
        
        ProcessorFactory = new ChannelProcessorFactory(Net, Listener, router, factory);
    }

    public NetworkAuthority Authority => _options.Authority;

    public void Initialize()
    {
        if (_options.Authority == NetworkAuthority.Server)
            StartServer();
        else
            StartClient();
    }

    public void StartServer() => Net.Start(_options.ServerPort);

    public void StartClient()
    {
        Net.Start();
        Net.Connect(_options.ServerAddress, _options.ServerPort, _options.ConnectionKey);
    }

    public void PollEvents() => Net.PollEvents();
    
    public void Stop()
    {
        Net.Stop();
    }
}
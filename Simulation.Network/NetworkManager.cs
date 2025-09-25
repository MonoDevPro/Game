using LiteNetLib;
using Microsoft.Extensions.Logging;
using Simulation.Core.Options;
using Simulation.Core.Ports.Network;
using Simulation.Network.Channel;

namespace Simulation.Network;

public class NetworkManager : INetworkManager
{
    private NetManager Net { get; }
    private NetworkListener Listener { get; }
    private readonly NetworkOptions _netOptions;
    private readonly AuthorityOptions _authorityOptions;

    public readonly ChannelProcessorFactory ProcessorFactory;

    public NetworkManager(NetworkOptions netOptions, AuthorityOptions authorityOptions, ILoggerFactory factory)
    {
        _netOptions = netOptions;
        _authorityOptions = authorityOptions;
        var router = new ChannelRouter();
        Listener = new NetworkListener(router, netOptions, factory.CreateLogger<NetworkListener>());
        Net = new NetManager(Listener) { DisconnectTimeout = netOptions.DisconnectTimeoutMs };
        Net.ChannelsCount = (byte)Enum.GetValues(typeof(NetworkChannel)).Length;
        
        ProcessorFactory = new ChannelProcessorFactory(Net, Listener, router, factory);
    }

    public Authority Authority => _authorityOptions.Authority;

    public void Initialize()
    {
        if (_authorityOptions.Authority == Authority.Server)
            StartServer();
        else
            StartClient();
    }

    public void StartServer() => Net.Start(_netOptions.ServerPort);

    public void StartClient()
    {
        Net.Start();
        Net.Connect(_netOptions.ServerAddress, _netOptions.ServerPort, _netOptions.ConnectionKey);
    }

    public void PollEvents() => Net.PollEvents();
    
    public void Stop()
    {
        Net.Stop();
    }
}
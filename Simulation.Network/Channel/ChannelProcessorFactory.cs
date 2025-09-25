using LiteNetLib;
using Microsoft.Extensions.Logging;
using Simulation.Core.Ports.Network;
using Simulation.Network.Packet;

namespace Simulation.Network.Channel;

public sealed class ChannelProcessorFactory(NetManager manager, NetworkListener listener, ChannelRouter router, ILoggerFactory loggerFactory)
    : IChannelProcessorFactory, IDisposable
{
    private readonly Dictionary<NetworkChannel, ChannelEndpoint> _cache = new();

    public IChannelEndpoint CreateOrGet(NetworkChannel channel)
    {
        if (_cache.TryGetValue(channel, out var existing))
            return existing;
            
        return _cache[channel] = CreateEndpoint(channel);
    }

    public bool TryGet(NetworkChannel channel, out IChannelEndpoint? processor)
    {
        if (_cache.TryGetValue(channel, out var endpoint))
        {
            processor = endpoint;
            return true;
        }
        processor = null;
        return false;
    }

    private ChannelEndpoint CreateEndpoint(NetworkChannel channel)
    {
        // create processor (packet decode/registry)
        var processorLogger = loggerFactory.CreateLogger<PacketProcessor>();
        var processor = new PacketProcessor(listener, processorLogger);

        // create dispatcher (uses processor as IPacketRegister)
        var sender = new PacketSender(manager, processor);

        // try to register on router so inbound calls are routed to this processor
        // ChannelRouter deve expor RegisterChannel para aceitar pares externos — abaixo assumimos que tem.
        router.RegisterChannel(channel, processor, sender);

        var endpointLogger = loggerFactory.CreateLogger<ChannelEndpoint>();
        var endpoint = new ChannelEndpoint(channel, processor, sender, listener, endpointLogger);
            
        return endpoint;
    }

    public void Dispose()
    {
        // se ChannelProcessor/PacketDispatcher implementarem IDisposable, dispose aqui.
        _cache.Clear();
    }
}
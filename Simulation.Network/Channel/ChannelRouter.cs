using LiteNetLib;
using Microsoft.Extensions.Logging;
using Simulation.Core.Ports.Network;
using Simulation.Network.Packet;

namespace Simulation.Network.Channel;

public sealed record Channel(NetworkChannel Id, PacketProcessor Processor, PacketSender Dispatcher);

public class ChannelRouter(ILogger<ChannelRouter>? logger = null)
{
    private readonly Dictionary<byte, Channel> _channels = new();
    private readonly Dictionary<byte, ChannelHandler> _channelHandlers = new();
    
    public delegate void ChannelHandler(NetPeer peer, NetPacketReader reader);
    

    public bool RegisterChannel(NetworkChannel channel, PacketProcessor processor, PacketSender dispatcher)
    {
        var ch = new Channel(channel, processor, dispatcher);
        if (!_channels.TryAdd((byte)channel, ch))
            logger?.LogWarning("Channel {Channel} already registered; replacing existing.", channel);

        _channelHandlers[(byte)channel] = processor.HandleData;
        logger?.LogInformation("Channel {Channel} registered via external factory.", channel);
        return true;
    }

    public void Handle(NetPeer fromPeer, NetPacketReader dataReader, byte channel)
    {
        if (_channelHandlers.TryGetValue(channel, out var handler))
        {
            try
            {
                handler.Invoke(fromPeer, dataReader);
            }
            catch (System.Exception ex)
            {
                logger?.LogError(ex, "Erro ao executar handler do canal {Channel} para peer {PeerId}", (NetworkChannel)channel, fromPeer.Id);
            }
        }
        else
        {
            logger?.LogWarning("Nenhum handler registado para o canal {Channel} recebido do peer {PeerId}", (NetworkChannel)channel, fromPeer.Id);
        }
    }
}
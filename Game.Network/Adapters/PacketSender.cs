using System.Net;
using Game.Network.Abstractions;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;

namespace Game.Network.Adapters;

public class PacketSender(NetManager manager, PacketProcessor processor)
{
    private readonly ThreadLocal<NetDataWriter> _threadWriter = new(() => new NetDataWriter());

    private void WritePacketToWriter<T>(NetDataWriter writer, ref T packet) where T : struct
    {
        writer.Reset();
        var packetId = processor.GetPacketId<T>();
        writer.Put(packetId);
        writer.Put(MemoryPackSerializer.Serialize(packet)); 
    }

    // Método base privado alterado para consistência
    private void Send<T>(NetPeer peer, ref T packet, byte channel, DeliveryMethod deliveryMethod) where T : struct
    {
        if (peer is not { ConnectionState: ConnectionState.Connected }) return;
            
        var writer = _threadWriter.Value!;
        WritePacketToWriter(writer, ref packet);
        peer.Send(writer, channel, deliveryMethod);
    }
        
    public void SendToServer<T>(ref T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct
        => Send(manager.FirstPeer, ref packet, (byte)channel, deliveryMethod);

    public void SendToPeer<T>(NetPeer peer, ref T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct
        => Send(peer, ref packet, (byte)channel, deliveryMethod);
            
    public void SendToPeerId<T>(int peerId, ref T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct
    {
        if (manager.GetPeerById(peerId) is { ConnectionState: ConnectionState.Connected } peer)
            Send(peer, ref packet, (byte)channel, deliveryMethod);
    }

    public void SendToAll<T>(ref T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct
    {
        var writer = _threadWriter.Value!;
        WritePacketToWriter(writer, ref packet);
        manager.SendToAll(writer, (byte)channel, deliveryMethod);
    }

    public void SendToAllExcept<T>(NetPeer excludePeer, ref T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct
    {
        var writer = _threadWriter.Value!;
        WritePacketToWriter(writer, ref packet);
        manager.SendToAll(writer, (byte)channel, deliveryMethod, excludePeer);
    }
    
    public void SendUnconnected<T>(IPEndPoint endPoint, ref T packet) where T : struct
    {
        var writer = _threadWriter.Value!;
        WritePacketToWriter(writer, ref packet);
        manager.SendUnconnectedMessage(writer, endPoint);
    }
}
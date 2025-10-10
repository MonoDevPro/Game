using Game.Network.Abstractions;
using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;

namespace Game.Network.Adapters;

public class PacketSender(NetManager manager, PacketProcessor processor)
{
    private readonly ThreadLocal<NetDataWriter> _threadWriter = new(() => new NetDataWriter());

    private void WritePacketToWriter<T>(NetDataWriter writer, T packet) where T : struct, IPacket
    {
        writer.Reset();
        var packetId = processor.GetPacketId<T>();
        writer.Put(packetId);
        writer.Put(MemoryPackSerializer.Serialize(packet)); 
    }

    // Método base privado alterado para consistência
    private void Send<T>(NetPeer peer, T packet, byte channel, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        if (peer is not { ConnectionState: ConnectionState.Connected }) return;
            
        var writer = _threadWriter.Value!;
        WritePacketToWriter(writer, packet);
        peer.Send(writer, channel, deliveryMethod);
    }
        
    public void SendToServer<T>(T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct, IPacket
        => Send(manager.FirstPeer, packet, (byte)channel, deliveryMethod);

    public void SendToPeer<T>(NetPeer peer, T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct, IPacket
        => Send(peer, packet, (byte)channel, deliveryMethod);
            
    public void SendToPeerId<T>(int peerId, T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        if (manager.GetPeerById(peerId) is { ConnectionState: ConnectionState.Connected } peer)
            Send(peer, packet, (byte)channel, deliveryMethod);
    }

    public void SendToAll<T>(T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        var writer = _threadWriter.Value!;
        WritePacketToWriter(writer, packet);
        manager.SendToAll(writer, (byte)channel, deliveryMethod);
    }

    public void SendToAllExcept<T>(NetPeer excludePeer, T packet, NetworkChannel channel, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        var writer = _threadWriter.Value!;
        WritePacketToWriter(writer, packet);
        manager.SendToAll(writer, (byte)channel, deliveryMethod, excludePeer);
    }
}
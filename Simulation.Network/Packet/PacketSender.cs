using LiteNetLib;
using LiteNetLib.Utils;
using MemoryPack;
using Simulation.Core.Network.Contracts;

namespace Simulation.Network.Packet;

public class PacketSender(NetManager manager, PacketProcessor processor)
{
    // Thread-local NetDataWriter to avoid allocation churn on hot-path.
    private static readonly ThreadLocal<NetDataWriter> ThreadWriter =
        new(() => new NetDataWriter());
    private NetDataWriter Writer => ThreadWriter.Value!;
    
    private void WritePacketToWriter<T>(NetDataWriter writer, T packet) where T : struct, IPacket
    {
        var packetId = processor.GetPacketId(typeof(T));
        writer.Put(packetId);
        writer.Put(MemoryPackSerializer.Serialize(packet)); 
    }

    private void Send<T>(NetPeer peer, NetworkChannel channel, T packet, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        if (peer is not { ConnectionState: ConnectionState.Connected }) return;
        
        Writer.Reset();
        WritePacketToWriter(Writer, packet);
        peer.Send(Writer, (byte)channel, deliveryMethod);
    }
    
    public void SendToServer<T>(NetworkChannel channel, T packet, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        Send(manager.FirstPeer, channel, packet, deliveryMethod);
    }

    public void SendToPeer<T>(NetPeer peer, NetworkChannel channel, T packet, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        Send(peer, channel, packet, deliveryMethod);
    }
    public void SendToPeerId<T>(int peerId, NetworkChannel channel, T packet, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        if (manager.GetPeerById(peerId) is not { ConnectionState: ConnectionState.Connected } peer) return;
        Send(peer, channel, packet, deliveryMethod);
    }

    public void SendToAll<T>(NetworkChannel channel, T packet, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        Writer.Reset();
        WritePacketToWriter(Writer, packet);
        manager.SendToAll(Writer, (byte)channel, deliveryMethod);
    }

    public void SendToAllExcept<T>(NetPeer excludePeer, NetworkChannel channel, T packet, DeliveryMethod deliveryMethod) where T : struct, IPacket
    {
        Writer.Reset();
        WritePacketToWriter(Writer, packet);
        manager.SendToAll(Writer, (byte)channel, deliveryMethod, excludePeer);
    }
}
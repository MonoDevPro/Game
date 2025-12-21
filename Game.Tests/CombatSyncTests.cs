using System;
using System.Collections.Generic;
using Game.DTOs.Game;
using Game.ECS.Shared.Services.Network;

namespace Game.Tests;

public sealed class CombatSyncTests
{

    private sealed class DummyNetworkManager : INetworkManager
    {
        public event Action<INetPeerAdapter> OnPeerConnected
        {
            add { }
            remove { }
        }

        public event Action<INetPeerAdapter> OnPeerDisconnected
        {
            add { }
            remove { }
        }

        public bool IsRunning => true;

        public IPeerRepository Peers { get; } = new DummyPeerRepository();

        public List<AttackPacket> CombatPackets { get; } = new();

        public void Initialize() { }
        public void ConnectToServer() => throw new NotSupportedException();
        public void Stop() { }
        public void PollEvents() { }

        public void RegisterPacketHandler<T>(PacketHandler<T> handler) where T : struct => throw new NotSupportedException();
        public bool UnregisterPacketHandler<T>() where T : struct => throw new NotSupportedException();
        public void RegisterUnconnectedPacketHandler<T>(UnconnectedPacketHandler<T> handler) where T : struct => throw new NotSupportedException();
        public bool UnregisterUnconnectedPacketHandler<T>() where T : struct => throw new NotSupportedException();
        public void SendToServer<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct => throw new NotSupportedException();
        public void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct => throw new NotSupportedException();
        public void SendToPeerId<T>(int peerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct => throw new NotSupportedException();

        public void SendToAll<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct
        {
            if (packet is AttackPacket combat)
                CombatPackets.Add(combat);
        }

        public void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct => throw new NotSupportedException();
        public void SendToAllExcept<T>(int excludePeerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct => throw new NotSupportedException();
        public void SendUnconnected<T>(System.Net.IPEndPoint endPoint, T packet) where T : struct => throw new NotSupportedException();

        private sealed class DummyPeerRepository : IPeerRepository
        {
            public bool TryGetPeer(int peerId, out INetPeerAdapter? peer)
            {
                peer = null;
                return false;
            }

            public IEnumerable<INetPeerAdapter> GetAllPeers() => Array.Empty<INetPeerAdapter>();
            public int PeerCount => 0;
        }
    }
}
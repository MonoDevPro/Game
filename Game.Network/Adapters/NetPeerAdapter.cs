using System.Net;
using Game.Network.Abstractions;
using LiteNetLib;

namespace Game.Network.Adapters;

/// <summary>
    /// Implementação do adaptador que encapsula NetPeer.
    /// </summary>
    public sealed class NetPeerAdapter(NetPeer peer) : INetPeerAdapter, IEquatable<NetPeerAdapter>
    {
        internal NetPeer Peer => peer;

        private void EnsurePeer()
        {
            if (Peer == null)
                throw new InvalidOperationException("Underlying NetPeer is null.");
        }

        private void EnsureConnected()
        {
            EnsurePeer();
            if (!Peer.ConnectionState.HasFlag(ConnectionState.Connected))
                throw new InvalidOperationException("Peer is not in Connected state.");
        }

        public int Id => Peer.Id;
        public int RemoteId => Peer.RemoteId;

        public IPEndPoint EndPoint => (IPEndPoint)Peer; // NetPeer derives from IPEndPoint

        public int Ping => Peer.Ping;

        public int RoundTripTime => Peer.RoundTripTime;

        public int Mtu => Peer.Mtu;

        public bool IsConnected => Peer.ConnectionState == ConnectionState.Connected;

        public object Tag
        {
            get => Peer.Tag;
            set => Peer.Tag = value;
        }

        public int GetPacketsCountInReliableQueue(byte channelNumber, bool ordered)
        {
            EnsurePeer();
            return Peer.GetPacketsCountInReliableQueue(channelNumber, ordered);
        }

        public int GetMaxSinglePacketSize(NetworkDeliveryMethod method)
        {
            EnsurePeer();
            return Peer.GetMaxSinglePacketSize(method.ToLite());
        }

        // ----------------------------------------------------
        // Simple forwarding send APIs (with state checks)
        // ----------------------------------------------------
        public void Send(byte[] data, NetworkDeliveryMethod deliveryMethod)
        {
            EnsureConnected();
            Peer.Send(data, deliveryMethod.ToLite());
        }

        public void Send(byte[] data, int start, int length, NetworkDeliveryMethod deliveryMethod)
        {
            EnsureConnected();
            Peer.Send(data, start, length, deliveryMethod.ToLite());
        }

        public void Send(byte[] data, byte channelNumber, NetworkDeliveryMethod deliveryMethod)
        {
            EnsureConnected();
            Peer.Send(data, channelNumber, deliveryMethod.ToLite());
        }

        public void Send(ReadOnlySpan<byte> data, NetworkDeliveryMethod deliveryMethod)
        {
            EnsureConnected();
            Peer.Send(data, deliveryMethod.ToLite());
        }

        public void Send(ReadOnlySpan<byte> data, byte channelNumber, NetworkDeliveryMethod deliveryMethod)
        {
            EnsureConnected();
            Peer.Send(data, channelNumber, deliveryMethod.ToLite());
        }

        public void SendWithDeliveryEvent(byte[] data, byte channelNumber, NetworkDeliveryMethod deliveryMethod, object userData)
        {
            EnsureConnected();
            Peer.SendWithDeliveryEvent(data, channelNumber, deliveryMethod.ToLite(), userData);
        }

        public void SendWithDeliveryEvent(ReadOnlySpan<byte> data, byte channelNumber, NetworkDeliveryMethod deliveryMethod, object userData)
        {
            EnsureConnected();
            Peer.SendWithDeliveryEvent(data, channelNumber, deliveryMethod.ToLite(), userData);
        }

        // ----------------------------------------------------
        // Pooled packet helpers (useful para zero-copy)
        // ----------------------------------------------------
        public PooledPacket CreatePooledPacket(DeliveryMethod deliveryMethod, byte channelNumber)
        {
            EnsureConnected();
            return Peer.CreatePacketFromPool(deliveryMethod, channelNumber);
        }

        public void SendPooledPacket(PooledPacket packet, int userDataSize)
        {
            EnsureConnected();
            Peer.SendPooledPacket(packet, userDataSize);
        }

        // ----------------------------------------------------
        // Disconnects
        // ----------------------------------------------------
        public void Disconnect()
        {
            EnsurePeer();
            Peer.Disconnect();
        }

        public void Disconnect(byte[] data)
        {
            EnsurePeer();
            Peer.Disconnect(data);
        }

        public void Disconnect(byte[] data, int start, int count)
        {
            EnsurePeer();
            Peer.Disconnect(data, start, count);
        }

        // equality based on underlying peer identity
        public bool Equals(NetPeerAdapter? other) => Equals(Peer, other?.Peer);
        
        public override bool Equals(object? obj) => obj is NetPeerAdapter other && Equals(other);

        public override int GetHashCode() => Peer.GetHashCode();

        public override string ToString() => $"NetPeerAdapter(Id={Id}, EndPoint={EndPoint}, Connected={IsConnected})";
    }
using System;
using System.Collections.Generic;
using System.Net;
using FluentAssertions;
using Game.Abstractions.Network;
using Game.Abstractions;
using Game.Core;
using Game.Domain.Entities;
using Game.Domain.Enums;
using Game.Domain.VOs;
using Game.Network.Packets;
using Game.Server;
using Game.Server.Players;
using Game.Server.Sessions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests.Server;

public class GameServerInputFlowTests
{
    [Fact]
    public void HandlePlayerInput_updates_sequence_and_ignores_out_of_order_packets()
    {
        var services = new ServiceCollection();
        services.AddSingleton(CreateMapService());
        var provider = services.BuildServiceProvider();

        var simulation = new GameSimulation(provider);
        var network = new TestNetworkManager();
        var sessions = new PlayerSessionManager(NullLogger<PlayerSessionManager>.Instance);
        var spawnService = new PlayerSpawnService(simulation, NullLogger<PlayerSpawnService>.Instance);
        var scopeFactory = new TestScopeFactory();
        _ = new GameServer(network, sessions, spawnService, scopeFactory, simulation, NullLogger<GameServer>.Instance);

        var peer = new TestPeerAdapter(10);
        network.AddPeer(peer);

        var account = new Account
        {
            Id = 1,
            Username = "tester",
            Email = "tester@example.com",
            PasswordHash = "hash",
            PasswordSalt = new byte[16],
            CreatedAt = DateTimeOffset.UtcNow,
            LastUpdatedAt = DateTimeOffset.UtcNow
        };

        var character = new Character
        {
            Id = 2,
            Name = "Hero",
            AccountId = account.Id,
            Account = account,
            Stats = new Stats(),
            Inventory = new Inventory(),
            PositionX = 0,
            PositionY = 0,
            DirectionEnum = DirectionEnum.South
        };

        var session = new PlayerSession(peer, account, character)
        {
            Entity = simulation.SpawnPlayer(account.Id, peer.Id,
                new Coordinate(character.PositionX, character.PositionY),
                character.DirectionEnum, character.Stats)
        };

        sessions.TryAddSession(session, out _).Should().BeTrue();

        var handler = network.GetHandler<PlayerInputPacket>();

        handler(peer, new PlayerInputPacket(sequence: 1, moveX: 1, moveY: 0, buttons: 0));
        session.LastInputSequence.Should().Be(1);

        handler(peer, new PlayerInputPacket(sequence: 1, moveX: 0, moveY: 0, buttons: 0));
        session.LastInputSequence.Should().Be(1, "out-of-order packet should be ignored");

        handler(peer, new PlayerInputPacket(sequence: 2, moveX: 0, moveY: 1, buttons: 0));
        session.LastInputSequence.Should().Be(2);
    }

    private static MapService CreateMapService()
    {
        const int size = 8;
        var tiles = new TileType[size * size];
        Array.Fill(tiles, TileType.Floor);

        var template = new Map
        {
            Id = 1,
            Name = "TestMap",
            Width = size,
            Height = size,
            Tiles = tiles,
            CollisionMask = new byte[size * size],
            BorderBlocked = false,
            UsePadded = false
        };

        return MapService.CreateFromTemplate(template);
    }

    private sealed class TestScopeFactory : IServiceScopeFactory
    {
        public IServiceScope CreateScope() => new TestScope();

        private sealed class TestScope : IServiceScope
        {
            public IServiceProvider ServiceProvider { get; } = new ServiceCollection().BuildServiceProvider();
            public void Dispose() { }
        }
    }

    private sealed class TestNetworkManager : INetworkManager
    {
        private readonly Dictionary<Type, Delegate> _handlers = new();
        private readonly TestPeerRepository _peers = new();

        public event Action<INetPeerAdapter> OnPeerConnected = delegate { };
        public event Action<INetPeerAdapter> OnPeerDisconnected = delegate { };

        public bool IsRunning => true;
        public IPeerRepository Peers => _peers;

        public void AddPeer(INetPeerAdapter peer)
        {
            _peers.Add(peer);
            OnPeerConnected(peer);
        }

        public void Start() { }
        public void Stop() { }
        public void PollEvents() { }

        public void RegisterPacketHandler<T>(PacketHandler<T> handler) where T : struct, IPacket
            => _handlers[typeof(T)] = handler;

        public bool UnregisterPacketHandler<T>() where T : struct, IPacket
            => _handlers.Remove(typeof(T));

        public PacketHandler<T> GetHandler<T>() where T : struct, IPacket
            => (PacketHandler<T>)_handlers[typeof(T)];

        public void SendToServer<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket { }
        public void SendToPeer<T>(INetPeerAdapter peer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket { }
        public void SendToPeerId<T>(int peerId, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket { }
        public void SendToAll<T>(T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket { }
        public void SendToAllExcept<T>(INetPeerAdapter excludePeer, T packet, NetworkChannel channel, NetworkDeliveryMethod deliveryMethod) where T : struct, IPacket { }
    }

    private sealed class TestPeerRepository : IPeerRepository
    {
        private readonly Dictionary<int, INetPeerAdapter> _peers = new();

        public bool TryGetPeer(int peerId, out INetPeerAdapter? peer) => _peers.TryGetValue(peerId, out peer);
        public IEnumerable<INetPeerAdapter> GetAllPeers() => _peers.Values;
        public int PeerCount => _peers.Count;

        public void Add(INetPeerAdapter peer) => _peers[peer.Id] = peer;
    }

    private sealed class TestPeerAdapter : INetPeerAdapter
    {
        public TestPeerAdapter(int id)
        {
            Id = id;
            EndPoint = new IPEndPoint(IPAddress.Loopback, 7000 + id);
        }

        public int Id { get; }
        public IPEndPoint EndPoint { get; }
        public int Ping => 0;
        public int RoundTripTime => 0;
        public int Mtu => 1200;
        public bool IsConnected => true;
        public object Tag { get; set; } = new();

        public int GetPacketsCountInReliableQueue(byte channelNumber, bool ordered) => 0;
        public int GetMaxSinglePacketSize(NetworkDeliveryMethod method) => 0;

        public void Send(byte[] data, NetworkDeliveryMethod networkDeliveryMethod) { }
        public void Send(byte[] data, int start, int length, NetworkDeliveryMethod networkDeliveryMethod) { }
        public void Send(byte[] data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod) { }
        public void Send(ReadOnlySpan<byte> data, NetworkDeliveryMethod networkDeliveryMethod) { }
        public void Send(ReadOnlySpan<byte> data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod) { }
        public void SendWithDeliveryEvent(byte[] data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod, object userData) { }
        public void SendWithDeliveryEvent(ReadOnlySpan<byte> data, byte channelNumber, NetworkDeliveryMethod networkDeliveryMethod, object userData) { }

        public void Disconnect() { }
        public void Disconnect(byte[] data) { }
        public void Disconnect(byte[] data, int start, int count) { }
    }
}

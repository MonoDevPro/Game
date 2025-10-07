using System;
using System.Net;
using System.Net.Sockets;
using FluentAssertions;
using Game.Abstractions.Network;
using Game.Domain.Entities;
using Game.Server.Sessions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Game.Tests.Sessions;

public class PlayerSessionManagerTests
{
    [Fact]
    public void PreventsDuplicateAccountSessions()
    {
        var manager = new PlayerSessionManager(NullLogger<PlayerSessionManager>.Instance);

        var account = new Account { Id = 1, Username = "tester" };
        var character = new Character { Id = 10, Name = "Hero", AccountId = 1 };

        var sessionA = new PlayerSession(new TestPeerAdapter(1), account, character);
        var sessionB = new PlayerSession(new TestPeerAdapter(2), account, character);

        manager.TryAddSession(sessionA, out _).Should().BeTrue();
        manager.TryAddSession(sessionB, out var error).Should().BeFalse();
        error.Should().NotBeNull();
    }

    private sealed class TestPeerAdapter : INetPeerAdapter
    {
        public TestPeerAdapter(int id)
        {
            Id = id;
            EndPoint = new IPEndPoint(IPAddress.Loopback, 7777 + id);
        }

        public int Id { get; }
        public IPEndPoint EndPoint { get; }
        public int Ping => 0;
        public int RoundTripTime => 0;
        public int Mtu => 1400;
        public bool IsConnected => true;
        public object Tag { get; set; } = new object();

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

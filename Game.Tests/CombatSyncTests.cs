using System;
using System.Collections.Generic;
using System.Linq;
using Arch.Core;
using FluentAssertions;
using Game.ECS;
using Game.ECS.Components;
using Game.ECS.Entities.Factories;
using Game.ECS.Services;
using Game.Network.Abstractions;
using Game.Network.Packets.Game;
using Game.Server.ECS.Systems;
using Xunit;

namespace Game.Tests;

public sealed class CombatSyncTests
{
    [Fact]
    public void BasicAttack_ShouldMarkCombatDirty_AndEmitCombatPacket()
    {
        // Arrange: world + services
        using var world = World.Create();
        var mapService = new MapService();
        mapService.RegisterMap(0, width: 32, height: 32, layers: 1);

        var network = new DummyNetworkManager();
        var combatSystem = new Game.Server.ECS.Systems.CombatSystem(world, mapService);
        var syncSystem = new ServerSyncSystem(world, network);

        // Create attacker and defender
        var attackerData = new PlayerData(
            PlayerId: 1,
            NetworkId: 101,
            Name: "Attacker",
            Gender: 0,
            Vocation: 0,
            SpawnX: 10,
            SpawnY: 10,
            SpawnZ: 0,
            FacingX: 1,
            FacingY: 0,
            Hp: 100,
            MaxHp: 100,
            HpRegen: 0,
            Mp: 50,
            MaxMp: 50,
            MpRegen: 0,
            MovementSpeed: 1f,
            AttackSpeed: 1f,
            PhysicalAttack: 20,
            MagicAttack: 0,
            PhysicalDefense: 5,
            MagicDefense: 5,
            MapId: 0);

        var attacker = world.CreatePlayer(index: null, attackerData);

        var defenderData = attackerData with
        {
            PlayerId = 2,
            NetworkId = 202,
            Name = "Defender",
            SpawnX = 11, // à frente do atacante (facingX = +1)
            PhysicalDefense = 1,
            MagicDefense = 1
        };

        var defender = world.CreatePlayer(index: null, defenderData);

        // Registrar entidades no spatial map (necessário para TryFindNearestTarget)
        var spatial = mapService.GetMapSpatial(0);
        Position attackerPos = world.Get<Position>(attacker);
        Position defenderPos = world.Get<Position>(defender);
        spatial.Insert(attackerPos, attacker);
        spatial.Insert(defenderPos, defender);

        // Simular input de ataque básico (Ctrl)
        ref var attackerInput = ref world.Get<PlayerInput>(attacker);
        attackerInput.Flags = InputFlags.Attack;
        attackerInput.InputX = 0;
        attackerInput.InputY = 0;
    world.Has<DirtyFlags>(attacker).Should().BeTrue("Jogador precisa ter componente DirtyFlags");

        // Act: roda sistemas na mesma ordem do servidor
        combatSystem.Update(SimulationConfig.TickDelta);

        world.Has<AttackAction>(attacker)
            .Should().BeTrue("CombatSystem deve anexar AttackAction ao atacante");
        ref var attackerDirty = ref world.Get<DirtyFlags>(attacker);
        attackerDirty.Raw.Should().BeGreaterThan(0, "DirtyFlags precisa receber ao menos um bit");
        attackerDirty.IsDirty(DirtyComponentType.CombatState)
            .Should().BeTrue("CombatSystem deve marcar CombatState como dirty");

        syncSystem.Update(SimulationConfig.TickDelta);

        // Assert: ataque gerou pacote de combate e resultado
        network.CombatPackets.Should().ContainSingle("CombatStatePacket não foi enviado pelo servidor");
        network.AttackResults.Should().ContainSingle("AttackResultPacket não foi enviado pelo servidor");

        var combatPacket = network.CombatPackets.Single();
        combatPacket.AttackerNetworkId.Should().Be(attackerData.NetworkId);
        combatPacket.DefenderNetworkId.Should().Be(defenderData.NetworkId);
        combatPacket.Type.Should().Be(AttackType.Basic);
    }

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

        public List<CombatStatePacket> CombatPackets { get; } = new();
        public List<AttackResultPacket> AttackResults { get; } = new();

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
            if (packet is CombatStatePacket combat)
                CombatPackets.Add(combat);
            else if (packet is AttackResultPacket result)
                AttackResults.Add(result);
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

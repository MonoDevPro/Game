using System;
using System.Collections.Generic;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Godot;

namespace GodotClient.Player;

public partial class PlayerRoot : Node2D
{
    private readonly Dictionary<int, PlayerVisual> _players = new();
    private Node2D? _world;
    private int _localNetworkId = -1;

    public override void _Ready()
    {
        base._Ready();
        _world = GetParent()?.GetNode<Node2D>("World");
    }

    public void SetLocalPlayer(PlayerSnapshot snapshot)
    {
        _localNetworkId = snapshot.NetworkId;
        ApplySnapshot(snapshot, true);
    }

    public void ApplySnapshot(PlayerSnapshot snapshot, bool isLocal)
    {
        var visual = GetOrCreateVisual(snapshot.NetworkId);
        var treatAsLocal = isLocal || snapshot.NetworkId == _localNetworkId;
        visual.Update(snapshot, treatAsLocal);
    }

    public void UpdateState(PlayerStatePacket packet)
    {
        if (_players.TryGetValue(packet.NetworkId, out var visual))
        {
            visual.UpdatePosition(packet.Position);
            visual.UpdateFacing(packet.Facing);
        }
    }

    public void RemovePlayer(int networkId)
    {
        if (_players.TryGetValue(networkId, out var visual))
        {
            visual.QueueFree();
            _players.Remove(networkId);
        }
    }

    public void Clear()
    {
        foreach (var visual in _players.Values)
        {
            visual.QueueFree();
        }

        _players.Clear();
        _localNetworkId = -1;
    }

    private PlayerVisual GetOrCreateVisual(int networkId)
    {
        if (_players.TryGetValue(networkId, out var visual))
        {
            return visual;
        }

        var world = EnsureWorld();
        visual = new PlayerVisual
        {
            Name = $"Player_{networkId}"
        };
        world.AddChild(visual);
        _players[networkId] = visual;
        return visual;
    }

    private Node2D EnsureWorld()
    {
        _world ??= GetParent()?.GetNode<Node2D>("World") ?? throw new InvalidOperationException("World node not found.");

        return _world;
    }
}

using System;
using System.Collections.Generic;
using Game.Domain.VOs;
using Game.Network.Packets;
using Game.Network.Packets.DTOs;
using Game.Network.Packets.Simulation;
using Godot;

namespace GodotClient.Visuals;

public partial class PlayerView : Node
{
    private readonly Dictionary<int, AnimatedPlayerVisual> _players = new();
    private Node2D? _world;
    private int _localNetworkId = -1;
    
    public AnimatedPlayerVisual? GetLocalPlayer()
    {
        if (_localNetworkId == -1 || !_players.TryGetValue(_localNetworkId, out var visual)) return null;
        return visual;
    }

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

    public void UpdateMovement(PlayerMovementPacket packet)
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

    private AnimatedPlayerVisual GetOrCreateVisual(int networkId)
    {
        if (_players.TryGetValue(networkId, out var visual))
        {
            return visual;
        }

        var world = EnsureWorld();
        visual = new AnimatedPlayerVisual
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

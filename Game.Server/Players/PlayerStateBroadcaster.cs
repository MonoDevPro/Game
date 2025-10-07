using System.Collections.Generic;
using Game.Abstractions.Network;
using Game.Core;
using Game.Network.Packets;
using Microsoft.Extensions.Logging;

namespace Game.Server.Players;

/// <summary>
/// Converts simulation dirty flags into network packets and broadcasts them.
/// </summary>
public sealed class PlayerStateBroadcaster
{
    private readonly GameSimulation _simulation;
    private readonly INetworkManager _networkManager;
    private readonly ILogger<PlayerStateBroadcaster> _logger;
    private readonly List<PlayerNetworkStateData> _buffer = new();

    public PlayerStateBroadcaster(
        GameSimulation simulation,
        INetworkManager networkManager,
        ILogger<PlayerStateBroadcaster> logger)
    {
        _simulation = simulation;
        _networkManager = networkManager;
        _logger = logger;
    }

    public void Broadcast()
    {
        _buffer.Clear();
        _simulation.CollectDirtyPlayers(_buffer);

        if (_buffer.Count == 0)
        {
            return;
        }

        foreach (var state in _buffer)
        {
            var packet = new PlayerStatePacket(
                state.NetworkId,
                GridPosition.FromCoordinate(state.Position),
                state.Facing,
                state.Tick);

            _networkManager.SendToAll(packet, NetworkChannel.Simulation, NetworkDeliveryMethod.Sequenced);
        }

        if (_logger.IsEnabled(LogLevel.Trace))
        {
            _logger.LogTrace("Broadcasted {Count} player state updates", _buffer.Count);
        }
    }
}

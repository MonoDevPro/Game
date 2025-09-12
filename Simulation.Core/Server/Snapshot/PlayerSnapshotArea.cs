using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Simulation.Core.Server.Snapshot;

public class PlayerSnapshotArea(ILogger<PlayerSnapshotArea> logger) : IPlayerSnapshotArea
{
    private readonly ConcurrentQueue<MapSnapshotPacket> _joinGameSnapshots = new();
    private readonly ConcurrentQueue<MapSnapshotPacket> _leaveGameSnapshots = new();
    public void StageJoinGameSnapshot(MapSnapshotPacket packet)
    {
        _joinGameSnapshots.Enqueue(packet);
        logger.LogDebug("Staged JoinGameSnapshot for PlayerId {PlayerId} on MapId {MapId}", packet.PlayerId, packet.MapId);
    }

    public bool TryDequeueJoinGameSnapshot(out MapSnapshotPacket snapshot)
    {
        var result = _joinGameSnapshots.TryDequeue(out snapshot);
        if (result)
        {
            logger.LogDebug("Dequeued JoinGameSnapshot for PlayerId {PlayerId} on MapId {MapId}", snapshot!.PlayerId, snapshot.MapId);
        }
        return result;
    }
}
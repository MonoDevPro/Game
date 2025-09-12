using Simulation.Core.Shared.Network;
using Simulation.Core.Shared.Templates;

namespace Simulation.Core.Server.Snapshot;

public interface IPlayerSnapshotArea
{
    void StageJoinGameSnapshot(MapSnapshotPacket packet);
    bool TryDequeueJoinGameSnapshot(out MapSnapshotPacket snapshot);
}
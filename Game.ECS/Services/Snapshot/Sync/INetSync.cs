namespace Game.ECS.Services.Snapshot.Sync;

public interface INetSync
{
    void SendToAllReliable<T>(ref T packet) where T : struct;
    void SendToAllUnreliable<T>(ref T packet) where T : struct;
    void SendToReliable<T>(int networkId, ref T packet) where T : struct;
    void SendToUnreliable<T>(int networkId, ref T packet) where T : struct;
}
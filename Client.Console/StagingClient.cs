using System.Collections.Concurrent;
using Simulation.Core.ECS.Data;
using Simulation.Core.ECS.Staging;

namespace Client.Console;

public class WorldStagingClient : IWorldStaging
{
    private readonly ConcurrentQueue<PlayerData> _playerLogins = new();
    private readonly ConcurrentQueue<int> _playerLeaves = new();

    public void Enqueue<T>(StagingQueue queue, T item)
    {
        switch (queue)
        {
            case StagingQueue.PlayerLogin when item is PlayerData p:
                _playerLogins.Enqueue(p);
                break;
            case StagingQueue.PlayerLeave when item is int id:
                _playerLeaves.Enqueue(id);
                break;
            default:
                throw new ArgumentException($"Tipo inválido {typeof(T).Name} para fila {queue}");
        }
    }

    public bool TryDequeue<T>(StagingQueue queue, out T item)
    {
        object? result = null;
        bool ok = queue switch
        {
            StagingQueue.PlayerLogin => _playerLogins.TryDequeue(out var p) && (result = p) != null,
            StagingQueue.PlayerLeave => _playerLeaves.TryDequeue(out var id) && (result = id) != null,
            _ => false
        };
        if (ok && result is T cast)
        {
            item = cast;
            return true;
        }
        item = default!;
        return false;
    }

    public void StageSave(PlayerData data) { /* no-op no cliente */ }
    public void StageSave(MapData data) { /* no-op no cliente */ }
}

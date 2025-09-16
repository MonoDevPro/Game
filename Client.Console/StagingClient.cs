using System.Collections.Concurrent;
using Simulation.Core.ECS.Staging;
using Simulation.Core.ECS.Staging.Map;
using Simulation.Core.ECS.Staging.Player;

namespace Client.Console;

public class WorldStagingClient : IWorldStaging
{
    private readonly ConcurrentQueue<PlayerData> _playerLogins = new();
    private readonly ConcurrentQueue<int> _playerLeaves = new();
    private readonly ConcurrentQueue<MapData> _mapLoaded = new();

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
            case StagingQueue.MapLoaded when item is MapData m:
                _mapLoaded.Enqueue(m);
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
            StagingQueue.PlayerLogin => _playerLogins.TryDequeue(out var p) ? (result = p) != null : false,
            StagingQueue.PlayerLeave => _playerLeaves.TryDequeue(out var id) ? (result = id) != null : false,
            StagingQueue.MapLoaded  => _mapLoaded.TryDequeue(out var m) ? (result = m) != null : false,
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

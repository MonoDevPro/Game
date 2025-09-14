using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.ECS.Shared.Staging;

namespace Client.Console;

public class MapStagingArea() : IMapStagingArea
{
    private readonly ConcurrentQueue<MapData> _pendingMapLoaded = new();

    public void StageMapLoaded(MapData model)
    {
        _pendingMapLoaded.Enqueue(model);
    }

    public bool TryDequeueMapLoaded(out MapData data) 
        => _pendingMapLoaded.TryDequeue(out data);

    public void StageSave(MapData data)
    {
    }
}

public class PlayerStagingArea(ILogger<PlayerStagingArea> logger) : IPlayerStagingArea
{
    private readonly ConcurrentQueue<PlayerData> _pendingLogins = new();
    private readonly ConcurrentQueue<int> _pendingLeaves = new();

    public void StageLogin(PlayerData data)
    {
        try
        {
            _pendingLogins.Enqueue(data);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao adicionar PlayerData ao repositório de memória para CharId {CharId}", data.Id);
        }
    }

    public bool TryDequeueLogin(out PlayerData data) 
        => _pendingLogins.TryDequeue(out data);

    public void StageLeave(int playerId)
        => _pendingLeaves.Enqueue(playerId);

    public bool TryDequeueLeave(out int playerId)
    {
        return _pendingLeaves.TryDequeue(out playerId);
    }

    public void StageSave(PlayerData data)
    {
    }
}
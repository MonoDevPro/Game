using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.Server.Persistence.Contracts;
using Simulation.Core.Server.Staging;
using Simulation.Core.Shared.Templates;

namespace Server.Persistence.Staging;

public class PlayerStagingArea(IBackgroundTaskQueue saveQueue, ILogger<PlayerStagingArea> logger,
    IRepository<int, MapData> mapMemory, IRepository<int, PlayerData> playerMemory) : IPlayerStagingArea
{
    private readonly ConcurrentQueue<PlayerData> _pendingLogins = new();
    private readonly ConcurrentQueue<int> _pendingLeaves = new();

    public void StageLogin(PlayerData data)
    {
        try
        {
            playerMemory.Add(data.Id, data);
            _pendingLogins.Enqueue(data);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao adicionar PlayerData ao repositório de memória para CharId {CharId}", data.Id);
        }
    }

    public bool TryDequeueLogin(out PlayerData? data) 
        => _pendingLogins.TryDequeue(out data);

    public void StageLeave(int charId)
        => _pendingLeaves.Enqueue(charId);

    public bool TryDequeueLeave(out int charId)
    {
        return _pendingLeaves.TryDequeue(out charId);
    }

    public void StageSave(PlayerData data)
    {
        saveQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            var repo = sp.GetRequiredService<IRepositoryAsync<int, PlayerData>>();
            await repo.UpdateAsync(data.Id, data, ct);
        });
    }
}
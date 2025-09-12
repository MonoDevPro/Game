using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.Models;
using Simulation.Core.Persistence.Contracts;

namespace Server.Persistence.Staging;

public class PlayerStagingArea(IBackgroundTaskQueue saveQueue, ILogger<PlayerStagingArea> logger,
    IRepository<int, MapModel> mapMemory, IRepository<int, PlayerModel> playerMemory) : IPlayerStagingArea
{
    private readonly ConcurrentQueue<PlayerModel> _pendingLogins = new();
    private readonly ConcurrentQueue<int> _pendingLeaves = new();

    public void StageLogin(PlayerModel model)
    {
        try
        {
            playerMemory.Add(model.Id, model);
            _pendingLogins.Enqueue(model);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Erro ao adicionar PlayerData ao repositório de memória para CharId {CharId}", model.Id);
        }
    }

    public bool TryDequeueLogin(out PlayerModel? data) 
        => _pendingLogins.TryDequeue(out data);

    public void StageLeave(int charId)
        => _pendingLeaves.Enqueue(charId);

    public bool TryDequeueLeave(out int charId)
    {
        return _pendingLeaves.TryDequeue(out charId);
    }

    public void StageSave(PlayerModel model)
    {
        saveQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            var repo = sp.GetRequiredService<IRepositoryAsync<int, PlayerModel>>();
            await repo.UpdateAsync(model.Id, model, ct);
        });
    }
}
using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Simulation.Core.ECS.Server.Staging;
using Simulation.Core.ECS.Shared.Data;
using Simulation.Core.Persistence.Contracts;
using Simulation.Core.Persistence.Models;

namespace Server.Persistence.Staging;

public class PlayerStagingArea(IBackgroundTaskQueue saveQueue, ILogger<PlayerStagingArea> logger) : IPlayerStagingArea
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
        saveQueue.QueueBackgroundWorkItem(async (sp, ct) =>
        {
            // Resolve o repositório específico
            var repo = sp.GetRequiredService<IPlayerRepository>();
            // Chama o método que entende de PlayerData
            await repo.UpdateFromDataAsync(data, ct);
        });
    }
}
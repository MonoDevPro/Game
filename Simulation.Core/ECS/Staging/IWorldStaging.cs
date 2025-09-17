
using Simulation.Core.ECS.Components;

namespace Simulation.Core.ECS.Staging;

public enum StagingQueue
{
    PlayerLogin,
    PlayerLeave,
}

/// <summary>
/// Área de staging unificada com sub-filas nomeadas para eventos de jogo.
/// </summary>
public interface IWorldStaging
{
    void Enqueue<T>(StagingQueue queue, T item);
    bool TryDequeue<T>(StagingQueue queue, out T item);

    // Operações de persistência assíncrona
    void StageSave(PlayerData data);
}
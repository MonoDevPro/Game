
using Simulation.Core.ECS.Staging.Map;
using Simulation.Core.ECS.Staging.Player;

namespace Simulation.Core.ECS.Staging;

public enum StagingQueue
{
    PlayerLogin,
    PlayerLeave,
    MapLoaded
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
    void StageSave(MapData data);
}

namespace Simulation.Core.Ports;

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
}
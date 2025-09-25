using Simulation.Core.ECS.Components;

namespace Simulation.Core.Ports.ECS;

/// <summary>
/// Área de staging unificada com sub-filas nomeadas para eventos de jogo.
/// </summary>
public interface IWorldInbox
{
    void Enqueue<T>(SpawnPlayerRequest queue);
    void Enqueue<T>(DespawnPlayerRequest queue);
}
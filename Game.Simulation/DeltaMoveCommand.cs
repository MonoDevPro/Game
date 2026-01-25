using Arch.Core;
using Game.Infrastructure.ArchECS.Commons.Components;
using Game.Infrastructure.ArchECS.Services.Navigation;

namespace Game.Simulation;

/// <summary>
/// Comando para mover uma entidade por um delta de posição.
/// </summary>
/// <param name="CharacterId">ID do personagem a ser movido.</param>
/// <param name="Dx">Delta X (direção horizontal).</param>
/// <param name="Dy">Delta Y (direção vertical).</param>
public sealed record DeltaMoveCommand(int CharacterId, int Dx, int Dy) : ISimulationCommand
{
    public bool Execute(IWorldSimulation simulation)
    {
        return simulation.RequestPlayerMoveDelta(CharacterId, Dx, Dy);
    }
}

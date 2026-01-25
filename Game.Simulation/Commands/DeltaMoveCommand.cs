namespace Game.Simulation.Commands;

/// <summary>
/// Comando para mover uma entidade por um delta de posição.
/// </summary>
/// <param name="CharacterId">ID do personagem a ser movido.</param>
/// <param name="Dx">Delta X (direção horizontal).</param>
/// <param name="Dy">Delta Y (direção vertical).</param>
public sealed record DeltaMoveCommand(int CharacterId, int Dx, int Dy) : IWorldCommand
{
    public bool Execute(IWorldSimulation simulation)
    {
        return simulation.RequestPlayerMoveDelta(CharacterId, Dx, Dy);
    }
}

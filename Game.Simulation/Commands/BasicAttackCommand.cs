using Game.Simulation;

namespace Game.Simulation.Commands;

/// <summary>
/// Comando para ataque básico direcional.
/// </summary>
/// <param name="CharacterId">ID do personagem atacante.</param>
/// <param name="DirX">Direção X (-1, 0, 1).</param>
/// <param name="DirY">Direção Y (-1, 0, 1).</param>
public sealed record BasicAttackCommand(int CharacterId, int DirX, int DirY) : IWorldCommand
{
    public bool Execute(IWorldSimulation simulation)
    {
        return simulation.RequestBasicAttack(CharacterId, DirX, DirY);
    }
}

using Game.Infrastructure.ArchECS.Services.Navigation.Components;

namespace Game.Simulation.Commands;

/// <summary>
/// Comando para solicitar movimento de um jogador usando pathfinding.
/// Requer que a simulação tenha sido criada com suporte a navegação.
/// </summary>
/// <param name="CharacterId">ID do personagem.</param>
/// <param name="TargetX">Posição X de destino.</param>
/// <param name="TargetY">Posição Y de destino.</param>
/// <param name="TargetFloor">Andar de destino (padrão 0).</param>
/// <param name="Flags">Flags de pathfinding.</param>
public sealed record NavigateCommand(
    int CharacterId, 
    int TargetX, 
    int TargetY, 
    int TargetFloor = 0,
    PathRequestFlags Flags = PathRequestFlags.None) : IWorldCommand
{
    /// <summary>
    /// Executa o comando de navegação usando a simulação completa.
    /// </summary>
    /// <param name="simulation">Simulação do servidor com suporte a navegação.</param>
    /// <returns>True se a navegação foi iniciada com sucesso.</returns>
    public bool Execute(IWorldSimulation simulation)
    {
        return simulation.RequestPlayerMove(CharacterId, TargetX, TargetY, TargetFloor, Flags);
    }
}

/// <summary>
/// Comando para parar o movimento de um jogador.
/// </summary>
/// <param name="CharacterId">ID do personagem.</param>
public sealed record StopMoveCommand(int CharacterId) : IWorldCommand
{
    public bool Execute(IWorldSimulation simulation)
    {
        return simulation.StopPlayerMove(CharacterId);
    }
}

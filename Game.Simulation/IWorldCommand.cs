using Arch.Core;

namespace Game.Simulation;

/// <summary>
/// Interface base para comandos do mundo.
/// Comandos são ações que modificam o estado do mundo ECS de forma determinística.
/// </summary>
public interface IWorldCommand
{
    /// <summary>
    /// Aplica o comando ao mundo ECS.
    /// </summary>
    /// <param name="world">O mundo ECS onde o comando será aplicado.</param>
    void Execute(World world);
}

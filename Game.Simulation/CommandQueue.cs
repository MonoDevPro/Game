using System.Collections.Concurrent;
using Arch.Core;

namespace Game.Simulation;

/// <summary>
/// Fila thread-safe para comandos do mundo.
/// Permite enfileirar comandos de múltiplas threads (ex: handlers de rede)
/// e processá-los de forma determinística no tick de simulação.
/// </summary>
public sealed class CommandQueue
{
    private readonly ConcurrentQueue<IWorldCommand> _queue = new();

    /// <summary>
    /// Número de comandos pendentes na fila.
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// Enfileira um comando para processamento posterior.
    /// Thread-safe: pode ser chamado de qualquer thread.
    /// </summary>
    /// <param name="command">Comando a ser enfileirado.</param>
    public void Enqueue(IWorldCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _queue.Enqueue(command);
    }

    /// <summary>
    /// Processa todos os comandos pendentes no mundo ECS.
    /// Deve ser chamado apenas da thread de simulação.
    /// </summary>
    /// <param name="world">Mundo ECS onde os comandos serão aplicados.</param>
    /// <returns>Número de comandos processados.</returns>
    public int Drain(World world)
    {
        ArgumentNullException.ThrowIfNull(world);
        
        var count = 0;
        while (_queue.TryDequeue(out var command))
        {
            command.Execute(world);
            count++;
        }

        return count;
    }

    /// <summary>
    /// Processa todos os comandos pendentes usando a simulação do servidor.
    /// Suporta comandos especiais de navegação que requerem ServerWorldSimulation.
    /// </summary>
    /// <param name="simulation">Simulação do servidor.</param>
    /// <returns>Número de comandos processados.</returns>
    public int Drain(ServerWorldSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        
        var count = 0;
        while (_queue.TryDequeue(out var command))
        {
            // Trata comandos especiais de navegação
            switch (command)
            {
                case NavigateCommand navCmd:
                    navCmd.Execute(simulation);
                    break;
                case StopMoveCommand stopCmd:
                    stopCmd.Execute(simulation);
                    break;
                default:
                    command.Execute(simulation.World);
                    break;
            }
            count++;
        }

        return count;
    }

    /// <summary>
    /// Processa todos os comandos pendentes usando a interface de simulação.
    /// Para comandos de navegação, prefira usar Drain(ServerWorldSimulation).
    /// </summary>
    /// <param name="simulation">Simulação do servidor.</param>
    /// <returns>Número de comandos processados.</returns>
    public int Drain(IWorldSimulation simulation)
    {
        ArgumentNullException.ThrowIfNull(simulation);
        
        // Se é ServerWorldSimulation, usa o método específico para suportar navegação
        if (simulation is ServerWorldSimulation serverSim)
            return Drain(serverSim);
            
        return Drain(simulation.World);
    }

    /// <summary>
    /// Limpa todos os comandos pendentes sem processá-los.
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}

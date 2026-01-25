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
    private readonly ConcurrentQueue<ISimulationCommand> _queue = new();

    /// <summary>
    /// Número de comandos pendentes na fila.
    /// </summary>
    public int Count => _queue.Count;

    /// <summary>
    /// Enfileira um comando para processamento posterior.
    /// Thread-safe: pode ser chamado de qualquer thread.
    /// </summary>
    /// <param name="command">Comando a ser enfileirado.</param>
    public void Enqueue(ISimulationCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);
        _queue.Enqueue(command);
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
            
        var count = 0;
        while (_queue.TryDequeue(out var command))
        {
            command.Execute(simulation);
            count++;
        }
        return count;
    }

    /// <summary>
    /// Limpa todos os comandos pendentes sem processá-los.
    /// </summary>
    public void Clear()
    {
        while (_queue.TryDequeue(out _)) { }
    }
}

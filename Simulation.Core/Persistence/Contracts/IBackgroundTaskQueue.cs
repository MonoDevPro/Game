namespace Simulation.Core.Persistence.Contracts;

/// <summary>
/// Define um contrato para uma fila de tarefas em segundo plano que podem ser enfileiradas
/// e processadas de forma assíncrona.
/// </summary>
public interface IBackgroundTaskQueue
{
    /// <summary>
    /// Enfileira uma nova tarefa para ser executada em segundo plano.
    /// A tarefa é um delegate que recebe o IServiceProvider de um escopo criado para ela.
    /// </summary>
    /// <param name="workItem">A unidade de trabalho a ser executada.</param>
    void QueueBackgroundWorkItem(Func<IServiceProvider, CancellationToken, ValueTask> workItem);

    /// <summary>
    /// Aguarda e retira uma tarefa da fila de forma assíncrona.
    /// </summary>
    /// <param name="cancellationToken">Token para cancelar a operação de espera.</param>
    /// <returns>A tarefa a ser executada.</returns>
    ValueTask<Func<IServiceProvider, CancellationToken, ValueTask>> DequeueAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Obtém o número atual de itens na fila.
    /// </summary>
    int Count { get; }
}

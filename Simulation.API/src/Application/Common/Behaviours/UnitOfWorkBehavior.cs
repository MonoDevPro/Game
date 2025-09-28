using GameWeb.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace GameWeb.Application.Common.Behaviours;

public class UnitOfWorkBehavior<TRequest, TResponse>(
    IUnitOfWork uow,
    ILogger<UnitOfWorkBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var isCommand = request is ICommand || request is ICommand<TResponse>;

        if (!isCommand)
            return await next();

        // Tenta iniciar a transação e guarda se nós a iniciamos
        var weStartedTransaction = await uow.BeginTransactionAsync(cancellationToken);

        try
        {
            var response = await next();

            if (!weStartedTransaction)
                return response;
            
            await uow.SaveChangesAsync(cancellationToken); 
            await uow.CommitTransactionAsync(cancellationToken);
            logger.LogInformation("Transactional unit of work committed for {Request}", typeof(TRequest).Name);

            return response;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Transactional unit of work failed for {Request}, attempting rollback", typeof(TRequest).Name);

            if (weStartedTransaction)
            {
                try
                {
                    await uow.RollbackTransactionAsync(cancellationToken);
                }
                catch (Exception rbEx)
                {
                    logger.LogError(rbEx, "Rollback also failed for {Request}", typeof(TRequest).Name);
                }
            }

            throw;
        }
    }
}

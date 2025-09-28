namespace GameWeb.Application.Common.Interfaces;

/// <summary>
/// Marcador para comandos (alteram estado) – usado para behaviors condicionais como UnitOfWork.
/// </summary>
public interface ICommand<out TResponse> : IRequest<TResponse> { }

/// <summary>
/// Marcador para comandos sem retorno específico.
/// </summary>
public interface ICommand : IRequest<Unit> { }

/// <summary>
/// Marcador para queries (somente leitura) – não devem disparar UnitOfWork.
/// </summary>
public interface IQuery<out TResponse> : IRequest<TResponse> { }

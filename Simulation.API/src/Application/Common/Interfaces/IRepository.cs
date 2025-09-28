using System.Linq.Expressions;
using GameWeb.Application.Common.Models;
using GameWeb.Domain.Common;

namespace GameWeb.Application.Common.Interfaces;

public interface IRepository<T> where T : BaseEntity
{
    // --- MÉTODOS DE LEITURA ---
    Task<T?> GetBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<TResult?> GetBySpecAsync<TResult>(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<List<T>> ListBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<List<TResult>> ListBySpecAsync<TResult>(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<PaginatedList<T>> ListBySpecAsync(ISpecification<T> spec, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    Task<PaginatedList<TResult>> ListBySpecAsync<TResult>(ISpecification<T> spec, int pageNumber, int pageSize, CancellationToken cancellationToken = default);
    
    // --- NOVO MÉTODO PARA VERIFICAÇÃO DE EXISTÊNCIA ---
    Task<bool> AnyBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    Task<int> CountBySpecAsync(ISpecification<T> spec, CancellationToken cancellationToken = default);
    
    // --- MÉTODOS DE ESCRITA ---
    void Add(T entity);
    void Update(T entity);
    void Delete(T entity);
}

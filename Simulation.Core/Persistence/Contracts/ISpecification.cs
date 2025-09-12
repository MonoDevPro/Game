using System.Linq.Expressions;

namespace Simulation.Core.Persistence.Contracts;

// Define a forma de uma consulta que pode ser aplicada a um IQueryable ou a uma coleção em memória.
public interface ISpecification<T>
{
    // O critério de filtro (cláusula WHERE)
    Expression<Func<T, bool>> Criteria { get; }

    // Lista de includes para eager loading (usado pelo EF Core)
    List<Expression<Func<T, object>>> Includes { get; }

    // Expressão de ordenação
    Expression<Func<T, object>>? OrderBy { get; }

    // Expressão de ordenação descendente
    Expression<Func<T, object>>? OrderByDescending { get; }
}
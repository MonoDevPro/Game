using System.Linq.Expressions;

namespace GameWeb.Application.Common.Interfaces;

public interface ISpecification<T>
{
    // filtro WHERE (nullable = permite specs "abertas")
    Expression<Func<T, bool>>? Criteria { get; }

    // includes por expressão (strongly-typed)
    List<Expression<Func<T, object>>> Includes { get; }

    // includes por string (ex.: "Orders.OrderLines")
    List<string> IncludeStrings { get; }

    // orderings: list preserving order; bool = descending
    List<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy { get; }

    // opção de projeção (se preenchida, o repositório usa isso em vez do AutoMapper)
    LambdaExpression? Selector { get; }

    // Paging
    int? Skip { get; }
    int? Take { get; }
    bool IsPagingEnabled { get; }

    // Flags úteis
    bool AsNoTracking { get; }
    bool IgnoreQueryFilters { get; }
}

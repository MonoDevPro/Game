using System.Linq.Expressions;

namespace GameWeb.Application.Common.Interfaces;

public abstract class BaseSpecification<T>(Expression<Func<T, bool>>? criteria = null) : ISpecification<T>
{
    public Expression<Func<T, bool>>? Criteria { get; } = criteria;
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public List<string> IncludeStrings { get; } = new();
    public List<(Expression<Func<T, object>> KeySelector, bool Descending)> OrderBy { get; } = new();
    public LambdaExpression? Selector { get; private set; }
    public int? Skip { get; private set; }
    public int? Take { get; private set; }
    public bool IsPagingEnabled { get; private set; }
    public bool AsNoTracking { get; private set; }
    public bool IgnoreQueryFilters { get; private set; }

    protected void AddInclude(Expression<Func<T, object>> include) => Includes.Add(include);
    protected void AddInclude(string includeString) => IncludeStrings.Add(includeString);

    protected void AddOrderBy(Expression<Func<T, object>> orderByExpression)
    {
        OrderBy.Add((orderByExpression, false));
    }
    
    protected void AddOrderByDescending(Expression<Func<T, object>> orderByDescendingExpression)
    {
        OrderBy.Add((orderByDescendingExpression, true));
    }

    protected void ApplyPaging(int skip, int take)
    {
        Skip = skip;
        Take = take;
        IsPagingEnabled = true;
    }

    protected void AsNoTrackingEnabled() => AsNoTracking = true;
    protected void IgnoreQueryFiltersEnabled() => IgnoreQueryFilters = true;

    protected void AddSelect<TResult>(Expression<Func<T, TResult>> selector) => Selector = selector;
}

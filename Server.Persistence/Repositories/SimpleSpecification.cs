using System.Linq.Expressions;
using Simulation.Core.Persistence.Contracts;

namespace Server.Persistence.Repositories;

public class SimpleSpecification<T>(Expression<Func<T, bool>> criteria) : ISpecification<T>
{
    public Expression<Func<T, bool>> Criteria { get; } = criteria ?? throw new ArgumentNullException(nameof(criteria));
    public List<Expression<Func<T, object>>> Includes { get; } = new();
    public Expression<Func<T, object>>? OrderBy { get; set; }
    public Expression<Func<T, object>>? OrderByDescending { get; set; }
}
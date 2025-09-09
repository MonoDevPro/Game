namespace Simulation.Core.Server.Persistence;

public record PagedResult<T>(IReadOnlyList<T> Items, int TotalCount, int Page, int PageSize);
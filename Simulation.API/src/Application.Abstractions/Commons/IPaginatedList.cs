namespace Application.Abstractions.Commons;

public interface IPaginatedList<out T>
{
    IReadOnlyCollection<T> Items { get; }
    int PageNumber { get; }
    int TotalPages { get; }
    long TotalCount { get; }
    bool HasPreviousPage { get; }
    bool HasNextPage { get; }
}

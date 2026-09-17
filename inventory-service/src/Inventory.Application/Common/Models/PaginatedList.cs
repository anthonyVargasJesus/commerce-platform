namespace Inventory.Application.Common.Models;

public sealed class PaginatedList<T>
{
    public PaginatedList(IReadOnlyList<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        PageNumber = pageNumber;
        TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int PageNumber { get; }

    public int TotalPages { get; }

    public int TotalCount { get; }

    public bool HasPreviousPage => PageNumber > 1;

    public bool HasNextPage => PageNumber < TotalPages;
}

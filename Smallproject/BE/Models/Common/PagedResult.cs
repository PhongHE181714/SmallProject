namespace BE.Models.Common;

public sealed class PagedResult<T>
{
    public required IReadOnlyList<T> Items { get; init; }

    public long TotalItems { get; init; }
}

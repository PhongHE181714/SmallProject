namespace BE.DTOs.SaleOuts;

public sealed class SaleOutSearchResponse
{
    public required IReadOnlyList<SaleOutOrderListItemResponse> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalItems { get; init; }

    public long TotalPages { get; init; }
}

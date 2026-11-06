namespace BE.DTOs.Products;

public sealed class ProductSearchResponse
{
    public required IReadOnlyList<ProductListItemResponse> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalItems { get; init; }

    public long TotalPages { get; init; }
}

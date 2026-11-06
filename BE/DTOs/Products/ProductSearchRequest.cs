using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.Products;

public static class ProductSearchLimits
{
    public const int MAX_PAGE_SIZE = 200;
}

public sealed class ProductSearchRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, ProductSearchLimits.MAX_PAGE_SIZE)]
    public int PageSize { get; init; } = 5;

    public string? SearchField { get; init; }

    public string? SearchValue { get; init; }
}

namespace BE.Models.MasterProduct;

public enum ProductSearchField
{
    ProductCode,
    ProductName,
    Unit,
    Specification,
    QuantityPerBox,
    ProductWeight
}

public sealed class ProductSearchCriteria
{
    public ProductSearchField? SearchField { get; init; }

    public string? SearchText { get; init; }

    public decimal? SearchNumber { get; init; }

    public long Offset { get; init; }

    public int PageSize { get; init; }
}


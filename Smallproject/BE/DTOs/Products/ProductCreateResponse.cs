namespace BE.DTOs.Products;

public sealed class ProductCreateResponse
{
    public Guid Id { get; init; }

    public string? ProductCode { get; init; }

    public string? ProductName { get; init; }

    public string? Unit { get; init; }

    public string? Specification { get; init; }

    public decimal? QuantityPerBox { get; init; }

    public decimal? ProductWeight { get; init; }

    public bool IsActive { get; init; }
}

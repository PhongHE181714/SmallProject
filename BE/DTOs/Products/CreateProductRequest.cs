namespace BE.DTOs.Products;

public sealed class CreateProductRequest
{
    public string? ProductCode { get; init; }

    public string? ProductName { get; init; }

    public string? Unit { get; init; }

    public string? Specification { get; init; }

    public int? QuantityPerBox { get; init; }

    public decimal? ProductWeight { get; init; }
}

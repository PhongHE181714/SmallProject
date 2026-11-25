namespace BE.Services.CatalogService.Application.DTOs;

public sealed class ProductListItemResponse
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

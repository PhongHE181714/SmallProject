namespace BE.DTOs.SaleOuts;

public sealed class SaleOutListItemResponse
{
    public Guid Id { get; init; }

    public string? SaleOutNo { get; init; }

    public string? CustomerPoNo { get; init; }

    public DateOnly? OrderDate { get; init; }

    public string? CustomerName { get; init; }

    public Guid? ProductId { get; init; }

    public string? ProductCode { get; init; }

    public string? ProductName { get; init; }

    public string? Unit { get; init; }

    public decimal? Quantity { get; init; }

    public decimal? Price { get; init; }

    public decimal? Amount { get; init; }

    public decimal? QuantityPerBox { get; init; }

    public decimal? BoxQuantity { get; init; }

    public bool IsActive { get; init; }
}

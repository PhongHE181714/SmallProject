namespace BE.DTOs.SaleOuts;

public sealed class SaleOutOrderListItemResponse
{
    public string? SaleOutNo { get; init; }

    public string? CustomerPoNo { get; init; }

    public DateOnly? OrderDate { get; init; }

    public string? CustomerName { get; init; }

    public long ProductCount { get; init; }

    public decimal TotalAmount { get; init; }

    public required IReadOnlyList<SaleOutListItemResponse> Products { get; init; }
}

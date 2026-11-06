namespace BE.Models.SaleOut;

public sealed class SaleOutOrder
{
    public string? SaleOutNo { get; init; }

    public string? CustomerPoNo { get; init; }

    public DateTime? OrderDate { get; init; }

    public string? CustomerName { get; init; }

    public long ProductCount { get; init; }

    public decimal TotalAmount { get; init; }

    public IReadOnlyList<SaleOut> Products { get; init; } = [];
}

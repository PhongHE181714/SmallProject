namespace BE.Models.SaleOut;

public sealed class SaleOutRevenueReportItem
{
    public string? ProductCode { get; init; }

    public string? ProductName { get; init; }

    public decimal Quantity { get; init; }

    public decimal Amount { get; init; }
}

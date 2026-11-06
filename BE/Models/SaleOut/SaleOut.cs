namespace BE.Models.SaleOut;

public sealed class SaleOut
{
    public Guid Id { get; set; }

    public string? SaleOutNo { get; set; }

    public string? CustomerPoNo { get; set; }

    public DateTime? OrderDate { get; set; }

    public string? CustomerName { get; set; }

    public Guid? ProductId { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public string? Unit { get; set; }

    public decimal? Quantity { get; set; }

    public decimal? Price { get; set; }

    public decimal? Amount { get; set; }

    public decimal? QuantityPerBox { get; set; }

    public decimal? BoxQuantity { get; set; }

    public bool IsActive { get; set; }
}

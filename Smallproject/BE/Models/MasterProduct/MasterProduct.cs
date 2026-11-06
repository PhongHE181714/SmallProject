namespace BE.Models.MasterProduct;

public sealed class MasterProduct
{
    public Guid Id { get; set; }

    public string? ProductCode { get; set; }

    public string? ProductName { get; set; }

    public string? Unit { get; set; }

    public string? Specification { get; set; }

    public decimal? QuantityPerBox { get; set; }

    public decimal? ProductWeight { get; set; }

    public bool IsActive { get; set; }
}

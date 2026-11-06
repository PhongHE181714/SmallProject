using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.SaleOuts;

public static class SaleOutCreateLimits
{
    public const int CUSTOMER_PO_NO_MAX_LENGTH = 50;
    public const int CUSTOMER_NAME_MAX_LENGTH = 200;
    public const string MAX_DECIMAL_18_3_TEXT = "999999999999999.999";
}

public sealed class CreateSaleOutRequest
{
    [Required]
    [StringLength(SaleOutCreateLimits.CUSTOMER_PO_NO_MAX_LENGTH)]
    public string? CustomerPoNo { get; init; }

    [Required]
    public DateOnly? OrderDate { get; init; }

    [Required]
    [StringLength(SaleOutCreateLimits.CUSTOMER_NAME_MAX_LENGTH)]
    public string? CustomerName { get; init; }

    [Required]
    [MinLength(1)]
    public IReadOnlyList<CreateSaleOutItemRequest>? Items { get; init; }
}

public sealed class CreateSaleOutItemRequest
{
    [Required]
    public Guid? ProductId { get; init; }

    [Required]
    [Range(typeof(decimal), "1", SaleOutCreateLimits.MAX_DECIMAL_18_3_TEXT)]
    public decimal? Quantity { get; init; }

    [Required]
    [Range(typeof(decimal), "0", SaleOutCreateLimits.MAX_DECIMAL_18_3_TEXT)]
    public decimal? Price { get; init; }

    [Required]
    [Range(typeof(decimal), "1", SaleOutCreateLimits.MAX_DECIMAL_18_3_TEXT)]
    public decimal? QuantityPerBox { get; init; }
}

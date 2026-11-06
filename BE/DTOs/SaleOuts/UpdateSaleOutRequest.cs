using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.SaleOuts;

public sealed class UpdateSaleOutRequest
{
    [Required]
    [Range(typeof(decimal), "1", SaleOutCreateLimits.MAX_DECIMAL_18_3_TEXT)]
    public decimal? Quantity { get; init; }

    [Required]
    [Range(typeof(decimal), "1", SaleOutCreateLimits.MAX_DECIMAL_18_3_TEXT)]
    public decimal? QuantityPerBox { get; init; }

    [Required]
    [Range(typeof(decimal), "0", SaleOutCreateLimits.MAX_DECIMAL_18_3_TEXT)]
    public decimal? Price { get; init; }
}

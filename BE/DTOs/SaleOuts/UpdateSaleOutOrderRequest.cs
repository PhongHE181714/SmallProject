using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.SaleOuts;

public sealed class UpdateSaleOutOrderRequest
{
    [Required]
    [MinLength(1)]
    public IReadOnlyList<UpdateSaleOutOrderItemRequest>? Items { get; init; }
}

public sealed class UpdateSaleOutOrderItemRequest
{
    public Guid? Id { get; init; }

    [Required]
    public Guid? ProductId { get; init; }

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

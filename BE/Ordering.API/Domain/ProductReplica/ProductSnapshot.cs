namespace BE.Services.OrderingService.Domain.ProductReplica;

public sealed class ProductSnapshot
{
    public Guid Id { get; init; }

    public string? ProductCode { get; init; }

    public string? ProductName { get; init; }

    public string? Unit { get; init; }

    public decimal? QuantityPerBox { get; init; }

    public bool IsActive { get; init; }
}

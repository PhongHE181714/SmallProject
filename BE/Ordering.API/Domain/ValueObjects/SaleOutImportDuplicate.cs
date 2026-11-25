namespace BE.Services.OrderingService.Domain.ValueObjects;

public sealed class SaleOutImportDuplicate
{
    public string CustomerPoNo { get; init; } = string.Empty;

    public Guid ProductId { get; init; }

    public bool IsOrderHeaderMismatch { get; init; }
}

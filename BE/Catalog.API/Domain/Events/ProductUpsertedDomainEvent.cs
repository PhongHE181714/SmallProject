using BE.BuildingBlocks.SharedKernel;

namespace BE.Services.CatalogService.Domain.Events;

public sealed record ProductUpsertedDomainEvent(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    string Specification,
    decimal QuantityPerBox,
    decimal ProductWeight,
    bool IsActive) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

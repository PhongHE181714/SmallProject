using BE.BuildingBlocks.SharedKernel;

namespace BE.Services.OrderingService.Domain.Events;

public sealed record SaleOutOrderCreatedDomainEvent(
    string CustomerPoNo,
    string CustomerName,
    DateOnly OrderDate,
    int ItemCount) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

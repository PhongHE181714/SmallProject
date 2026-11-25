using BE.BuildingBlocks.SharedKernel;

namespace BE.Services.CatalogService.Domain.Events;

public sealed record ProductDeletedDomainEvent(Guid ProductId) : IDomainEvent
{
    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;
}

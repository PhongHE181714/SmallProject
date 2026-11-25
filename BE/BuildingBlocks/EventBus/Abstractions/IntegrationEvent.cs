namespace BE.BuildingBlocks.EventBus.Abstractions;

public abstract record IntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();

    public DateTime OccurredOnUtc { get; init; } = DateTime.UtcNow;

    public abstract string RoutingKey { get; }
}

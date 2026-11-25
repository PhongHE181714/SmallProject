using BE.BuildingBlocks.EventBus.Abstractions;

namespace BE.BuildingBlocks.EventBus.Events;

public sealed record ProductDeletedIntegrationEvent(Guid ProductId) : IntegrationEvent
{
    public const string ROUTING_KEY = "catalog.product.deleted";

    public override string RoutingKey => ROUTING_KEY;
}

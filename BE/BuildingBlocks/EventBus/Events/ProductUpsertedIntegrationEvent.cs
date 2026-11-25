using BE.BuildingBlocks.EventBus.Abstractions;

namespace BE.BuildingBlocks.EventBus.Events;

public sealed record ProductUpsertedIntegrationEvent(
    Guid ProductId,
    string ProductCode,
    string ProductName,
    string Unit,
    string Specification,
    decimal QuantityPerBox,
    decimal ProductWeight,
    bool IsActive) : IntegrationEvent
{
    public const string ROUTING_KEY = "catalog.product.upserted";

    public override string RoutingKey => ROUTING_KEY;
}

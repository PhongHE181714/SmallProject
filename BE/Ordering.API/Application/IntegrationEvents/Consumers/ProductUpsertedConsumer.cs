using BE.BuildingBlocks.EventBus.Abstractions;
using BE.BuildingBlocks.EventBus.Events;
using BE.Services.OrderingService.Domain.ProductReplica;

namespace BE.Services.OrderingService.Application.IntegrationEvents.Consumers;

public sealed class ProductUpsertedConsumer : IIntegrationEventHandler<ProductUpsertedIntegrationEvent>
{
    private readonly IProductSnapshotRepository _productSnapshotRepository;
    private readonly ILogger<ProductUpsertedConsumer> _logger;

    public ProductUpsertedConsumer(
        IProductSnapshotRepository productSnapshotRepository,
        ILogger<ProductUpsertedConsumer> logger)
    {
        _productSnapshotRepository = productSnapshotRepository;
        _logger = logger;
    }

    public Task HandleAsync(
        ProductUpsertedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        var snapshot = new ProductSnapshot
        {
            Id = integrationEvent.ProductId,
            ProductCode = integrationEvent.ProductCode,
            ProductName = integrationEvent.ProductName,
            Unit = integrationEvent.Unit,
            QuantityPerBox = integrationEvent.QuantityPerBox,
            IsActive = integrationEvent.IsActive
        };

        _productSnapshotRepository.UpsertSnapshot(snapshot);

        _logger.LogInformation(
            "[OrderingService Consumer] Đã đồng bộ ProductSnapshot từ RabbitMQ: ProductId={ProductId}, ProductCode={ProductCode}, IsActive={IsActive}.",
            integrationEvent.ProductId,
            integrationEvent.ProductCode,
            integrationEvent.IsActive);

        return Task.CompletedTask;
    }
}

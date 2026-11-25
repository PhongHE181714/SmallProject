using BE.BuildingBlocks.EventBus.Abstractions;
using BE.BuildingBlocks.EventBus.Events;
using BE.Services.OrderingService.Domain.ProductReplica;

namespace BE.Services.OrderingService.Application.IntegrationEvents.Consumers;

public sealed class ProductDeletedConsumer : IIntegrationEventHandler<ProductDeletedIntegrationEvent>
{
    private readonly IProductSnapshotRepository _productSnapshotRepository;
    private readonly ILogger<ProductDeletedConsumer> _logger;

    public ProductDeletedConsumer(
        IProductSnapshotRepository productSnapshotRepository,
        ILogger<ProductDeletedConsumer> logger)
    {
        _productSnapshotRepository = productSnapshotRepository;
        _logger = logger;
    }

    public Task HandleAsync(
        ProductDeletedIntegrationEvent integrationEvent,
        CancellationToken cancellationToken = default)
    {
        _productSnapshotRepository.RemoveSnapshot(integrationEvent.ProductId);

        _logger.LogInformation(
            "[OrderingService Consumer] Đã xóa ProductSnapshot khỏi bản sao cục bộ từ RabbitMQ: ProductId={ProductId}.",
            integrationEvent.ProductId);

        return Task.CompletedTask;
    }
}

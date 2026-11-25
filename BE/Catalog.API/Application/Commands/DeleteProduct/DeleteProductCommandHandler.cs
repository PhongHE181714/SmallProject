using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.EventBus.Abstractions;
using BE.BuildingBlocks.EventBus.Events;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.CatalogService.Domain.Repositories;

namespace BE.Services.CatalogService.Application.Commands.DeleteProduct;

public sealed class DeleteProductCommandHandler : ICommandHandler<DeleteProductCommand, Unit>
{
    private readonly IProductRepository _productRepository;
    private readonly IEventBus _eventBus;

    public DeleteProductCommandHandler(IProductRepository productRepository, IEventBus eventBus)
    {
        _productRepository = productRepository;
        _eventBus = eventBus;
    }

    public async Task<Unit> HandleAsync(
        DeleteProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var exists = await _productRepository.ExistsAsync(command.Id);
        if (!exists)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với Id: {command.Id}")
                .WithTitle("Không tìm thấy sản phẩm");
        }

        var hasRelatedTransactions = await _productRepository.HasRelatedTransactionsAsync(command.Id);
        if (hasRelatedTransactions)
        {
            throw new InvalidOperationException(
                "Sản phẩm đã phát sinh giao dịch xuất bán (SaleOut), không thể xóa.")
                .WithTitle("Không thể xóa sản phẩm");
        }

        await _productRepository.DeleteAsync(command.Id);

        await _eventBus.PublishAsync(
            new ProductDeletedIntegrationEvent(command.Id),
            cancellationToken);

        return Unit.VALUE;
    }
}

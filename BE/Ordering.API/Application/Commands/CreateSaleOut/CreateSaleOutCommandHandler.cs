using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Domain.Aggregates;
using BE.Services.OrderingService.Domain.ProductReplica;
using BE.Services.OrderingService.Domain.Repositories;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Application.Commands.CreateSaleOut;

public sealed class CreateSaleOutCommandHandler : ICommandHandler<CreateSaleOutCommand, SaleOutCreateResponse>
{
    private readonly ISaleOutRepository _saleOutRepository;
    private readonly IProductSnapshotRepository _productSnapshotRepository;

    public CreateSaleOutCommandHandler(
        ISaleOutRepository saleOutRepository,
        IProductSnapshotRepository productSnapshotRepository)
    {
        _saleOutRepository = saleOutRepository;
        _productSnapshotRepository = productSnapshotRepository;
    }

    public async Task<SaleOutCreateResponse> HandleAsync(
        CreateSaleOutCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = command.Request;
            if (request is null || request.Items is null || request.Items.Count == 0)
            {
                throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.", nameof(command));
            }

            var purchaseOrder = CustomerPurchaseOrder.Create(
                request.CustomerPoNo,
                request.CustomerName,
                request.OrderDate);

            SaleOutOrderAggregate.EnsureDistinctProducts(request.Items.Select(item => item.ProductId));

            var saleOuts = new List<SaleOut>(request.Items.Count);
            foreach (var item in request.Items)
            {
                var productId = SaleOutOrderAggregate.ValidateRequiredProductId(item.ProductId);
                var product = await _productSnapshotRepository.FindActiveByIdAsync(productId)
                    ?? throw new KeyNotFoundException("Không tìm thấy sản phẩm đang hoạt động.")
                        .WithTitle("Không tìm thấy sản phẩm");

                saleOuts.Add(SaleOut.CreateLine(
                    purchaseOrder,
                    product,
                    item.Quantity,
                    item.QuantityPerBox,
                    item.Price,
                    Guid.NewGuid()));
            }

            var orderAggregate = SaleOutOrderAggregate.CreateNew(purchaseOrder, saleOuts);
            var saleOutNo = await _saleOutRepository.CreateAsync(orderAggregate.Lines);
            if (saleOutNo is null)
            {
                throw new InvalidOperationException(
                    $"Số PO khách hàng '{purchaseOrder.CustomerPoNo}' đã tồn tại. Hãy mở đơn đó để chỉnh sửa danh sách sản phẩm.")
                    .WithTitle("Đơn xuất hàng đã tồn tại");
            }

            orderAggregate.ClearDomainEvents();

            return new SaleOutCreateResponse
            {
                SaleOutNo = saleOutNo,
                CustomerPoNo = purchaseOrder.CustomerPoNo,
                OrderDate = purchaseOrder.OrderDate,
                CustomerName = purchaseOrder.CustomerName,
                ItemCount = orderAggregate.Lines.Count
            };
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle("Thông tin đơn xuất hàng chưa hợp lệ");
        }
    }
}

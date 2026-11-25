using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Domain.Aggregates;
using BE.Services.OrderingService.Domain.ProductReplica;
using BE.Services.OrderingService.Domain.Repositories;
using BE.Services.OrderingService.Domain.ValueObjects;
using Microsoft.Data.SqlClient;

namespace BE.Services.OrderingService.Application.Commands.UpdateSaleOutOrder;

public sealed class UpdateSaleOutOrderCommandHandler : ICommandHandler<UpdateSaleOutOrderCommand, Unit>
{
    private readonly ISaleOutRepository _saleOutRepository;
    private readonly IProductSnapshotRepository _productSnapshotRepository;

    public UpdateSaleOutOrderCommandHandler(
        ISaleOutRepository saleOutRepository,
        IProductSnapshotRepository productSnapshotRepository)
    {
        _saleOutRepository = saleOutRepository;
        _productSnapshotRepository = productSnapshotRepository;
    }

    public async Task<Unit> HandleAsync(
        UpdateSaleOutOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var normalizedSaleOutNo = CustomerPurchaseOrder.NormalizeRequiredText(
                command.SaleOutNo,
                CustomerPurchaseOrder.MAX_PO_NO_LENGTH,
                "Mã số phiếu");
            var request = command.Request;
            if (request?.Items is null || request.Items.Count == 0)
            {
                throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.");
            }

            var existingItems = await _saleOutRepository.GetOrderAsync(normalizedSaleOutNo);
            if (existingItems.Count == 0)
            {
                throw new KeyNotFoundException(string.Empty)
                    .WithTitle("Không tìm thấy đơn xuất hàng");
            }

            var existingById = existingItems.ToDictionary(item => item.Id);
            var requestedIds = request.Items
                .Where(item => item.Id.HasValue)
                .Select(item => item.Id!.Value)
                .ToArray();
            if (requestedIds.Distinct().Count() != requestedIds.Length
                || requestedIds.Any(id => !existingById.ContainsKey(id)))
            {
                throw new ArgumentException("Danh sách sản phẩm chứa dòng không thuộc đơn hàng này.");
            }

            SaleOutOrderAggregate.EnsureDistinctProducts(request.Items.Select(item => item.ProductId));
            var firstItem = existingItems[0];
            var orderDate = firstItem.OrderDate is { } date
                ? DateOnly.FromDateTime(date)
                : DateOnly.FromDateTime(DateTime.Today);
            var purchaseOrder = CustomerPurchaseOrder.Create(
                firstItem.CustomerPoNo,
                firstItem.CustomerName,
                orderDate);

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
                    item.Id ?? Guid.Empty));
            }

            var updated = await _saleOutRepository.UpdateOrderAsync(normalizedSaleOutNo, saleOuts);
            if (!updated)
            {
                throw new KeyNotFoundException(string.Empty)
                    .WithTitle("Không tìm thấy đơn xuất hàng");
            }

            return Unit.VALUE;
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle("Danh sách sản phẩm chưa hợp lệ");
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            throw new InvalidOperationException(
                "Sản phẩm đã tồn tại trong một đơn hàng khác hoặc đã được lưu trước đó.",
                exception)
                .WithTitle("Sản phẩm đã tồn tại");
        }
    }
}

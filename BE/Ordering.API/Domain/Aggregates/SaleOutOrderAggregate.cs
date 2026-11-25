using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Domain.Events;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Domain.Aggregates;

public sealed class SaleOutOrderAggregate : AggregateRoot<string>
{
    private readonly List<SaleOut> _lines;

    public CustomerPurchaseOrder PurchaseOrder { get; }

    public IReadOnlyList<SaleOut> Lines => _lines.AsReadOnly();

    private SaleOutOrderAggregate(
        string id,
        CustomerPurchaseOrder purchaseOrder,
        List<SaleOut> lines)
    {
        Id = id;
        PurchaseOrder = purchaseOrder;
        _lines = lines;
    }

    public static SaleOutOrderAggregate CreateNew(
        CustomerPurchaseOrder purchaseOrder,
        IReadOnlyList<SaleOut> lines)
    {
        if (lines.Count == 0)
        {
            throw new ArgumentException("Đơn hàng phải có ít nhất một sản phẩm.");
        }

        var aggregate = new SaleOutOrderAggregate(purchaseOrder.CustomerPoNo, purchaseOrder, lines.ToList());
        aggregate.AddDomainEvent(new SaleOutOrderCreatedDomainEvent(
            purchaseOrder.CustomerPoNo,
            purchaseOrder.CustomerName,
            purchaseOrder.OrderDate,
            lines.Count));
        return aggregate;
    }

    public static void EnsureDistinctProducts(IEnumerable<Guid?> productIdValues)
    {
        var productIds = productIdValues.Select(id => id ?? Guid.Empty).ToArray();
        if (productIds.Any(id => id == Guid.Empty) || productIds.Distinct().Count() != productIds.Length)
        {
            throw new ArgumentException("Mỗi sản phẩm chỉ được xuất hiện một lần trong cùng đơn hàng.");
        }
    }

    public static Guid ValidateRequiredProductId(Guid? productIdValue)
    {
        var productId = productIdValue ?? throw new ArgumentException("Vui lòng chọn sản phẩm.");
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Vui lòng chọn sản phẩm hợp lệ.");
        }

        return productId;
    }
}

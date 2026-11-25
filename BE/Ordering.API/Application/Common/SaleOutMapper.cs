using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Domain.Aggregates;

namespace BE.Services.OrderingService.Application.Common;

public static class SaleOutMapper
{
    public static SaleOutListItemResponse ToListItemResponse(SaleOut saleOut)
    {
        return new SaleOutListItemResponse
        {
            Id = saleOut.Id,
            SaleOutNo = saleOut.SaleOutNo,
            CustomerPoNo = saleOut.CustomerPoNo,
            OrderDate = saleOut.OrderDate is { } orderDate ? DateOnly.FromDateTime(orderDate) : null,
            CustomerName = saleOut.CustomerName,
            ProductId = saleOut.ProductId,
            ProductCode = saleOut.ProductCode,
            ProductName = saleOut.ProductName,
            Unit = saleOut.Unit,
            Quantity = saleOut.Quantity,
            Price = saleOut.Price,
            Amount = saleOut.Amount,
            QuantityPerBox = saleOut.QuantityPerBox,
            BoxQuantity = saleOut.BoxQuantity,
            IsActive = saleOut.IsActive
        };
    }
}

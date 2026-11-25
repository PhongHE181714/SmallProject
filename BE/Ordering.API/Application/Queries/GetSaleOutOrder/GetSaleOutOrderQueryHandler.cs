using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Application.Common;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Domain.Repositories;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Application.Queries.GetSaleOutOrder;

public sealed class GetSaleOutOrderQueryHandler : IQueryHandler<GetSaleOutOrderQuery, IReadOnlyList<SaleOutListItemResponse>>
{
    private readonly ISaleOutRepository _saleOutRepository;

    public GetSaleOutOrderQueryHandler(ISaleOutRepository saleOutRepository)
    {
        _saleOutRepository = saleOutRepository;
    }

    public async Task<IReadOnlyList<SaleOutListItemResponse>> HandleAsync(
        GetSaleOutOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var normalizedSaleOutNo = CustomerPurchaseOrder.NormalizeRequiredText(
            query.SaleOutNo,
            CustomerPurchaseOrder.MAX_PO_NO_LENGTH,
            "Mã số phiếu");
        var saleOuts = await _saleOutRepository.GetOrderAsync(normalizedSaleOutNo);
        if (saleOuts.Count == 0)
        {
            throw new KeyNotFoundException(string.Empty)
                .WithTitle("Không tìm thấy đơn xuất hàng");
        }

        return saleOuts.Select(SaleOutMapper.ToListItemResponse).ToArray();
    }
}

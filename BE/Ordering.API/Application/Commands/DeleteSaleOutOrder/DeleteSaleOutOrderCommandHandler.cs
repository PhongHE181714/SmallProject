using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Domain.Repositories;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Application.Commands.DeleteSaleOutOrder;

public sealed class DeleteSaleOutOrderCommandHandler : ICommandHandler<DeleteSaleOutOrderCommand, Unit>
{
    private readonly ISaleOutRepository _saleOutRepository;

    public DeleteSaleOutOrderCommandHandler(ISaleOutRepository saleOutRepository)
    {
        _saleOutRepository = saleOutRepository;
    }

    public async Task<Unit> HandleAsync(
        DeleteSaleOutOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        var normalizedSaleOutNo = CustomerPurchaseOrder.NormalizeRequiredText(
            command.SaleOutNo,
            CustomerPurchaseOrder.MAX_PO_NO_LENGTH,
            "Mã số phiếu");
        var deleted = await _saleOutRepository.SoftDeleteOrderAsync(normalizedSaleOutNo);
        if (!deleted)
        {
            throw new KeyNotFoundException(string.Empty)
                .WithTitle("Không tìm thấy đơn xuất hàng đang hoạt động");
        }

        return Unit.VALUE;
    }
}

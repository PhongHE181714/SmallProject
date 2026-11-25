using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Domain.Aggregates;
using BE.Services.OrderingService.Domain.Repositories;

namespace BE.Services.OrderingService.Application.Commands.UpdateSaleOutLine;

public sealed class UpdateSaleOutLineCommandHandler : ICommandHandler<UpdateSaleOutLineCommand, Unit>
{
    private readonly ISaleOutRepository _saleOutRepository;

    public UpdateSaleOutLineCommandHandler(ISaleOutRepository saleOutRepository)
    {
        _saleOutRepository = saleOutRepository;
    }

    public async Task<Unit> HandleAsync(
        UpdateSaleOutLineCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (command.Id == Guid.Empty || command.Request is null)
            {
                throw new ArgumentException("Thông tin đơn xuất hàng chưa hợp lệ.");
            }

            var (quantity, quantityPerBox, price, amount, boxQuantity) = SaleOut.ValidateLineUpdate(
                command.Request.Quantity,
                command.Request.QuantityPerBox,
                command.Request.Price);

            var updated = await _saleOutRepository.UpdateAsync(
                command.Id,
                quantity,
                quantityPerBox,
                price,
                amount,
                boxQuantity);

            if (!updated)
            {
                throw new KeyNotFoundException(string.Empty)
                    .WithTitle("Không tìm thấy đơn xuất hàng");
            }

            return Unit.VALUE;
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle("Thông tin đơn xuất hàng chưa hợp lệ");
        }
    }
}

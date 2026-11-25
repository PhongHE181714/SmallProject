using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Domain.Repositories;

namespace BE.Services.OrderingService.Application.Commands.DeleteSaleOutLine;

public sealed class DeleteSaleOutLineCommandHandler : ICommandHandler<DeleteSaleOutLineCommand, Unit>
{
    private readonly ISaleOutRepository _saleOutRepository;

    public DeleteSaleOutLineCommandHandler(ISaleOutRepository saleOutRepository)
    {
        _saleOutRepository = saleOutRepository;
    }

    public async Task<Unit> HandleAsync(
        DeleteSaleOutLineCommand command,
        CancellationToken cancellationToken = default)
    {
        if (command.Id == Guid.Empty)
        {
            throw new ArgumentException("Mã đơn xuất hàng không hợp lệ.", nameof(command))
                .WithTitle("Mã đơn xuất hàng chưa hợp lệ");
        }

        var deleted = await _saleOutRepository.SoftDeleteAsync(command.Id);
        if (!deleted)
        {
            throw new KeyNotFoundException(string.Empty)
                .WithTitle("Không tìm thấy đơn xuất hàng đang hoạt động");
        }

        return Unit.VALUE;
    }
}

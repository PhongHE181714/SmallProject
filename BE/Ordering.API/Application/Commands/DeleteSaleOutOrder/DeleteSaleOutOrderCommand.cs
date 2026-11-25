using BE.BuildingBlocks.CQRS;

namespace BE.Services.OrderingService.Application.Commands.DeleteSaleOutOrder;

public sealed record DeleteSaleOutOrderCommand(string SaleOutNo) : ICommand<Unit>;

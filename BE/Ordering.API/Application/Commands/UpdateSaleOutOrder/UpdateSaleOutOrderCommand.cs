using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Commands.UpdateSaleOutOrder;

public sealed record UpdateSaleOutOrderCommand(
    string SaleOutNo,
    UpdateSaleOutOrderRequest? Request) : ICommand<Unit>;

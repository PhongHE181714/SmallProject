using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Commands.CreateSaleOut;

public sealed record CreateSaleOutCommand(CreateSaleOutRequest? Request) : ICommand<SaleOutCreateResponse>;

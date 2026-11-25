using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Commands.UpdateSaleOutLine;

public sealed record UpdateSaleOutLineCommand(Guid Id, UpdateSaleOutRequest? Request) : ICommand<Unit>;

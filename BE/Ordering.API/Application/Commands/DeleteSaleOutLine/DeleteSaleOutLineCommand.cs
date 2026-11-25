using BE.BuildingBlocks.CQRS;

namespace BE.Services.OrderingService.Application.Commands.DeleteSaleOutLine;

public sealed record DeleteSaleOutLineCommand(Guid Id) : ICommand<Unit>;

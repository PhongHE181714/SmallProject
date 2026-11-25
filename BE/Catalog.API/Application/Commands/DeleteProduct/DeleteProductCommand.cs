using BE.BuildingBlocks.CQRS;

namespace BE.Services.CatalogService.Application.Commands.DeleteProduct;

public sealed record DeleteProductCommand(Guid Id) : ICommand<Unit>;

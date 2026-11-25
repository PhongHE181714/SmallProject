using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.DTOs;

namespace BE.Services.CatalogService.Application.Commands.UpdateProduct;

public sealed record UpdateProductCommand(Guid Id, UpdateProductRequest? Request) : ICommand<ProductCreateResponse>;

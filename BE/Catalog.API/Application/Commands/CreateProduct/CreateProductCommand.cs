using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.DTOs;

namespace BE.Services.CatalogService.Application.Commands.CreateProduct;

public sealed record CreateProductCommand(CreateProductRequest? Request) : ICommand<ProductCreateResponse>;

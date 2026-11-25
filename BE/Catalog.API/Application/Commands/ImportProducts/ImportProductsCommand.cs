using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace BE.Services.CatalogService.Application.Commands.ImportProducts;

public sealed record ImportProductsCommand(IFormFile? File) : ICommand<ProductImportResponse>;

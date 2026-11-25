using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;
using Microsoft.AspNetCore.Http;

namespace BE.Services.OrderingService.Application.Commands.ImportSaleOuts;

public sealed record ImportSaleOutsCommand(IFormFile? File) : ICommand<SaleOutImportResponse>;

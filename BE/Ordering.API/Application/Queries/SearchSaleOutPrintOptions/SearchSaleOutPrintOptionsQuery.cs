using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Queries.SearchSaleOutPrintOptions;

public sealed record SearchSaleOutPrintOptionsQuery(
    SaleOutPrintOptionsRequest Request) : IQuery<SaleOutPrintOptionsResponse>;

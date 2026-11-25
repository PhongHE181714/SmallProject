using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Queries.SearchSaleOuts;

public sealed record SearchSaleOutsQuery(SaleOutSearchRequest Request) : IQuery<SaleOutSearchResponse>;

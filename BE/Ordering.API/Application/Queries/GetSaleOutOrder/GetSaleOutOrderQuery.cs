using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Queries.GetSaleOutOrder;

public sealed record GetSaleOutOrderQuery(string SaleOutNo) : IQuery<IReadOnlyList<SaleOutListItemResponse>>;

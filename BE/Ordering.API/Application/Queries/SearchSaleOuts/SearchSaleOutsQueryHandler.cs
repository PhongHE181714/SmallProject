using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Application.Common;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Domain.Repositories;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Application.Queries.SearchSaleOuts;

public sealed class SearchSaleOutsQueryHandler : IQueryHandler<SearchSaleOutsQuery, SaleOutSearchResponse>
{
    private readonly ISaleOutRepository _saleOutRepository;

    public SearchSaleOutsQueryHandler(ISaleOutRepository saleOutRepository)
    {
        _saleOutRepository = saleOutRepository;
    }

    public async Task<SaleOutSearchResponse> HandleAsync(
        SearchSaleOutsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = query.Request;
            var criteria = SaleOutSearchCriteria.Create(
                request.SearchField,
                request.SearchValue,
                request.Page,
                request.PageSize);

            var result = await _saleOutRepository.SearchAsync(criteria);
            var totalPages = result.TotalItems == 0
                ? 0
                : (result.TotalItems + request.PageSize - 1) / request.PageSize;

            return new SaleOutSearchResponse
            {
                Items = result.Items.Select(order => new SaleOutOrderListItemResponse
                {
                    SaleOutNo = order.SaleOutNo,
                    CustomerPoNo = order.CustomerPoNo,
                    OrderDate = order.OrderDate is { } orderDate
                        ? DateOnly.FromDateTime(orderDate)
                        : null,
                    CustomerName = order.CustomerName,
                    ProductCount = order.ProductCount,
                    TotalAmount = order.TotalAmount,
                    Products = order.Products.Select(SaleOutMapper.ToListItemResponse).ToArray()
                }).ToArray(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = totalPages
            };
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle("Thông tin tìm kiếm Sale Out không hợp lệ");
        }
    }
}

using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Domain.Repositories;

namespace BE.Services.OrderingService.Application.Queries.SearchSaleOutPrintOptions;

public sealed class SearchSaleOutPrintOptionsQueryHandler
    : IQueryHandler<SearchSaleOutPrintOptionsQuery, SaleOutPrintOptionsResponse>
{
    private readonly ISaleOutRepository _saleOutRepository;

    public SearchSaleOutPrintOptionsQueryHandler(ISaleOutRepository saleOutRepository)
    {
        _saleOutRepository = saleOutRepository;
    }

    public async Task<SaleOutPrintOptionsResponse> HandleAsync(
        SearchSaleOutPrintOptionsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = query.Request;
            if (request.Page < 1)
            {
                throw new ArgumentException("Số trang phải lớn hơn hoặc bằng 1.");
            }

            if (request.PageSize is < 1 or > SaleOutPrintOptionsLimits.MAX_PAGE_SIZE)
            {
                throw new ArgumentException($"Số đơn mỗi trang phải từ 1 đến {SaleOutPrintOptionsLimits.MAX_PAGE_SIZE}.");
            }

            var searchText = request.SearchValue?.Trim();
            if (searchText?.Length > SaleOutPrintOptionsLimits.MAX_SALE_OUT_NO_SEARCH_LENGTH)
            {
                throw new ArgumentException(
                    $"Mã số phiếu tìm kiếm không được vượt quá {SaleOutPrintOptionsLimits.MAX_SALE_OUT_NO_SEARCH_LENGTH} ký tự.");
            }

            if (string.IsNullOrWhiteSpace(searchText))
            {
                searchText = null;
            }

            var result = await _saleOutRepository.SearchPrintOptionsAsync(
                searchText,
                (long)(request.Page - 1) * request.PageSize,
                request.PageSize);

            return new SaleOutPrintOptionsResponse
            {
                Items = result.Items.Select(order => new SaleOutPrintOptionResponse
                {
                    SaleOutNo = order.SaleOutNo,
                    CustomerPoNo = order.CustomerPoNo,
                    OrderDate = order.OrderDate is { } orderDate ? DateOnly.FromDateTime(orderDate) : null,
                    CustomerName = order.CustomerName,
                    ProductCount = order.ProductCount,
                    TotalAmount = order.TotalAmount
                }).ToArray(),
                Page = request.Page,
                PageSize = request.PageSize,
                TotalItems = result.TotalItems,
                TotalPages = result.TotalItems == 0
                    ? 0
                    : (result.TotalItems + request.PageSize - 1) / request.PageSize
            };
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle("Thông tin tìm phiếu in không hợp lệ");
        }
    }
}

using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.CatalogService.Application.DTOs;
using BE.Services.CatalogService.Domain.Repositories;
using BE.Services.CatalogService.Domain.ValueObjects;

namespace BE.Services.CatalogService.Application.Queries.SearchProducts;

public sealed class SearchProductsQueryHandler : IQueryHandler<SearchProductsQuery, ProductSearchResponse>
{
    private readonly IProductRepository _productRepository;

    public SearchProductsQueryHandler(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public async Task<ProductSearchResponse> HandleAsync(
        SearchProductsQuery query,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = query.Request;
            var criteria = ProductSearchCriteria.Create(
                request.SearchField,
                request.SearchValue,
                request.Page,
                request.PageSize);

            var result = await _productRepository.SearchAsync(criteria);
            var totalPages = result.TotalItems == 0
                ? 0
                : (result.TotalItems + request.PageSize - 1) / request.PageSize;

            return new ProductSearchResponse
            {
                Items = result.Items.Select(product => new ProductListItemResponse
                {
                    Id = product.Id,
                    ProductCode = product.ProductCode,
                    ProductName = product.ProductName,
                    Unit = product.Unit,
                    Specification = product.Specification,
                    QuantityPerBox = product.QuantityPerBox,
                    ProductWeight = product.ProductWeight,
                    IsActive = product.IsActive
                }).ToArray(),
                Page = request.Page != 0 ? request.Page : 1,
                PageSize = request.PageSize != 0 ? request.PageSize : 5,
                TotalItems = result.TotalItems,
                TotalPages = totalPages
            };
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle("Thông tin tìm kiếm không hợp lệ");
        }
    }
}

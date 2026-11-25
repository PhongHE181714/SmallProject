using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.DTOs;

namespace BE.Services.CatalogService.Application.Queries.SearchProducts;

public sealed record SearchProductsQuery(ProductSearchRequest Request) : IQuery<ProductSearchResponse>;

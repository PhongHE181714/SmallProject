using BE.Data.Interfaces;
using BE.Services.CatalogService.Domain.Repositories;

namespace BE.Services.CatalogService.Repositories;

public sealed partial class ProductRepository : IProductRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public ProductRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }
}

using BE.Data.Interfaces;
using BE.Repositories.Interfaces;

namespace BE.Repositories.Products;

public sealed partial class ProductRepository : IProductRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public ProductRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }
}

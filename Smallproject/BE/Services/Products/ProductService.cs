using BE.Repositories.Interfaces;
using BE.Services.Interfaces;

namespace BE.Services.Products;

public sealed partial class ProductService : IProductService
{
    private readonly IProductRepository _productRepository;

    public ProductService(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }
}

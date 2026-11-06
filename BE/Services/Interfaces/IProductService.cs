using BE.DTOs.Products;

namespace BE.Services.Interfaces;

public interface IProductService
{
    Task<ProductSearchResponse> SearchAsync(ProductSearchRequest request);
    Task<ProductCreateResponse> CreateAsync(CreateProductRequest request);
    Task<ProductCreateResponse> UpdateAsync(Guid id, UpdateProductRequest request);
    Task DeleteAsync(Guid id);
    byte[] GenerateProductTemplate();
    Task<ProductImportResponse> ImportProductsAsync(Stream fileStream, string fileExtension);
}

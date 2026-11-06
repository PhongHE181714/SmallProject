using BE.DTOs.Products;
using BE.Models.MasterProduct;
using BE.Repositories.Interfaces;

namespace BE.Services.Products;

public sealed partial class ProductService
{
    public async Task<ProductCreateResponse> CreateAsync(CreateProductRequest request)
    {
        if (request is null)
        {
            throw new ArgumentException("Thông tin sản phẩm không được để trống.", nameof(request));
        }

        var product = new MasterProduct
        {
            Id = Guid.NewGuid(),
            ProductCode = ProductValidation.NormalizeRequiredText(request.ProductCode, ProductValidation.TEXT_LENGTH_50, "Mã sản phẩm"),
            ProductName = ProductValidation.NormalizeRequiredText(request.ProductName, ProductValidation.TEXT_LENGTH_200, "Tên sản phẩm"),
            Unit = ProductValidation.NormalizeRequiredText(request.Unit, ProductValidation.TEXT_LENGTH_50, "Đơn vị"),
            Specification = ProductValidation.NormalizeRequiredText(request.Specification, ProductValidation.TEXT_LENGTH_200, "Quy cách"),
            QuantityPerBox = ProductValidation.ValidateRequiredQuantityPerBox(request.QuantityPerBox),
            ProductWeight = ProductValidation.ValidateRequiredDecimal(request.ProductWeight, "Trọng lượng"),
            IsActive = true
        };

        var duplicateFields = await _productRepository.FindDuplicateFieldsAsync(
            product.ProductCode,
            product.ProductName);

        if (duplicateFields.Count > 0)
        {
            throw CreateDuplicateProductException(duplicateFields, product.ProductCode, product.ProductName);
        }

        await _productRepository.CreateAsync(product);

        return new ProductCreateResponse
        {
            Id = product.Id,
            ProductCode = product.ProductCode,
            ProductName = product.ProductName,
            Unit = product.Unit,
            Specification = product.Specification,
            QuantityPerBox = product.QuantityPerBox,
            ProductWeight = product.ProductWeight,
            IsActive = product.IsActive
        };
    }

    public async Task<ProductCreateResponse> UpdateAsync(Guid id, UpdateProductRequest request)
{
    if (request is null)
    {
        throw new ArgumentException("Thông tin sản phẩm không được để trống.", nameof(request));
    }

    var product = new MasterProduct
    {
        Id = id,
        ProductCode = ProductValidation.NormalizeRequiredText(request.ProductCode, ProductValidation.TEXT_LENGTH_50, "Mã sản phẩm"),
        ProductName = ProductValidation.NormalizeRequiredText(request.ProductName, ProductValidation.TEXT_LENGTH_200, "Tên sản phẩm"),
        Unit = ProductValidation.NormalizeRequiredText(request.Unit, ProductValidation.TEXT_LENGTH_50, "Đơn vị"),
        Specification = ProductValidation.NormalizeRequiredText(request.Specification, ProductValidation.TEXT_LENGTH_200, "Quy cách"),
        QuantityPerBox = ProductValidation.ValidateRequiredQuantityPerBox(request.QuantityPerBox),
        ProductWeight = ProductValidation.ValidateRequiredDecimal(request.ProductWeight, "Trọng lượng"),
        IsActive = request.IsActive,
    };

    var duplicateFields = await _productRepository.FindDuplicateFieldsForUpdateAsync(
        product.Id,
        product.ProductCode,
        product.ProductName);

    if (duplicateFields.Count > 0)
    {
        throw CreateDuplicateProductException(duplicateFields, product.ProductCode, product.ProductName);
    }

    await _productRepository.UpdateAsync(product);

    return new ProductCreateResponse
    {
        Id = product.Id,
        ProductCode = product.ProductCode,
        ProductName = product.ProductName,
        Unit = product.Unit,
        Specification = product.Specification,
        QuantityPerBox = product.QuantityPerBox,
        ProductWeight = product.ProductWeight,
        IsActive = product.IsActive
    };
}

    public async Task DeleteAsync(Guid id)
    {
        var exists = await _productRepository.ExistsAsync(id);
        if (!exists)
        {
            throw new KeyNotFoundException($"Không tìm thấy sản phẩm với Id: {id}");
        }

        var hasRelatedTransactions = await _productRepository.HasRelatedTransactionsAsync(id);
        if (hasRelatedTransactions)
        {
            throw new InvalidOperationException(
                "Sản phẩm đã phát sinh giao dịch xuất bán (SaleOut), không thể xóa.");
        }

        await _productRepository.DeleteAsync(id);
    }

    private static InvalidOperationException CreateDuplicateProductException(
        IReadOnlyList<string> duplicateFields,
        string? productCode,
        string? productName)
    {
        var messages = duplicateFields.Select(field => field switch
        {
            "ProductCode" => $"Mã sản phẩm {productCode} đã tồn tại, vui lòng nhập lại.",
            "ProductName" => $"Tên sản phẩm {productName} đã tồn tại, vui lòng nhập lại.",
            _ => "Thông tin sản phẩm đã tồn tại."
        });

        return new InvalidOperationException(string.Join(" ", messages));
    }
}

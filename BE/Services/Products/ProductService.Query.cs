using System.Globalization;
using BE.DTOs.Products;
using BE.Models.MasterProduct;
using BE.Repositories.Interfaces;

namespace BE.Services.Products;

public sealed partial class ProductService
{
    public async Task<ProductSearchResponse> SearchAsync(ProductSearchRequest request)
    {
        var criteria = CreateSearchCriteria(request);
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
            PageSize = request.PageSize != 0 ? request.PageSize: 5,
            TotalItems = result.TotalItems,
            TotalPages = totalPages
        };
    }

    private static ProductSearchCriteria CreateSearchCriteria(ProductSearchRequest request)
    {
        var searchValue = request.SearchValue?.Trim();
        ProductSearchField? searchField = null;
        decimal? searchNumber = null;

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            if (string.IsNullOrWhiteSpace(request.SearchField))
            {
                throw new ArgumentException(
                    "Cần chọn searchField khi có searchValue.");
            }

            searchField = ParseSearchField(request.SearchField);

            if (searchField == ProductSearchField.QuantityPerBox)
            {
                if (!int.TryParse(
                    searchValue,
                    NumberStyles.None,
                    CultureInfo.InvariantCulture,
                    out var parsedQuantity) || parsedQuantity < 0)
                {
                    throw new ArgumentException(
                        "Số lượng/thùng phải là số nguyên lớn hơn 0, ví dụ 24.");
                }

                searchNumber = parsedQuantity;
                searchValue = null;
            }
            else if (searchField == ProductSearchField.ProductWeight)
            {
                if (searchValue!.Contains(',') && searchValue.Contains('.'))
                {
                    throw new ArgumentException(
                        $"Giá trị tìm kiếm của {searchField} chỉ được dùng một loại dấu thập phân.");
                }

                var normalizedSearchValue = searchValue!.Replace(',', '.');
                if (!decimal.TryParse(
                    normalizedSearchValue,
                    NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                    CultureInfo.InvariantCulture,
                    out var parsedNumber))
                {
                    throw new ArgumentException(
                        $"Giá trị tìm kiếm của {searchField} phải là số hợp lệ.");
                }

                searchNumber = parsedNumber;
                searchValue = null;
            }
        }
        else
        {
            searchValue = null;
        }

        return new ProductSearchCriteria
        {
            SearchField = searchField,
            SearchText = searchValue,
            SearchNumber = searchNumber,
            Offset = (long)(request.Page - 1) * request.PageSize,
            PageSize = request.PageSize
        };
    }

    private static ProductSearchField ParseSearchField(string searchField)
    {
        return searchField.Trim().ToUpperInvariant() switch
        {
            "PRODUCTCODE" => ProductSearchField.ProductCode,
            "PRODUCTNAME" => ProductSearchField.ProductName,
            "UNIT" => ProductSearchField.Unit,
            "SPECIFICATION" => ProductSearchField.Specification,
            "QUANTITYPERBOX" => ProductSearchField.QuantityPerBox,
            "PRODUCTWEIGHT" => ProductSearchField.ProductWeight,
            _ => throw new ArgumentException(
                "searchField không hợp lệ. Hãy chọn một trường sản phẩm được hỗ trợ.")
        };
    }
}

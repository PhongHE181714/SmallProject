using System.Globalization;
using BE.BuildingBlocks.SharedKernel;

namespace BE.Services.CatalogService.Domain.ValueObjects;

public enum ProductSearchField
{
    ProductCode,
    ProductName,
    Unit,
    Specification,
    QuantityPerBox,
    ProductWeight
}

public sealed class ProductSearchCriteria : ValueObject
{
    public ProductSearchField? SearchField { get; init; }

    public string? SearchText { get; init; }

    public decimal? SearchNumber { get; init; }

    public long Offset { get; init; }

    public int PageSize { get; init; }

    public static ProductSearchCriteria Create(
        string? searchFieldText,
        string? rawSearchValue,
        int page,
        int pageSize)
    {
        var searchValue = rawSearchValue?.Trim();
        ProductSearchField? searchField = null;
        decimal? searchNumber = null;

        if (!string.IsNullOrWhiteSpace(searchValue))
        {
            if (string.IsNullOrWhiteSpace(searchFieldText))
            {
                throw new ArgumentException("Cần chọn searchField khi có searchValue.");
            }

            searchField = ParseSearchField(searchFieldText);

            if (searchField == ProductSearchField.QuantityPerBox)
            {
                if (!int.TryParse(
                        searchValue,
                        NumberStyles.None,
                        CultureInfo.InvariantCulture,
                        out var parsedQuantity) || parsedQuantity < 0)
                {
                    throw new ArgumentException("Số lượng/thùng phải là số nguyên lớn hơn 0, ví dụ 24.");
                }

                searchNumber = parsedQuantity;
                searchValue = null;
            }
            else if (searchField == ProductSearchField.ProductWeight)
            {
                if (searchValue.Contains(',') && searchValue.Contains('.'))
                {
                    throw new ArgumentException(
                        $"Giá trị tìm kiếm của {searchField} chỉ được dùng một loại dấu thập phân.");
                }

                var normalizedSearchValue = searchValue.Replace(',', '.');
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
            Offset = (long)(page - 1) * pageSize,
            PageSize = pageSize
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

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SearchField;
        yield return SearchText;
        yield return SearchNumber;
        yield return Offset;
        yield return PageSize;
    }
}

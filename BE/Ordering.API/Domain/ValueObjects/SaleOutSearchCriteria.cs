using BE.BuildingBlocks.SharedKernel;

namespace BE.Services.OrderingService.Domain.ValueObjects;

public enum SaleOutSearchField
{
    CustomerPoNo,
    SaleOutNo,
    OrderDate,
    CustomerName,
    ProductCode,
    ProductName
}

public sealed class SaleOutSearchCriteria : ValueObject
{
    public SaleOutSearchField? SearchField { get; init; }

    public string? SearchText { get; init; }

    public long Offset { get; init; }

    public int PageSize { get; init; }

    public static SaleOutSearchCriteria Create(
        string? searchFieldText,
        string? rawSearchValue,
        int page,
        int pageSize)
    {
        var searchText = rawSearchValue?.Trim();
        SaleOutSearchField? searchField = null;

        if (!string.IsNullOrWhiteSpace(searchText))
        {
            if (string.IsNullOrWhiteSpace(searchFieldText))
            {
                throw new ArgumentException("Cần chọn searchField khi có searchValue.");
            }

            searchField = ParseSearchField(searchFieldText);
        }
        else
        {
            searchText = null;
        }

        return new SaleOutSearchCriteria
        {
            SearchField = searchField,
            SearchText = searchText,
            Offset = (long)(page - 1) * pageSize,
            PageSize = pageSize
        };
    }

    private static SaleOutSearchField ParseSearchField(string searchField)
    {
        return searchField.Trim().ToUpperInvariant() switch
        {
            "CUSTOMERPONO" => SaleOutSearchField.CustomerPoNo,
            "SALEOUTNO" => SaleOutSearchField.SaleOutNo,
            "ORDERDATE" => SaleOutSearchField.OrderDate,
            "CUSTOMERNAME" => SaleOutSearchField.CustomerName,
            "PRODUCTCODE" => SaleOutSearchField.ProductCode,
            "PRODUCTNAME" => SaleOutSearchField.ProductName,
            _ => throw new ArgumentException(
                "searchField không hợp lệ. Hãy chọn một trường tìm kiếm Sale Out được hỗ trợ.")
        };
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return SearchField;
        yield return SearchText;
        yield return Offset;
        yield return PageSize;
    }
}

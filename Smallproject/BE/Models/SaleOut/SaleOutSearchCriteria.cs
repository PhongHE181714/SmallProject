namespace BE.Models.SaleOut;

public enum SaleOutSearchField
{
    CustomerPoNo,
    SaleOutNo,
    OrderDate,
    CustomerName,
    ProductCode,
    ProductName
}

public sealed class SaleOutSearchCriteria
{
    public SaleOutSearchField? SearchField { get; init; }

    public string? SearchText {get; init;}

    public long Offset {get; init;}

    public int PageSize {get; init;}
}

namespace BE.DTOs.SaleOuts;

public sealed class SaleOutPrintOptionsResponse
{
    public required IReadOnlyList<SaleOutPrintOptionResponse> Items { get; init; }

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalItems { get; init; }

    public long TotalPages { get; init; }
}

public sealed class SaleOutPrintOptionResponse
{
    public string? SaleOutNo { get; init; }

    public string? CustomerPoNo { get; init; }

    public DateOnly? OrderDate { get; init; }

    public string? CustomerName { get; init; }

    public long ProductCount { get; init; }

    public decimal TotalAmount { get; init; }
}

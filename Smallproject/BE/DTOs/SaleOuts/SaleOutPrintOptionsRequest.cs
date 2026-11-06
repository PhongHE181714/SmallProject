using System.ComponentModel.DataAnnotations;

namespace BE.DTOs.SaleOuts;

public sealed class SaleOutPrintOptionsRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, SaleOutPrintOptionsLimits.MAX_PAGE_SIZE)]
    public int PageSize { get; init; } = 20;

    public string? SearchValue { get; init; }
}

public static class SaleOutPrintOptionsLimits
{
    public const int MAX_PAGE_SIZE = 100;
    public const int MAX_SALE_OUT_NO_SEARCH_LENGTH = 50;
}

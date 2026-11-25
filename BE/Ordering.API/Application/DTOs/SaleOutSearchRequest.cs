using System.ComponentModel.DataAnnotations;

namespace BE.Services.OrderingService.Application.DTOs;

public static class SaleOutSearchLimits
{
    public const int MAX_PAGE_SIZE = 200;
}

public sealed class SaleOutSearchRequest
{
    [Range(1, int.MaxValue)]
    public int Page { get; init; } = 1;

    [Range(1, SaleOutSearchLimits.MAX_PAGE_SIZE)]
    public int PageSize { get; init; } = 5;

    public string? SearchField { get; init; }

    public string? SearchValue { get; init; }
}

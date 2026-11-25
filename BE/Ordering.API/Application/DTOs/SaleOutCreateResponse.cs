namespace BE.Services.OrderingService.Application.DTOs;

public sealed class SaleOutCreateResponse
{
    public string? SaleOutNo { get; init; }

    public string? CustomerPoNo { get; init; }

    public DateOnly? OrderDate { get; init; }

    public string? CustomerName { get; init; }

    public int ItemCount { get; init; }
}

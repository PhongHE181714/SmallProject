namespace BE.Services.CatalogService.Application.DTOs;

public sealed class ProductImportErrorResponse
{
    public int? RowNumber { get; set; }

    public string Field { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;
}

namespace BE.DTOs.SaleOuts;

public sealed class SaleOutImportErrorResponse
{
    public int? RowNumber { get; init; }

    public string Field { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;
}

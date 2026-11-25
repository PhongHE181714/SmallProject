using BE.Services.OrderingService.Domain.Aggregates;
using BE.Services.OrderingService.Domain.ProductReplica;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Application.Services;

public sealed record ParsedSaleOutImportRow(
    int RowNumber,
    string CustomerPoNo,
    DateOnly OrderDate,
    string CustomerName,
    string ProductCode,
    decimal Price,
    decimal Quantity,
    decimal QuantityPerBox);

public interface ISaleOutDocumentService
{
    IReadOnlyList<ParsedSaleOutImportRow> ParseImportFile(Stream fileStream, string fileExtension);
    IReadOnlyList<SaleOut> BuildSaleOutEntitiesFromImport(
        IReadOnlyList<ParsedSaleOutImportRow> parsedRows,
        IReadOnlyDictionary<string, ProductSnapshot> productsByCode);
    InvalidDataException CreateImportDatabaseConflictException(
        IReadOnlyList<ParsedSaleOutImportRow> parsedRows,
        IReadOnlyDictionary<string, ProductSnapshot> productsByCode,
        IReadOnlyList<SaleOutImportDuplicate> databaseDuplicates);
    byte[] GenerateImportTemplate();
    byte[] GeneratePrintPdf(string normalizedSaleOutNo, IReadOnlyList<SaleOut> saleOutItems);
    byte[] CreateRevenueReportExcel(
        IReadOnlyList<SaleOutRevenueReportItem> reportItems,
        DateOnly timeBegin,
        DateOnly timeEnd);
}

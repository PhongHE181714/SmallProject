using BE.Services.CatalogService.Domain.Aggregates;
using BE.Services.CatalogService.Domain.ValueObjects;

namespace BE.Services.CatalogService.Application.Services;

public interface IProductSpreadsheetService
{
    IReadOnlyList<(int RowNumber, MasterProduct Product)> ParseImportFile(Stream fileStream, string fileExtension);
    InvalidDataException CreateDatabaseDuplicateException(
        IReadOnlyList<(int RowNumber, MasterProduct Product)> importRows,
        IReadOnlyList<ProductImportDuplicate> databaseDuplicates);
    byte[] GenerateTemplate();
}

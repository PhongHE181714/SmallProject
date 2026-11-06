using System.Globalization;
using System.Text;
using BE.DTOs.Products;
using BE.Models.MasterProduct;
using BE.Repositories.Interfaces;
using Microsoft.VisualBasic.FileIO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BE.Services.Products;

public sealed partial class ProductService
{
    private const int MAX_TEXT_LENGTH_50 = 50;
    private const int MAX_TEXT_LENGTH_200 = 200;
    private const int MAX_DECIMAL_SCALE = 3;
    private const decimal MAX_DECIMAL_18_3 = 999_999_999_999_999.999m;
    private static readonly string[] REQUIRED_IMPORT_HEADERS = ["ProductCode", "ProductName", "Unit", "Specification", "QuantityPerBox", "ProductWeight"];

    public async Task<ProductImportResponse> ImportProductsAsync(Stream fileStream, string fileExtension)
    {
        ValidateFileExtension(fileExtension);

        var rows = ReadRows(fileStream, fileExtension);
        ValidateRowCount(rows.Count);
        var headerIndexes = GetAndValidateHeaders(rows);
        var parsedFile = ParseProductRows(rows, headerIndexes);
        ValidateParsedFile(parsedFile);

        var importedCount = await ImportProductsToDatabaseAsync(parsedFile.Products);

        return new ProductImportResponse
        {
            ImportedCount = importedCount
        };
    }
    private static void ValidateFileExtension(string fileExtension)
    {
        var normalizedExtension = fileExtension.Trim().ToLowerInvariant();
        if (normalizedExtension is not ".xlsx" and not ".csv")
        {
            throw CreateImportException("Chỉ hỗ trợ file Excel định dạng .xlsx hoặc .csv.", "File");
        }
    }
    private static List<string?[]> ReadRows(Stream fileStream, string fileExtension)
    {
        var normalizedExtension = fileExtension.Trim().ToLowerInvariant();
        try
        {
            return normalizedExtension == ".xlsx"
                ? ReadExcelRows(fileStream)
                : ReadCsvRows(fileStream);
        }
        catch (Exception exception) when (
            exception is InvalidDataException
                or IOException
                or FormatException
                or ArgumentException
                or MalformedLineException)
        {
            throw CreateImportException("Không thể đọc file. Hãy kiểm tra định dạng và nội dung file.", "File");
        }
    }
    private static void ValidateRowCount(int totalRowCount)
    {
        var dataRowCount = Math.Max(totalRowCount - 1, 0);
        if (dataRowCount > ProductImportLimits.MAX_DATA_ROWS)
        {
            throw CreateImportException(
                $"File không được vượt quá {ProductImportLimits.MAX_DATA_ROWS} dòng dữ liệu.",
                "File");
        }
    }

    private static IReadOnlyDictionary<string, int> GetAndValidateHeaders(IReadOnlyList<string?[]> rows)
    {
        if (rows.Count == 0)
        {
            throw CreateImportException("File không có dòng tiêu đề và dữ liệu.", "File");
        }

        var headerIndexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var columnIndex = 0; columnIndex < rows[0].Length; columnIndex++)
        {
            var header = rows[0][columnIndex]?.Trim();
            if (!string.IsNullOrEmpty(header) && !headerIndexes.ContainsKey(header))
            {
                headerIndexes.Add(header, columnIndex);
            }
        }

        var missingHeaders = REQUIRED_IMPORT_HEADERS
            .Where(header => !headerIndexes.ContainsKey(header))
            .Select(header => new ProductImportErrorResponse
            {
                Field = header,
                Message = $"File thiếu cột bắt buộc {header}."
            })
            .ToArray();

        if (missingHeaders.Length > 0)
        {
            throw CreateImportException("File thiếu cột dữ liệu bắt buộc.", missingHeaders);
        }

        return headerIndexes;
    }
    private static ParsedProductImport ParseProductRows(
        IReadOnlyList<string?[]> rows,
        IReadOnlyDictionary<string, int> headerIndexes)
    {
        var importRows = new List<(int RowNumber, MasterProduct Product)>();
        var errors = new List<ProductImportErrorResponse>();

        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var rowNumber = rowIndex + 1;
            var product = ParseProductRow(row, headerIndexes, rowNumber, errors);
            if (product is not null)
            {
                importRows.Add((rowNumber, product));
            }
        }

        return new ParsedProductImport(importRows, errors);
    }
    private static MasterProduct? ParseProductRow(
        IReadOnlyList<string?> row,
        IReadOnlyDictionary<string, int> headerIndexes,
        int rowNumber,
        ICollection<ProductImportErrorResponse> errors)
    {
        var initialErrorCount = errors.Count;
        var productCode = ValidateImportText(
            GetImportValue(row, headerIndexes, "ProductCode"), "ProductCode", "Mã sản phẩm", MAX_TEXT_LENGTH_50, rowNumber, errors);
        var productName = ValidateImportText(
            GetImportValue(row, headerIndexes, "ProductName"), "ProductName", "Tên sản phẩm", MAX_TEXT_LENGTH_200, rowNumber, errors);
        var unit = ValidateImportText(
            GetImportValue(row, headerIndexes, "Unit"), "Unit", "Đơn vị", MAX_TEXT_LENGTH_50, rowNumber, errors);
        var specification = ValidateImportText(
            GetImportValue(row, headerIndexes, "Specification"), "Specification", "Quy cách", MAX_TEXT_LENGTH_200, rowNumber, errors);
        var quantityPerBox = ParseQuantityPerBox(
            GetImportValue(row, headerIndexes, "QuantityPerBox"), rowNumber, errors);
        var productWeight = ParseProductWeight(
            GetImportValue(row, headerIndexes, "ProductWeight"), rowNumber, errors);

        if (errors.Count > initialErrorCount)
        {
            return null;
        }

        return new MasterProduct
        {
            Id = Guid.NewGuid(),
            ProductCode = productCode,
            ProductName = productName,
            Unit = unit,
            Specification = specification,
            QuantityPerBox = quantityPerBox,
            ProductWeight = productWeight,
            IsActive = true
        };
    }
    private static int? ParseQuantityPerBox(
        string? value,
        int rowNumber,
        ICollection<ProductImportErrorResponse> errors)
    {
        var quantityText = value?.Trim();
        if (string.IsNullOrWhiteSpace(quantityText))
        {
            errors.Add(CreateRowError(rowNumber, "QuantityPerBox", "Số lượng/thùng không được để trống."));
            return null;
        }

        if (!int.TryParse(quantityText, NumberStyles.None, CultureInfo.InvariantCulture, out var quantityPerBox)
            || quantityPerBox <= 0)
        {
            errors.Add(CreateRowError(rowNumber, "QuantityPerBox", "Số lượng/thùng phải là số nguyên dương."));
            return null;
        }

        return quantityPerBox;
    }
    private static decimal? ParseProductWeight(
        string? value,
        int rowNumber,
        ICollection<ProductImportErrorResponse> errors)
    {
        var weightText = value?.Trim();
        if (string.IsNullOrWhiteSpace(weightText))
        {
            errors.Add(CreateRowError(rowNumber, "ProductWeight", "Trọng lượng không được để trống."));
            return null;
        }

        if (!decimal.TryParse(
                weightText,
                NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out var productWeight)
            || productWeight <= 0
            || productWeight > MAX_DECIMAL_18_3
            || decimal.Round(productWeight, MAX_DECIMAL_SCALE) != productWeight)
        {
            errors.Add(CreateRowError(
                rowNumber,
                "ProductWeight",
                "Trọng lượng phải lớn hơn 0 và có tối đa 3 chữ số thập phân."));
            return null;
        }

        return productWeight;
    }
    private static void ValidateParsedFile(ParsedProductImport parsedFile)
    {
        if (parsedFile.Products.Count == 0 && parsedFile.Errors.Count == 0)
        {
            throw CreateImportException("File không có dòng sản phẩm để tải lên.", "File");
        }

        AddFileDuplicateErrors(parsedFile.Products, "ProductCode", "Mã sản phẩm", product => product.ProductCode, parsedFile.Errors);
        AddFileDuplicateErrors(parsedFile.Products, "ProductName", "Tên sản phẩm", product => product.ProductName, parsedFile.Errors);

        if (parsedFile.Errors.Count > 0)
        {
            throw CreateImportException("File có dữ liệu chưa hợp lệ.", parsedFile.Errors);
        }
    }
    private async Task<int> ImportProductsToDatabaseAsync(
        IReadOnlyList<(int RowNumber, MasterProduct Product)> importRows)
    {
        var products = importRows.Select(importRow => importRow.Product).ToArray();
        var databaseDuplicates = await _productRepository.ImportAsync(products);
        if (databaseDuplicates.Count == 0)
        {
            return products.Length;
        }

        var duplicateErrors = new List<ProductImportErrorResponse>();
        foreach (var duplicate in databaseDuplicates)
        {
            var isProductCode = duplicate.FieldName == "ProductCode";
            var fieldName = isProductCode ? "ProductCode" : "ProductName";
            var displayName = isProductCode ? "Mã sản phẩm" : "Tên sản phẩm";
            var matchingRows = importRows.Where(importRow => string.Equals(
                isProductCode ? importRow.Product.ProductCode : importRow.Product.ProductName,
                duplicate.Value,
                StringComparison.OrdinalIgnoreCase));

            duplicateErrors.AddRange(matchingRows.Select(importRow => CreateRowError(
                importRow.RowNumber,
                fieldName,
                $"{displayName} '{duplicate.Value}' đã tồn tại trên hệ thống.")));
        }

        if (duplicateErrors.Count == 0)
        {
            duplicateErrors.Add(new ProductImportErrorResponse
            {
                Field = "ProductCode/ProductName",
                Message = "Mã hoặc tên sản phẩm đã tồn tại trên hệ thống."
            });
        }

        throw CreateImportException(
            "File có sản phẩm đã tồn tại trên hệ thống. Không có dòng nào được import.",
            duplicateErrors,
            isConflict: true);
    }
    private static List<string?[]> ReadExcelRows(Stream fileStream)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet?.Dimension is null)
        {
            return [];
        }

        var totalRowCount = worksheet.Dimension.End.Row - worksheet.Dimension.Start.Row + 1;
        ValidateRowCount(totalRowCount);

        var rows = new List<string?[]>();
        for (var rowIndex = worksheet.Dimension.Start.Row; rowIndex <= worksheet.Dimension.End.Row; rowIndex++)
        {
            var row = new string?[worksheet.Dimension.End.Column];
            for (var columnIndex = worksheet.Dimension.Start.Column; columnIndex <= worksheet.Dimension.End.Column; columnIndex++)
            {
                row[columnIndex - 1] = Convert.ToString(
                    worksheet.Cells[rowIndex, columnIndex].Value,
                    CultureInfo.InvariantCulture);
            }

            rows.Add(row);
        }

        return rows;
    }
    private static List<string?[]> ReadCsvRows(Stream fileStream)
    {
        using var parser = new TextFieldParser(fileStream, Encoding.UTF8, detectEncoding: true)
        {
            HasFieldsEnclosedInQuotes = true,
            TrimWhiteSpace = false
        };
        parser.SetDelimiters(",");

        var rows = new List<string?[]>();
        while (!parser.EndOfData)
        {
            ValidateRowCount(rows.Count + 1);
            rows.Add(parser.ReadFields() ?? []);
        }

        return rows;
    }
    private static string? GetImportValue(
        IReadOnlyList<string?> row,
        IReadOnlyDictionary<string, int> headerIndexes,
        string header)
    {
        var columnIndex = headerIndexes[header];
        return columnIndex < row.Count ? row[columnIndex] : null;
    }
    private static string? ValidateImportText(
        string? value,
        string field,
        string displayName,
        int maxLength,
        int rowNumber,
        ICollection<ProductImportErrorResponse> errors)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            errors.Add(CreateRowError(rowNumber, field, $"{displayName} không được để trống."));
            return null;
        }

        if (normalizedValue.Length > maxLength)
        {
            errors.Add(CreateRowError(rowNumber, field, $"{displayName} không được vượt quá {maxLength} ký tự."));
            return null;
        }

        return normalizedValue;
    }
    private static void AddFileDuplicateErrors(
        IEnumerable<(int RowNumber, MasterProduct Product)> importRows,
        string field,
        string displayName,
        Func<MasterProduct, string?> valueSelector,
        ICollection<ProductImportErrorResponse> errors)
    {
        var duplicates = importRows
            .GroupBy(importRow => valueSelector(importRow.Product)!, StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1);

        foreach (var duplicateGroup in duplicates)
        {
            var rowNumbers = string.Join(", ", duplicateGroup.Select(importRow => importRow.RowNumber));
            foreach (var importRow in duplicateGroup)
            {
                errors.Add(CreateRowError(
                    importRow.RowNumber,
                    field,
                    $"{displayName} '{duplicateGroup.Key}' bị trùng trong file (dòng {rowNumbers})."));
            }
        }
    }
    private static InvalidDataException CreateImportException(string message, string field)
    {
        return CreateImportException(message,
        [
            new ProductImportErrorResponse { Field = field, Message = message }
        ]);
    }

    private static InvalidDataException CreateImportException(
        string message,
        IReadOnlyList<ProductImportErrorResponse> errors,
        bool isConflict = false)
    {
        var exception = new InvalidDataException(message);
        exception.Data["Errors"] = errors;
        exception.Data["IsConflict"] = isConflict;
        return exception;
    }
    private static ProductImportErrorResponse CreateRowError(int rowNumber, string field, string message)
    {
        return new ProductImportErrorResponse
        {
            RowNumber = rowNumber,
            Field = field,
            Message = message
        };
    }
    public byte[] GenerateProductTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;

        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("MasterProduct");
        string[] headers = ["ProductCode", "ProductName", "Unit", "Specification", "QuantityPerBox", "ProductWeight"];

        for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
        {
            var cell = sheet.Cells[1, columnIndex + 1];
            cell.Value = headers[columnIndex];
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(47, 104, 72));
        }

        object?[][] sampleRows =
        [
            ["P001", "Cà phê rang xay 250g", "Hộp", "250g x 12 hộp/thùng", 12, 0.250],
            ["P002", "Trà xanh túi lọc", "Hộp", "2g x 25 túi x 20 hộp", 20, 0.050],
            ["P003", "Mật ong hoa cà phê", "Chai", "500ml x 12 chai/thùng", 12, 0.600]
        ];

        for (var rowIndex = 0; rowIndex < sampleRows.Length; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < sampleRows[rowIndex].Length; columnIndex++)
            {
                sheet.Cells[rowIndex + 2, columnIndex + 1].Value = sampleRows[rowIndex][columnIndex];
            }
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        return package.GetAsByteArray();
    }
    private sealed record ParsedProductImport(
        IReadOnlyList<(int RowNumber, MasterProduct Product)> Products,
        List<ProductImportErrorResponse> Errors);
}

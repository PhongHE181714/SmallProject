using System.Globalization;
using System.Text;
using BE.DTOs.SaleOuts;
using BE.Models.MasterProduct;
using BE.Models.SaleOut;
using Microsoft.VisualBasic.FileIO;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BE.Services.SaleOuts;

public sealed partial class SaleOutService
{
    private const int IMPORT_MAX_TEXT_LENGTH_50 = 50;
    private const int IMPORT_MAX_TEXT_LENGTH_200 = 200;
    private const decimal MAX_IMPORT_DECIMAL_18_3_INTEGER = 999_999_999_999_999m;
    private static readonly string[] REQUIRED_IMPORT_HEADERS =
    ["CustomerPoNo","OrderDate", "CustomerName", "ProductCode", "Price", "Quantity", "QuantityPerBox"];

    public async Task<SaleOutImportResponse> ImportAsync(Stream fileStream, string fileExtension)
    {
        ValidateImportFileExtension(fileExtension);
        var rows = ReadImportRows(fileStream, fileExtension);
        ValidateImportRowCount(rows.Count);
        var headerIndexes = GetImportHeaderIndexes(rows);
        var parsedRows = ParseImportRows(rows, headerIndexes);
        ValidateImportFileDuplicates(parsedRows);
        ValidateImportOrderHeaders(parsedRows);

        var products = await _productRepository.FindActiveByCodesAsync(
            parsedRows.Select(row => row.ProductCode).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        var productsByCode = products
            .Where(product => !string.IsNullOrWhiteSpace(product.ProductCode))
            .ToDictionary(product => product.ProductCode!, StringComparer.OrdinalIgnoreCase);
        var errors = new List<SaleOutImportErrorResponse>();
        var saleOuts = new List<SaleOut>(parsedRows.Count);

        foreach (var row in parsedRows)
        {
            if (!productsByCode.TryGetValue(row.ProductCode, out var product))
            {
                errors.Add(CreateImportRowError(
                    row.RowNumber,
                    "ProductCode",
                    $"Mã sản phẩm '{row.ProductCode}' không tồn tại hoặc không hoạt động."));
                continue;
            }

            if (row.Price > 0 && row.Quantity > MAX_DECIMAL_18_3 / row.Price)
            {
                errors.Add(CreateImportRowError(
                    row.RowNumber,
                    "Price",
                    "Thành tiền vượt quá giới hạn lưu trữ."));
                continue;
            }

            var amount = decimal.Round(row.Quantity * row.Price, DECIMAL_SCALE, MidpointRounding.AwayFromZero);

            saleOuts.Add(new SaleOut
            {
                Id = Guid.NewGuid(),
                CustomerPoNo = row.CustomerPoNo,
                OrderDate = row.OrderDate.ToDateTime(TimeOnly.MinValue),
                CustomerName = row.CustomerName,
                ProductId = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Quantity = row.Quantity,
                Price = row.Price,
                Amount = amount,
                QuantityPerBox = row.QuantityPerBox,
                BoxQuantity = decimal.Ceiling(row.Quantity / row.QuantityPerBox),
                IsActive = true
            });
        }

        if (errors.Count > 0)
        {
            throw CreateImportException("File có dữ liệu chưa hợp lệ. Không có dòng nào được import.", errors);
        }

        var databaseDuplicates = await _saleOutRepository.ImportAsync(saleOuts);
        if (databaseDuplicates.Count > 0)
        {
            var conflictByKey = databaseDuplicates
                .GroupBy(duplicate => CreateImportPairKey(duplicate.CustomerPoNo, duplicate.ProductId.ToString()), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.Any(conflict => conflict.IsOrderHeaderMismatch), StringComparer.OrdinalIgnoreCase);

            var conflictErrors = parsedRows
                .Where(row => productsByCode.TryGetValue(row.ProductCode, out var product)
                    && conflictByKey.ContainsKey(CreateImportPairKey(row.CustomerPoNo, product.Id.ToString())))
                .Select(row =>
                {
                    var product = productsByCode[row.ProductCode];
                    var hasHeaderMismatch = conflictByKey[CreateImportPairKey(row.CustomerPoNo, product.Id.ToString())];
                    return CreateImportRowError(
                        row.RowNumber,
                        hasHeaderMismatch ? "CustomerName/OrderDate" : "CustomerPoNo/ProductCode",
                        hasHeaderMismatch
                            ? $"Số PO '{row.CustomerPoNo}' đã tồn tại nhưng tên khách hàng hoặc ngày đặt hàng không khớp đơn hiện tại."
                            : $"Số PO khách hàng '{row.CustomerPoNo}' và mã sản phẩm '{row.ProductCode}' đã có trên hệ thống.");
                })
                .ToArray();

            throw CreateImportException(
                "File có thông tin đơn hàng không khớp hoặc sản phẩm đã tồn tại. Không có dòng nào được import.",
                conflictErrors,
                isConflict: true);
        }

        return new SaleOutImportResponse { ImportedCount = saleOuts.Count };
    }

    public byte[] GenerateImportTemplate()
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var sheet = package.Workbook.Worksheets.Add("SaleOut");
        var headers = REQUIRED_IMPORT_HEADERS;

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
            ["PO-MAU-001", new DateTime(2026, 11, 1), "Khách hàng mẫu", "P001", 10000, 12, 12],
            ["PO-MAU-001", new DateTime(2026, 11, 1), "Khách hàng mẫu", "P002", 15000, 24, 12]
        ];

        for (var rowIndex = 0; rowIndex < sampleRows.Length; rowIndex++)
        {
            for (var columnIndex = 0; columnIndex < sampleRows[rowIndex].Length; columnIndex++)
            {
                sheet.Cells[rowIndex + 2, columnIndex + 1].Value = sampleRows[rowIndex][columnIndex];
            }

            sheet.Cells[rowIndex + 2, 2].Style.Numberformat.Format = "dd/mm/yyyy";
        }

        sheet.Cells[sheet.Dimension.Address].AutoFitColumns();
        sheet.View.FreezePanes(2, 1);
        return package.GetAsByteArray();
    }

    private static void ValidateImportFileExtension(string fileExtension)
    {
        if (fileExtension.Trim().ToLowerInvariant() is not ".xlsx" and not ".csv")
        {
            throw CreateImportException("Chỉ hỗ trợ file Excel định dạng .xlsx hoặc .csv.", "File");
        }
    }

    private static List<string?[]> ReadImportRows(Stream fileStream, string fileExtension)
    {
        try
        {
            return fileExtension.Trim().Equals(".xlsx", StringComparison.OrdinalIgnoreCase)
                ? ReadImportExcelRows(fileStream)
                : ReadImportCsvRows(fileStream);
        }
        catch (Exception exception) when (
            exception.Data["Errors"] is not IReadOnlyList<SaleOutImportErrorResponse>
            && (exception is InvalidDataException
                || exception is IOException
                || exception is FormatException
                || exception is ArgumentException
                || exception is MalformedLineException))
        {
            throw CreateImportException("Không thể đọc file. Hãy kiểm tra định dạng và nội dung file.", "File");
        }
    }

    private static List<string?[]> ReadImportExcelRows(Stream fileStream)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage(fileStream);
        var worksheet = package.Workbook.Worksheets.FirstOrDefault();
        if (worksheet?.Dimension is null)
        {
            return [];
        }

        var totalRowCount = worksheet.Dimension.End.Row - worksheet.Dimension.Start.Row + 1;
        ValidateImportRowCount(totalRowCount);
        var headerRowIndex = worksheet.Dimension.Start.Row;
        var orderDateColumnIndex = -1;
        for (var columnIndex = worksheet.Dimension.Start.Column; columnIndex <= worksheet.Dimension.End.Column; columnIndex++)
        {
            var header = Convert.ToString(worksheet.Cells[headerRowIndex, columnIndex].Value, CultureInfo.InvariantCulture)?.Trim();
            if (string.Equals(header, "OrderDate", StringComparison.OrdinalIgnoreCase))
            {
                orderDateColumnIndex = columnIndex;
                break;
            }
        }

        var rows = new List<string?[]>();
        for (var rowIndex = worksheet.Dimension.Start.Row; rowIndex <= worksheet.Dimension.End.Row; rowIndex++)
        {
            var row = new string?[worksheet.Dimension.End.Column];
            for (var columnIndex = worksheet.Dimension.Start.Column; columnIndex <= worksheet.Dimension.End.Column; columnIndex++)
            {
                var cell = worksheet.Cells[rowIndex, columnIndex];
                var value = cell.Value;
                if (rowIndex > headerRowIndex && columnIndex == orderDateColumnIndex)
                {
                    row[columnIndex - 1] = cell.Text;
                    continue;
                }

                row[columnIndex - 1] = value switch
                {
                    DateTime dateTime => dateTime.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    DateOnly dateOnly => dateOnly.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
                    _ => Convert.ToString(value, CultureInfo.InvariantCulture)
                };
            }

            rows.Add(row);
        }

        return rows;
    }

    private static List<string?[]> ReadImportCsvRows(Stream fileStream)
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
            ValidateImportRowCount(rows.Count + 1);
            rows.Add(parser.ReadFields() ?? []);
        }

        return rows;
    }

    private static void ValidateImportRowCount(int totalRowCount)
    {
        if (Math.Max(totalRowCount - 1, 0) > SaleOutImportLimits.MAX_DATA_ROWS)
        {
            throw CreateImportException(
                $"File không được vượt quá {SaleOutImportLimits.MAX_DATA_ROWS} dòng dữ liệu.",
                "File");
        }
    }

    private static IReadOnlyDictionary<string, int> GetImportHeaderIndexes(IReadOnlyList<string?[]> rows)
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
            .Select(header => new SaleOutImportErrorResponse
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

    private static List<ParsedSaleOutImportRow> ParseImportRows(
        IReadOnlyList<string?[]> rows,
        IReadOnlyDictionary<string, int> headerIndexes)
    {
        var parsedRows = new List<ParsedSaleOutImportRow>();
        var errors = new List<SaleOutImportErrorResponse>();
        for (var rowIndex = 1; rowIndex < rows.Count; rowIndex++)
        {
            var row = rows[rowIndex];
            if (row.All(string.IsNullOrWhiteSpace))
            {
                continue;
            }

            var rowNumber = rowIndex + 1;
            var errorCount = errors.Count;
            var customerPoNo = ValidateImportText(
                GetImportValue(row, headerIndexes, "CustomerPoNo"), "CustomerPoNo", "Số PO khách hàng", IMPORT_MAX_TEXT_LENGTH_50, rowNumber, errors);
            var orderDate = ParseImportDate(GetImportValue(row, headerIndexes, "OrderDate"), rowNumber, errors);
            var customerName = ValidateImportText(
                GetImportValue(row, headerIndexes, "CustomerName"), "CustomerName", "Khách hàng", IMPORT_MAX_TEXT_LENGTH_200, rowNumber, errors);
            var productCode = ValidateImportText(
                GetImportValue(row, headerIndexes, "ProductCode"), "ProductCode", "Mã sản phẩm", IMPORT_MAX_TEXT_LENGTH_50, rowNumber, errors);
            var price = ParseImportNonNegativeInteger(
                GetImportValue(row, headerIndexes, "Price"), rowNumber, errors);
            var quantity = ParseImportPositiveInteger(
                GetImportValue(row, headerIndexes, "Quantity"), "Quantity", "Số lượng", rowNumber, errors);
            var quantityPerBox = ParseImportPositiveInteger(
                GetImportValue(row, headerIndexes, "QuantityPerBox"), "QuantityPerBox", "Số lượng/thùng", rowNumber, errors);

            if (errors.Count == errorCount)
            {
                parsedRows.Add(new ParsedSaleOutImportRow(
                    rowNumber,
                    customerPoNo!,
                    orderDate!.Value,
                    customerName!,
                    productCode!,
                    price!.Value,
                    quantity!.Value,
                    quantityPerBox!.Value));
            }
        }

        if (errors.Count > 0)
        {
            throw CreateImportException("File có dữ liệu chưa hợp lệ. Hãy kiểm tra các dòng được báo lỗi.", errors);
        }

        if (parsedRows.Count == 0)
        {
            throw CreateImportException("File không có dòng dữ liệu để tải lên.", "File");
        }

        return parsedRows;
    }

    private static void ValidateImportFileDuplicates(IReadOnlyList<ParsedSaleOutImportRow> rows)
    {
        var duplicateGroups = rows
            .GroupBy(row => CreateImportPairKey(row.CustomerPoNo, row.ProductCode), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .ToArray();
        if (duplicateGroups.Length == 0)
        {
            return;
        }

        var errors = duplicateGroups.SelectMany(group => group.Select(row => CreateImportRowError(
            row.RowNumber,
            "CustomerPoNo/ProductCode",
            $"Số PO khách hàng '{row.CustomerPoNo}' và mã sản phẩm '{row.ProductCode}' bị trùng trong file.")))
            .ToArray();
        throw CreateImportException("File có PO và sản phẩm bị trùng. Không có dòng nào được import.", errors);
    }

    private static void ValidateImportOrderHeaders(IReadOnlyList<ParsedSaleOutImportRow> rows)
    {
        var mismatchedOrders = rows
            .GroupBy(row => row.CustomerPoNo, StringComparer.OrdinalIgnoreCase)
            .Where(group => group
                .Select(row => (row.OrderDate, row.CustomerName))
                .Distinct()
                .Skip(1)
                .Any())
            .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);

        if (mismatchedOrders.Count == 0)
        {
            return;
        }

        var errors = mismatchedOrders.Values
            .SelectMany(orderRows => orderRows.Select(row => CreateImportRowError(
                row.RowNumber,
                "CustomerPoNo/CustomerName/OrderDate",
                $"Các dòng cùng PO '{row.CustomerPoNo}' phải có cùng khách hàng và ngày đặt hàng.")))
            .ToArray();
        throw CreateImportException("File có thông tin đầu đơn không thống nhất. Không có dòng nào được import.", errors);
    }

    private static string? ValidateImportText(
        string? value,
        string field,
        string displayName,
        int maxLength,
        int rowNumber,
        ICollection<SaleOutImportErrorResponse> errors)
    {
        var normalizedValue = value?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedValue))
        {
            errors.Add(CreateImportRowError(rowNumber, field, $"{displayName} không được để trống."));
            return null;
        }

        if (normalizedValue.Length > maxLength)
        {
            errors.Add(CreateImportRowError(rowNumber, field, $"{displayName} không được vượt quá {maxLength} ký tự."));
            return null;
        }

        return normalizedValue;
    }

    private static DateOnly? ParseImportDate(
        string? value,
        int rowNumber,
        ICollection<SaleOutImportErrorResponse> errors)
    {
        var dateText = value?.Trim();
        if (DateOnly.TryParseExact(
                dateText,
                ["yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy", "yyyyMMdd", "M/d/yyyy", "MM/dd/yyyy"],
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var orderDate))
        {
            return orderDate;
        }

        errors.Add(CreateImportRowError(rowNumber, "OrderDate", "Ngày đặt hàng không hợp lệ. Dùng định dạng dd/MM/yyyy."));
        return null;
    }

    private static decimal? ParseImportPositiveInteger(
        string? value,
        string field,
        string displayName,
        int rowNumber,
        ICollection<SaleOutImportErrorResponse> errors)
    {
        var numberText = value?.Trim();
        if (!decimal.TryParse(numberText, NumberStyles.None, CultureInfo.InvariantCulture, out var number)
            || number <= 0
            || number > MAX_IMPORT_DECIMAL_18_3_INTEGER)
        {
            errors.Add(CreateImportRowError(rowNumber, field, $"{displayName} phải là số nguyên dương."));
            return null;
        }

        return number;
    }

    private static decimal? ParseImportNonNegativeInteger(
        string? value,
        int rowNumber,
        ICollection<SaleOutImportErrorResponse> errors)
    {
        var numberText = value?.Trim();
        if (!decimal.TryParse(numberText, NumberStyles.None, CultureInfo.InvariantCulture, out var price)
            || price < 0
            || price > MAX_IMPORT_DECIMAL_18_3_INTEGER)
        {
            errors.Add(CreateImportRowError(rowNumber, "Price", "Đơn giá phải là số nguyên không âm."));
            return null;
        }

        return price;
    }

    private static string? GetImportValue(IReadOnlyList<string?> row, IReadOnlyDictionary<string, int> headerIndexes, string header)
    {
        var columnIndex = headerIndexes[header];
        return columnIndex < row.Count ? row[columnIndex] : null;
    }

    private static string CreateImportPairKey(string customerPoNo, string productCode)
    {
        return $"{customerPoNo.Trim()}\u001F{productCode.Trim()}";
    }

    private static SaleOutImportErrorResponse CreateImportRowError(int rowNumber, string field, string message)
    {
        return new SaleOutImportErrorResponse { RowNumber = rowNumber, Field = field, Message = message };
    }

    private static InvalidDataException CreateImportException(string message, string field)
    {
        return CreateImportException(message,
        [new SaleOutImportErrorResponse { Field = field, Message = message }]);
    }

    private static InvalidDataException CreateImportException(
        string message,
        IReadOnlyList<SaleOutImportErrorResponse> errors,
        bool isConflict = false)
    {
        var exception = new InvalidDataException(message);
        exception.Data["Errors"] = errors;
        exception.Data["IsConflict"] = isConflict;
        return exception;
    }

    private sealed record ParsedSaleOutImportRow(
        int RowNumber,
        string CustomerPoNo,
        DateOnly OrderDate,
        string CustomerName,
        string ProductCode,
        decimal Price,
        decimal Quantity,
        decimal QuantityPerBox);
}

using System.Globalization;
using System.Text;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Domain.Aggregates;
using BE.Services.OrderingService.Domain.ProductReplica;
using BE.Services.OrderingService.Domain.ValueObjects;
using Microsoft.VisualBasic.FileIO;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BE.Services.OrderingService.Application.Services;

public sealed class SaleOutDocumentService : ISaleOutDocumentService
{
    private const int IMPORT_MAX_TEXT_LENGTH_50 = 50;
    private const int IMPORT_MAX_TEXT_LENGTH_200 = 200;
    private const decimal MAX_IMPORT_DECIMAL_18_3_INTEGER = 999_999_999_999_999m;
    private static readonly string[] REQUIRED_IMPORT_HEADERS =
    ["CustomerPoNo", "OrderDate", "CustomerName", "ProductCode", "Price", "Quantity", "QuantityPerBox"];

    public IReadOnlyList<ParsedSaleOutImportRow> ParseImportFile(Stream fileStream, string fileExtension)
    {
        ValidateImportFileExtension(fileExtension);
        var rows = ReadImportRows(fileStream, fileExtension);
        ValidateImportRowCount(rows.Count);
        var headerIndexes = GetImportHeaderIndexes(rows);
        var parsedRows = ParseImportRows(rows, headerIndexes);
        ValidateImportFileDuplicates(parsedRows);
        ValidateImportOrderHeaders(parsedRows);

        return parsedRows;
    }

    public IReadOnlyList<SaleOut> BuildSaleOutEntitiesFromImport(
        IReadOnlyList<ParsedSaleOutImportRow> parsedRows,
        IReadOnlyDictionary<string, ProductSnapshot> productsByCode)
    {
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

            if (row.Price > 0 && row.Quantity > SaleOut.MAX_DECIMAL_18_3 / row.Price)
            {
                errors.Add(CreateImportRowError(
                    row.RowNumber,
                    "Price",
                    "Thành tiền vượt quá giới hạn lưu trữ."));
                continue;
            }

            var amount = decimal.Round(row.Quantity * row.Price, SaleOut.DECIMAL_SCALE, MidpointRounding.AwayFromZero);

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

        return saleOuts;
    }

    public InvalidDataException CreateImportDatabaseConflictException(
        IReadOnlyList<ParsedSaleOutImportRow> parsedRows,
        IReadOnlyDictionary<string, ProductSnapshot> productsByCode,
        IReadOnlyList<SaleOutImportDuplicate> databaseDuplicates)
    {
        var conflictByKey = databaseDuplicates
            .GroupBy(
                duplicate => CreateImportPairKey(duplicate.CustomerPoNo, duplicate.ProductId.ToString()),
                StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group.Any(conflict => conflict.IsOrderHeaderMismatch),
                StringComparer.OrdinalIgnoreCase);

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

        return CreateImportException(
            "File có thông tin đơn hàng không khớp hoặc sản phẩm đã tồn tại. Không có dòng nào được import.",
            conflictErrors,
            isConflict: true);
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

    public byte[] GeneratePrintPdf(string normalizedSaleOutNo, IReadOnlyList<SaleOut> saleOutItems)
    {
        QuestPDF.Settings.License = LicenseType.Community;
        QuestPDF.Settings.UseSystemFonts = true;
        using var qrGenerator = new QRCodeGenerator();
        using var qrData = qrGenerator.CreateQrCode(normalizedSaleOutNo, QRCodeGenerator.ECCLevel.Q);
        var qrCode = new PngByteQRCode(qrData);
        var qrImage = qrCode.GetGraphic(8);
        var order = saleOutItems[0];
        var totalQuantity = saleOutItems.Sum(item => item.Quantity ?? 0m);
        var totalAmount = saleOutItems.Sum(item => item.Amount ?? 0m);
        var culture = CultureInfo.GetCultureInfo("vi-VN");

        return Document.Create(document => document.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(32);
            page.DefaultTextStyle(style => style.FontFamily("Times New Roman").FontSize(10));
            page.Content().Column(column =>
            {
                column.Spacing(16);
                column.Item().AlignCenter().Text("PHIẾU XUẤT HÀNG").FontSize(20).Bold();
                column.Item().Row(row =>
                {
                    row.RelativeItem().Column(info =>
                    {
                        info.Spacing(7);
                        info.Item().Text(text =>
                        {
                            text.Span("Khách hàng: ").Bold();
                            text.Span(order.CustomerName ?? string.Empty);
                        });
                        info.Item().Text(text =>
                        {
                            text.Span("Số PO khách hàng: ").Bold();
                            text.Span(order.CustomerPoNo ?? string.Empty);
                        });
                        info.Item().Text(text =>
                        {
                            text.Span("Ngày xuất kho: ").Bold();
                            text.Span(order.OrderDate?.ToString("dd/MM/yyyy", culture) ?? string.Empty);
                        });
                        info.Item().Text(text =>
                        {
                            text.Span("Số phiếu: ").Bold();
                            text.Span(order.SaleOutNo ?? normalizedSaleOutNo);
                        });
                    });
                    row.ConstantItem(92).AlignRight().Image(qrImage).FitArea();
                });

                column.Item().Table(table =>
                {
                    table.ColumnsDefinition(columns =>
                    {
                        columns.ConstantColumn(34);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(2.4f);
                        columns.RelativeColumn(1.1f);
                        columns.RelativeColumn(1.2f);
                        columns.RelativeColumn(1.4f);
                    });
                    table.Header(header =>
                    {
                        header.Cell().Element(PrintHeaderCell).AlignCenter().Text("STT");
                        header.Cell().Element(PrintHeaderCell).AlignCenter().Text("Mã sản phẩm");
                        header.Cell().Element(PrintHeaderCell).AlignCenter().Text("Tên sản phẩm");
                        header.Cell().Element(PrintHeaderCell).AlignCenter().Text("Số lượng");
                        header.Cell().Element(PrintHeaderCell).AlignCenter().Text("Đơn giá");
                        header.Cell().Element(PrintHeaderCell).AlignCenter().Text("Thành tiền");
                    });

                    for (var index = 0; index < saleOutItems.Count; index++)
                    {
                        var item = saleOutItems[index];
                        table.Cell().Element(PrintBodyCell).AlignCenter().Text((index + 1).ToString(culture));
                        table.Cell().Element(PrintBodyCell).AlignCenter().Text(item.ProductCode ?? string.Empty);
                        table.Cell().Element(PrintBodyCell).AlignCenter().Text(item.ProductName ?? string.Empty);
                        table.Cell().Element(PrintBodyCell).AlignCenter().Text((item.Quantity ?? 0m).ToString("N0", culture));
                        table.Cell().Element(PrintBodyCell).AlignCenter().Text((item.Price ?? 0m).ToString("N0", culture));
                        table.Cell().Element(PrintBodyCell).AlignCenter().Text((item.Amount ?? 0m).ToString("N0", culture));
                    }

                    table.Cell().ColumnSpan(3).Element(PrintBodyCell).AlignCenter().Text("Tổng:").Bold();
                    table.Cell().Element(PrintBodyCell).AlignCenter().Text(totalQuantity.ToString("N0", culture)).Bold();
                    table.Cell().Element(PrintBodyCell).AlignCenter().Text("x");
                    table.Cell().Element(PrintBodyCell).AlignCenter().Text(totalAmount.ToString("N0", culture)).Bold();
                });

                column.Item().PaddingTop(28).Row(row =>
                {
                    row.RelativeItem().AlignCenter().Text("Người lập phiếu").Bold();
                    row.RelativeItem().AlignCenter().Text("Người duyệt").Bold();
                    row.RelativeItem().AlignCenter().Text("Thủ kho").Bold();
                    row.RelativeItem().AlignCenter().Text("Người nhận").Bold();
                });
            });
        })).GeneratePdf();
    }

    public byte[] CreateRevenueReportExcel(
        IReadOnlyList<SaleOutRevenueReportItem> reportItems,
        DateOnly timeBegin,
        DateOnly timeEnd)
    {
        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
        using var package = new ExcelPackage();
        var worksheet = package.Workbook.Worksheets.Add("Doanh thu sản phẩm");
        var lastColumn = 5;
        const int HEADER_ROW = 4;
        const int FIRST_DATA_ROW = HEADER_ROW + 1;

        worksheet.Cells[1, 1, 1, lastColumn].Merge = true;
        worksheet.Cells[1, 1].Value = "BÁO CÁO DOANH THU SẢN PHẨM";
        worksheet.Cells[1, 1, 1, lastColumn].Style.Font.Bold = true;
        worksheet.Cells[1, 1, 1, lastColumn].Style.Font.Size = 16;
        worksheet.Cells[1, 1, 1, lastColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        worksheet.Cells[2, 1, 2, lastColumn].Merge = true;
        worksheet.Cells[2, 1].Value = $"Từ ngày: {timeBegin:dd/MM/yyyy}    Đến ngày: {timeEnd:dd/MM/yyyy}";
        worksheet.Cells[2, 1, 2, lastColumn].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;

        var headers = new[] { "STT", "Mã sản phẩm", "Tên sản phẩm", "Số lượng", "Thành tiền" };
        for (var columnIndex = 0; columnIndex < headers.Length; columnIndex++)
        {
            var cell = worksheet.Cells[HEADER_ROW, columnIndex + 1];
            cell.Value = headers[columnIndex];
            cell.Style.Font.Bold = true;
            cell.Style.Font.Color.SetColor(System.Drawing.Color.White);
            cell.Style.Fill.PatternType = ExcelFillStyle.Solid;
            cell.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.FromArgb(47, 104, 72));
            cell.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            cell.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
        }

        decimal totalQuantity = 0m;
        decimal totalAmount = 0m;
        for (var index = 0; index < reportItems.Count; index++)
        {
            var row = FIRST_DATA_ROW + index;
            var item = reportItems[index];
            worksheet.Cells[row, 1].Value = index + 1;
            worksheet.Cells[row, 2].Value = item.ProductCode;
            worksheet.Cells[row, 3].Value = item.ProductName;
            worksheet.Cells[row, 4].Value = item.Quantity;
            worksheet.Cells[row, 5].Value = item.Amount;
            worksheet.Cells[row, 1, row, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            worksheet.Cells[row, 4, row, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
            worksheet.Cells[row, 4].Style.Numberformat.Format = "#,##0";
            worksheet.Cells[row, 5].Style.Numberformat.Format = "#,##0";
            totalQuantity += item.Quantity;
            totalAmount += item.Amount;
        }

        var totalRow = FIRST_DATA_ROW + reportItems.Count;
        worksheet.Cells[totalRow, 1, totalRow, 3].Merge = true;
        worksheet.Cells[totalRow, 1].Value = "Tổng:";
        worksheet.Cells[totalRow, 1, totalRow, 3].Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
        worksheet.Cells[totalRow, 4].Value = totalQuantity;
        worksheet.Cells[totalRow, 4].Style.Numberformat.Format = "#,##0";
        worksheet.Cells[totalRow, 4].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        worksheet.Cells[totalRow, 5].Value = totalAmount;
        worksheet.Cells[totalRow, 5].Style.Numberformat.Format = "#,##0";
        worksheet.Cells[totalRow, 5].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        worksheet.Cells[totalRow, 1, totalRow, lastColumn].Style.Font.Bold = true;

        for (var rowIndex = HEADER_ROW; rowIndex <= totalRow; rowIndex++)
        {
            for (var columnIndex = 1; columnIndex <= lastColumn; columnIndex++)
            {
                var border = worksheet.Cells[rowIndex, columnIndex].Style.Border;
                border.Top.Style = ExcelBorderStyle.Thin;
                border.Bottom.Style = ExcelBorderStyle.Thin;
                border.Left.Style = ExcelBorderStyle.Thin;
                border.Right.Style = ExcelBorderStyle.Thin;
            }
        }

        worksheet.Cells[HEADER_ROW, 1, Math.Max(totalRow, HEADER_ROW), lastColumn].Style.Font.Name = "Times New Roman";
        worksheet.Cells[1, 1, totalRow, lastColumn].Style.Font.Name = "Times New Roman";
        worksheet.Cells[1, 1, totalRow, lastColumn].AutoFitColumns();
        worksheet.Column(1).Width = Math.Max(worksheet.Column(1).Width, 8);
        worksheet.Column(2).Width = Math.Max(worksheet.Column(2).Width, 18);
        worksheet.Column(3).Width = Math.Max(worksheet.Column(3).Width, 32);
        worksheet.Column(4).Width = Math.Max(worksheet.Column(4).Width, 16);
        worksheet.Column(5).Width = Math.Max(worksheet.Column(5).Width, 18);
        worksheet.View.FreezePanes(FIRST_DATA_ROW, 1);

        return package.GetAsByteArray();
    }

    private static IContainer PrintHeaderCell(IContainer container)
    {
        return container.Background(Colors.Grey.Lighten3).Border(0.7f).Padding(5).AlignMiddle();
    }

    private static IContainer PrintBodyCell(IContainer container)
    {
        return container.Border(0.7f).Padding(5).AlignMiddle();
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

    private static string? GetImportValue(
        IReadOnlyList<string?> row,
        IReadOnlyDictionary<string, int> headerIndexes,
        string header)
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
        exception.Data["ProblemTitle"] = isConflict
            ? "Thông tin đơn hàng không khớp hoặc sản phẩm đã tồn tại"
            : "File tải lên chưa hợp lệ";
        return exception;
    }
}

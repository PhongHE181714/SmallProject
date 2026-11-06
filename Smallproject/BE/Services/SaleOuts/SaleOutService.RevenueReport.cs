using BE.Models.SaleOut;
using OfficeOpenXml;
using OfficeOpenXml.Style;

namespace BE.Services.SaleOuts;

public sealed partial class SaleOutService
{
    private const int REVENUE_REPORT_QUERY_PAGE_SIZE = 500;

    public async Task<byte[]> GenerateRevenueReportAsync(DateOnly timeBegin, DateOnly timeEnd)
    {
        if (timeBegin > timeEnd)
        {
            throw new ArgumentException("Từ ngày không được lớn hơn đến ngày.");
        }

        var reportItems = new List<SaleOutRevenueReportItem>();
        long offset = 0;
        long totalItems;
        do
        {
            var page = await _saleOutRepository.RevenueReportAsync(
                timeBegin,
                timeEnd,
                offset,
                REVENUE_REPORT_QUERY_PAGE_SIZE);
            totalItems = page.TotalItems;
            reportItems.AddRange(page.Items);
            offset += page.Items.Count;

            if (page.Items.Count == 0)
            {
                break;
            }
        }
        while (offset < totalItems);

        return CreateRevenueReportExcel(reportItems, timeBegin, timeEnd);
    }

    private static byte[] CreateRevenueReportExcel(
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
}

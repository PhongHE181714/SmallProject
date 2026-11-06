using System.Globalization;
using BE.DTOs.SaleOuts;
using BE.Models.SaleOut;
using QRCoder;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BE.Services.SaleOuts;

public sealed partial class SaleOutService
{
    public async Task<SaleOutPrintOptionsResponse> SearchPrintOptionsAsync(SaleOutPrintOptionsRequest request)
    {
        if (request.Page < 1)
        {
            throw new ArgumentException("Số trang phải lớn hơn hoặc bằng 1.");
        }

        if (request.PageSize is < 1 or > SaleOutPrintOptionsLimits.MAX_PAGE_SIZE)
        {
            throw new ArgumentException($"Số đơn mỗi trang phải từ 1 đến {SaleOutPrintOptionsLimits.MAX_PAGE_SIZE}.");
        }

        var searchText = request.SearchValue?.Trim();
        if (searchText?.Length > SaleOutPrintOptionsLimits.MAX_SALE_OUT_NO_SEARCH_LENGTH)
        {
            throw new ArgumentException($"Mã số phiếu tìm kiếm không được vượt quá {SaleOutPrintOptionsLimits.MAX_SALE_OUT_NO_SEARCH_LENGTH} ký tự.");
        }

        if (string.IsNullOrWhiteSpace(searchText))
        {
            searchText = null;
        }

        var result = await _saleOutRepository.SearchPrintOptionsAsync(
            searchText,
            (long)(request.Page - 1) * request.PageSize,
            request.PageSize);
        return new SaleOutPrintOptionsResponse
        {
            Items = result.Items.Select(order => new SaleOutPrintOptionResponse
            {
                SaleOutNo = order.SaleOutNo,
                CustomerPoNo = order.CustomerPoNo,
                OrderDate = order.OrderDate is { } orderDate ? DateOnly.FromDateTime(orderDate) : null,
                CustomerName = order.CustomerName,
                ProductCount = order.ProductCount,
                TotalAmount = order.TotalAmount
            }).ToArray(),
            Page = request.Page,
            PageSize = request.PageSize,
            TotalItems = result.TotalItems,
            TotalPages = result.TotalItems == 0
                ? 0
                : (result.TotalItems + request.PageSize - 1) / request.PageSize
        };
    }

    public async Task<byte[]?> GeneratePrintPdfAsync(string saleOutNo)
    {
        var normalizedSaleOutNo = NormalizeRequiredText(saleOutNo, TEXT_LENGTH_50, "Mã số phiếu");
        var saleOutItems = (await _saleOutRepository.GetOrderAsync(normalizedSaleOutNo))
            .OrderBy(item => item.ProductCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ProductCode, StringComparer.Ordinal)
            .ToArray();
        if (saleOutItems.Length == 0)
        {
            return null;
        }

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

                    for (var index = 0; index < saleOutItems.Length; index++)
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

    private static IContainer PrintHeaderCell(IContainer container)
    {
        return container.Background(Colors.Grey.Lighten3).Border(0.7f).Padding(5).AlignMiddle();
    }

    private static IContainer PrintBodyCell(IContainer container)
    {
        return container.Border(0.7f).Padding(5).AlignMiddle();
    }
}

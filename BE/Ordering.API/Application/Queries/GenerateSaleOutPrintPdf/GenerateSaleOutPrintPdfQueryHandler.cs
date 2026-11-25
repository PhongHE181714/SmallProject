using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Application.Services;
using BE.Services.OrderingService.Domain.Repositories;
using BE.Services.OrderingService.Domain.ValueObjects;

namespace BE.Services.OrderingService.Application.Queries.GenerateSaleOutPrintPdf;

public sealed class GenerateSaleOutPrintPdfQueryHandler
    : IQueryHandler<GenerateSaleOutPrintPdfQuery, FileDownloadResult>
{
    private const string PDF_CONTENT_TYPE = "application/pdf";

    private readonly ISaleOutRepository _saleOutRepository;
    private readonly ISaleOutDocumentService _documentService;

    public GenerateSaleOutPrintPdfQueryHandler(
        ISaleOutRepository saleOutRepository,
        ISaleOutDocumentService documentService)
    {
        _saleOutRepository = saleOutRepository;
        _documentService = documentService;
    }

    public async Task<FileDownloadResult> HandleAsync(
        GenerateSaleOutPrintPdfQuery query,
        CancellationToken cancellationToken = default)
    {
        string normalizedSaleOutNo;
        try
        {
            normalizedSaleOutNo = CustomerPurchaseOrder.NormalizeRequiredText(
                query.SaleOutNo,
                CustomerPurchaseOrder.MAX_PO_NO_LENGTH,
                "Mã số phiếu");
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle("Mã số phiếu không hợp lệ");
        }

        var saleOutItems = (await _saleOutRepository.GetOrderAsync(normalizedSaleOutNo))
            .OrderBy(item => item.ProductCode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.ProductCode, StringComparer.Ordinal)
            .ToArray();

        if (saleOutItems.Length == 0)
        {
            throw new KeyNotFoundException(string.Empty)
                .WithTitle("Không tìm thấy phiếu xuất hàng đang hoạt động");
        }

        var pdfBytes = _documentService.GeneratePrintPdf(normalizedSaleOutNo, saleOutItems);
        var safeFileName = string.Concat(
            query.SaleOutNo.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));

        return new FileDownloadResult(
            pdfBytes,
            PDF_CONTENT_TYPE,
            $"phieu-xuat-hang-{safeFileName}.pdf");
    }
}

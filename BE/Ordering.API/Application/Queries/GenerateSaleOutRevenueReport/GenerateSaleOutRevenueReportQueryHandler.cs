using System.Globalization;
using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Application.Services;
using BE.Services.OrderingService.Domain.Aggregates;
using BE.Services.OrderingService.Domain.Repositories;

namespace BE.Services.OrderingService.Application.Queries.GenerateSaleOutRevenueReport;

public sealed class GenerateSaleOutRevenueReportQueryHandler
    : IQueryHandler<GenerateSaleOutRevenueReportQuery, FileDownloadResult>
{
    private const int REVENUE_REPORT_QUERY_PAGE_SIZE = 500;
    private const string EXCEL_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string INVALID_DATE_RANGE_TITLE = "Khoảng ngày báo cáo chưa hợp lệ";

    private readonly ISaleOutRepository _saleOutRepository;
    private readonly ISaleOutDocumentService _documentService;

    public GenerateSaleOutRevenueReportQueryHandler(
        ISaleOutRepository saleOutRepository,
        ISaleOutDocumentService documentService)
    {
        _saleOutRepository = saleOutRepository;
        _documentService = documentService;
    }

    public async Task<FileDownloadResult> HandleAsync(
        GenerateSaleOutRevenueReportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!TryParseRevenueReportDate(query.TimeBeginText, out var parsedTimeBegin)
            || !TryParseRevenueReportDate(query.TimeEndText, out var parsedTimeEnd))
        {
            throw new ArgumentException("Vui lòng truyền đủ ngày theo định dạng dd/MM/yyyy hoặc yyyy-MM-dd.")
                .WithTitle(INVALID_DATE_RANGE_TITLE);
        }

        if (parsedTimeBegin > parsedTimeEnd)
        {
            throw new ArgumentException("Từ ngày không được lớn hơn đến ngày.")
                .WithTitle(INVALID_DATE_RANGE_TITLE);
        }

        var reportItems = new List<SaleOutRevenueReportItem>();
        long offset = 0;
        long totalItems;
        do
        {
            var page = await _saleOutRepository.RevenueReportAsync(
                parsedTimeBegin,
                parsedTimeEnd,
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

        if (reportItems.Count == 0)
        {
            throw new KeyNotFoundException("Không tìm thấy bản ghi xuất hàng trong khoảng thời gian đã chọn.")
                .WithTitle("Không có dữ liệu báo cáo doanh thu");
        }

        var fileBytes = _documentService.CreateRevenueReportExcel(reportItems, parsedTimeBegin, parsedTimeEnd);
        var fileName = $"bao-cao-doanh-thu-{parsedTimeBegin:yyyyMMdd}-{parsedTimeEnd:yyyyMMdd}.xlsx";

        return new FileDownloadResult(fileBytes, EXCEL_CONTENT_TYPE, fileName);
    }

    private static bool TryParseRevenueReportDate(string? value, out DateOnly date)
    {
        return DateOnly.TryParseExact(
            value?.Trim(),
            ["dd/MM/yyyy", "d/M/yyyy", "yyyy-MM-dd", "yyyyMMdd"],
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out date);
    }
}

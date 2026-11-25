using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Queries.GenerateSaleOutRevenueReport;

public sealed record GenerateSaleOutRevenueReportQuery(
    string? TimeBeginText,
    string? TimeEndText) : IQuery<FileDownloadResult>;

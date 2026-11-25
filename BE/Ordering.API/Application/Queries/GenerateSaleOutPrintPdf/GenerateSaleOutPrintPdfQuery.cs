using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.DTOs;

namespace BE.Services.OrderingService.Application.Queries.GenerateSaleOutPrintPdf;

public sealed record GenerateSaleOutPrintPdfQuery(string SaleOutNo) : IQuery<FileDownloadResult>;

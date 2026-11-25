using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Application.Services;
using BE.Services.OrderingService.Domain.ProductReplica;
using BE.Services.OrderingService.Domain.Repositories;

namespace BE.Services.OrderingService.Application.Commands.ImportSaleOuts;

public sealed class ImportSaleOutsCommandHandler : ICommandHandler<ImportSaleOutsCommand, SaleOutImportResponse>
{
    private readonly ISaleOutRepository _saleOutRepository;
    private readonly IProductSnapshotRepository _productSnapshotRepository;
    private readonly ISaleOutDocumentService _documentService;

    public ImportSaleOutsCommandHandler(
        ISaleOutRepository saleOutRepository,
        IProductSnapshotRepository productSnapshotRepository,
        ISaleOutDocumentService documentService)
    {
        _saleOutRepository = saleOutRepository;
        _productSnapshotRepository = productSnapshotRepository;
        _documentService = documentService;
    }

    public async Task<SaleOutImportResponse> HandleAsync(
        ImportSaleOutsCommand command,
        CancellationToken cancellationToken = default)
    {
        var file = command.File;
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file .xlsx hoặc .csv có dữ liệu.")
                .WithTitle("Chưa chọn file tải lên");
        }

        if (file.Length > SaleOutImportLimits.MAX_FILE_SIZE_BYTES)
        {
            throw new PayloadTooLargeException(
                "File vượt quá dung lượng cho phép",
                "Vui lòng chọn file không lớn hơn 10 MB.");
        }

        await using var fileStream = file.OpenReadStream();
        var parsedRows = _documentService.ParseImportFile(fileStream, Path.GetExtension(file.FileName));
        var distinctCodes = parsedRows
            .Select(row => row.ProductCode)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var products = await _productSnapshotRepository.FindActiveByCodesAsync(distinctCodes);
        var productsByCode = products
            .Where(product => !string.IsNullOrWhiteSpace(product.ProductCode))
            .ToDictionary(product => product.ProductCode!, StringComparer.OrdinalIgnoreCase);

        var saleOuts = _documentService.BuildSaleOutEntitiesFromImport(parsedRows, productsByCode);
        var databaseDuplicates = await _saleOutRepository.ImportAsync(saleOuts);
        if (databaseDuplicates.Count > 0)
        {
            throw _documentService.CreateImportDatabaseConflictException(
                parsedRows,
                productsByCode,
                databaseDuplicates);
        }

        return new SaleOutImportResponse
        {
            ImportedCount = saleOuts.Count
        };
    }
}

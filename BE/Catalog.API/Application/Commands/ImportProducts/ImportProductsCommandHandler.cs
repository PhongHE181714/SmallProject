using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.EventBus.Abstractions;
using BE.BuildingBlocks.EventBus.Events;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.CatalogService.Application.DTOs;
using BE.Services.CatalogService.Application.Services;
using BE.Services.CatalogService.Domain.Repositories;

namespace BE.Services.CatalogService.Application.Commands.ImportProducts;

public sealed class ImportProductsCommandHandler : ICommandHandler<ImportProductsCommand, ProductImportResponse>
{
    private readonly IProductRepository _productRepository;
    private readonly IProductSpreadsheetService _spreadsheetService;
    private readonly IEventBus _eventBus;

    public ImportProductsCommandHandler(
        IProductRepository productRepository,
        IProductSpreadsheetService spreadsheetService,
        IEventBus eventBus)
    {
        _productRepository = productRepository;
        _spreadsheetService = spreadsheetService;
        _eventBus = eventBus;
    }

    public async Task<ProductImportResponse> HandleAsync(
        ImportProductsCommand command,
        CancellationToken cancellationToken = default)
    {
        var file = command.File;
        if (file is null || file.Length == 0)
        {
            throw new ArgumentException("Vui lòng chọn file .xlsx hoặc .csv có dữ liệu.")
                .WithTitle("Chưa chọn file tải lên");
        }

        if (file.Length > ProductImportLimits.MAX_FILE_SIZE_BYTES)
        {
            throw new PayloadTooLargeException(
                "File vượt quá dung lượng cho phép",
                "Vui lòng chọn file không lớn hơn 10 MB.");
        }

        await using var fileStream = file.OpenReadStream();
        var importRows = _spreadsheetService.ParseImportFile(fileStream, Path.GetExtension(file.FileName));
        var products = importRows.Select(importRow => importRow.Product).ToArray();
        var databaseDuplicates = await _productRepository.ImportAsync(products);

        if (databaseDuplicates.Count > 0)
        {
            throw _spreadsheetService.CreateDatabaseDuplicateException(importRows, databaseDuplicates);
        }

        foreach (var product in products)
        {
            await _eventBus.PublishAsync(
                new ProductUpsertedIntegrationEvent(
                    product.Id,
                    product.ProductCode!,
                    product.ProductName!,
                    product.Unit!,
                    product.Specification!,
                    product.QuantityPerBox!.Value,
                    product.ProductWeight!.Value,
                    product.IsActive),
                cancellationToken);
            product.ClearDomainEvents();
        }

        return new ProductImportResponse
        {
            ImportedCount = products.Length
        };
    }
}

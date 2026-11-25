using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.EventBus.Abstractions;
using BE.BuildingBlocks.EventBus.Events;
using BE.BuildingBlocks.SharedKernel;
using BE.Services.CatalogService.Application.DTOs;
using BE.Services.CatalogService.Domain.Aggregates;
using BE.Services.CatalogService.Domain.Repositories;

namespace BE.Services.CatalogService.Application.Commands.CreateProduct;

public sealed class CreateProductCommandHandler : ICommandHandler<CreateProductCommand, ProductCreateResponse>
{
    private const string INVALID_PRODUCT_TITLE = "Thông tin sản phẩm không hợp lệ";
    private const string DUPLICATE_PRODUCT_TITLE = "Sản phẩm đã tồn tại";

    private readonly IProductRepository _productRepository;
    private readonly IEventBus _eventBus;

    public CreateProductCommandHandler(IProductRepository productRepository, IEventBus eventBus)
    {
        _productRepository = productRepository;
        _eventBus = eventBus;
    }

    public async Task<ProductCreateResponse> HandleAsync(
        CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var request = command.Request
                ?? throw new ArgumentException("Thông tin sản phẩm không được để trống.", nameof(command));

            var product = MasterProduct.Create(
                request.ProductCode,
                request.ProductName,
                request.Unit,
                request.Specification,
                request.QuantityPerBox,
                request.ProductWeight);

            var duplicateFields = await _productRepository.FindDuplicateFieldsAsync(
                product.ProductCode,
                product.ProductName);

            if (duplicateFields.Count > 0)
            {
                throw CreateDuplicateProductException(duplicateFields, product.ProductCode, product.ProductName);
            }

            await _productRepository.CreateAsync(product);

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

            return new ProductCreateResponse
            {
                Id = product.Id,
                ProductCode = product.ProductCode,
                ProductName = product.ProductName,
                Unit = product.Unit,
                Specification = product.Specification,
                QuantityPerBox = product.QuantityPerBox,
                ProductWeight = product.ProductWeight,
                IsActive = product.IsActive
            };
        }
        catch (ArgumentException exception)
        {
            throw exception.WithTitle(INVALID_PRODUCT_TITLE);
        }
        catch (InvalidOperationException exception)
        {
            throw exception.WithTitle(DUPLICATE_PRODUCT_TITLE);
        }
    }

    internal static InvalidOperationException CreateDuplicateProductException(
        IReadOnlyList<string> duplicateFields,
        string? productCode,
        string? productName)
    {
        var messages = duplicateFields.Select(field => field switch
        {
            "ProductCode" => $"Mã sản phẩm {productCode} đã tồn tại, vui lòng nhập lại.",
            "ProductName" => $"Tên sản phẩm {productName} đã tồn tại, vui lòng nhập lại.",
            _ => "Thông tin sản phẩm đã tồn tại."
        });

        return new InvalidOperationException(string.Join(" ", messages))
            .WithTitle(DUPLICATE_PRODUCT_TITLE);
    }
}

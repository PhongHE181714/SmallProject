using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.Commands.CreateProduct;
using BE.Services.CatalogService.Application.Commands.DeleteProduct;
using BE.Services.CatalogService.Application.Commands.ImportProducts;
using BE.Services.CatalogService.Application.Commands.UpdateProduct;
using BE.Services.CatalogService.Application.DTOs;
using BE.Services.CatalogService.Application.Queries.GenerateProductTemplate;
using BE.Services.CatalogService.Application.Queries.SearchProducts;
using Microsoft.AspNetCore.Mvc;

namespace BE.Services.CatalogService.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private const string EXCEL_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string TEMPLATE_FILE_NAME = "master-product-template.xlsx";

    private readonly ISender _sender;

    public ProductsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] ProductSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendQueryAsync<SearchProductsQuery, ProductSearchResponse>(
            new SearchProductsQuery(request),
            cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendCommandAsync<CreateProductCommand, ProductCreateResponse>(
            new CreateProductCommand(request),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductCreateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update(
        [FromRoute] Guid id,
        [FromBody] UpdateProductRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendCommandAsync<UpdateProductCommand, ProductCreateResponse>(
            new UpdateProductCommand(id, request),
            cancellationToken);
        return Ok(response);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(
        [FromRoute] Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.SendCommandAsync<DeleteProductCommand, Unit>(
            new DeleteProductCommand(id),
            cancellationToken);
        return NoContent();
    }

    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var fileBytes = await _sender.SendQueryAsync<GenerateProductTemplateQuery, byte[]>(
            new GenerateProductTemplateQuery(),
            cancellationToken);
        return File(fileBytes, EXCEL_CONTENT_TYPE, TEMPLATE_FILE_NAME);
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [ProducesResponseType(typeof(ProductImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Import(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendCommandAsync<ImportProductsCommand, ProductImportResponse>(
            new ImportProductsCommand(file),
            cancellationToken);
        return Ok(response);
    }
}

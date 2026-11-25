using BE.BuildingBlocks.CQRS;
using BE.Services.OrderingService.Application.Commands.CreateSaleOut;
using BE.Services.OrderingService.Application.Commands.DeleteSaleOutLine;
using BE.Services.OrderingService.Application.Commands.DeleteSaleOutOrder;
using BE.Services.OrderingService.Application.Commands.ImportSaleOuts;
using BE.Services.OrderingService.Application.Commands.UpdateSaleOutLine;
using BE.Services.OrderingService.Application.Commands.UpdateSaleOutOrder;
using BE.Services.OrderingService.Application.DTOs;
using BE.Services.OrderingService.Application.Queries.GenerateSaleOutPrintPdf;
using BE.Services.OrderingService.Application.Queries.GenerateSaleOutRevenueReport;
using BE.Services.OrderingService.Application.Queries.GenerateSaleOutTemplate;
using BE.Services.OrderingService.Application.Queries.GetSaleOutOrder;
using BE.Services.OrderingService.Application.Queries.SearchSaleOutPrintOptions;
using BE.Services.OrderingService.Application.Queries.SearchSaleOuts;
using Microsoft.AspNetCore.Mvc;

namespace BE.Services.OrderingService.Controllers;

[ApiController]
[Route("api/sale-outs")]
public sealed class SaleOutsController : ControllerBase
{
    private const string EXCEL_CONTENT_TYPE = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string TEMPLATE_FILE_NAME = "sale-out-template.xlsx";

    private readonly ISender _sender;

    public SaleOutsController(ISender sender)
    {
        _sender = sender;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SaleOutCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateSaleOutRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendCommandAsync<CreateSaleOutCommand, SaleOutCreateResponse>(
            new CreateSaleOutCommand(request),
            cancellationToken);
        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet]
    [ProducesResponseType(typeof(SaleOutSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search(
        [FromQuery] SaleOutSearchRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendQueryAsync<SearchSaleOutsQuery, SaleOutSearchResponse>(
            new SearchSaleOutsQuery(request),
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("print-options")]
    [ProducesResponseType(typeof(SaleOutPrintOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPrintOptions(
        [FromQuery] SaleOutPrintOptionsRequest request,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendQueryAsync<SearchSaleOutPrintOptionsQuery, SaleOutPrintOptionsResponse>(
            new SearchSaleOutPrintOptionsQuery(request),
            cancellationToken);
        return Ok(response);
    }

    [HttpGet("print/{saleOutNo}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPrintPdf(
        string saleOutNo,
        CancellationToken cancellationToken)
    {
        var file = await _sender.SendQueryAsync<GenerateSaleOutPrintPdfQuery, FileDownloadResult>(
            new GenerateSaleOutPrintPdfQuery(saleOutNo),
            cancellationToken);
        return File(file.FileBytes, file.ContentType, file.FileName);
    }

    [HttpGet("revenue-report")]
    [Produces(EXCEL_CONTENT_TYPE)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadRevenueReport(
        [FromQuery] string? timeBegin,
        [FromQuery] string? timeEnd,
        CancellationToken cancellationToken)
    {
        var file = await _sender.SendQueryAsync<GenerateSaleOutRevenueReportQuery, FileDownloadResult>(
            new GenerateSaleOutRevenueReportQuery(timeBegin, timeEnd),
            cancellationToken);
        return File(file.FileBytes, file.ContentType, file.FileName);
    }

    [HttpGet("order/{saleOutNo}")]
    [ProducesResponseType(typeof(IReadOnlyList<SaleOutListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(
        string saleOutNo,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendQueryAsync<GetSaleOutOrderQuery, IReadOnlyList<SaleOutListItemResponse>>(
            new GetSaleOutOrderQuery(saleOutNo),
            cancellationToken);
        return Ok(response);
    }

    [HttpPut("order/{saleOutNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOrder(
        string saleOutNo,
        [FromBody] UpdateSaleOutOrderRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.SendCommandAsync<UpdateSaleOutOrderCommand, Unit>(
            new UpdateSaleOutOrderCommand(saleOutNo, request),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("order/{saleOutNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(
        string saleOutNo,
        CancellationToken cancellationToken)
    {
        await _sender.SendCommandAsync<DeleteSaleOutOrderCommand, Unit>(
            new DeleteSaleOutOrderCommand(saleOutNo),
            cancellationToken);
        return NoContent();
    }

    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> DownloadTemplate(CancellationToken cancellationToken)
    {
        var fileBytes = await _sender.SendQueryAsync<GenerateSaleOutTemplateQuery, byte[]>(
            new GenerateSaleOutTemplateQuery(),
            cancellationToken);
        return File(fileBytes, EXCEL_CONTENT_TYPE, TEMPLATE_FILE_NAME);
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(SaleOutImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [RequestFormLimits(MultipartBodyLengthLimit = SaleOutImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [ProducesResponseType(typeof(SaleOutImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Import(
        IFormFile? file,
        CancellationToken cancellationToken)
    {
        var response = await _sender.SendCommandAsync<ImportSaleOutsCommand, SaleOutImportResponse>(
            new ImportSaleOutsCommand(file),
            cancellationToken);
        return Ok(response);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateSaleOutRequest request,
        CancellationToken cancellationToken)
    {
        await _sender.SendCommandAsync<UpdateSaleOutLineCommand, Unit>(
            new UpdateSaleOutLineCommand(id, request),
            cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken)
    {
        await _sender.SendCommandAsync<DeleteSaleOutLineCommand, Unit>(
            new DeleteSaleOutLineCommand(id),
            cancellationToken);
        return NoContent();
    }
}

using BE.DTOs.Products;
using BE.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BE.Controllers;

[ApiController]
[Route("api/products")]
public sealed class ProductsController : ControllerBase
{
    private readonly IProductService _productService;

    public ProductsController(IProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(ProductSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] ProductSearchRequest request)
    {
        try
        {
            var response = await _productService.SearchAsync(request);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thông tin tìm kiếm không hợp lệ",
                Detail = exception.Message
            });
        }
    }

    [HttpPost]
    [ProducesResponseType(typeof(ProductCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateProductRequest request)
    {
        try
        {
            var response = await _productService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thông tin sản phẩm không hợp lệ",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Sản phẩm đã tồn tại",
                Detail = exception.Message
            });
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(ProductCreateResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Update([FromRoute] Guid id, [FromBody] UpdateProductRequest request)
    {
        try
        {
            var response = await _productService.UpdateAsync(id, request);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thông tin sản phẩm không hợp lệ",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Sản phẩm đã tồn tại",
                Detail = exception.Message
            });
        }
    }

    

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete([FromRoute] Guid id)
    {
        try
        {
            await _productService.DeleteAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy sản phẩm",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Không thể xóa sản phẩm",
                Detail = exception.Message
            });
        }
    }

    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var fileBytes = _productService.GenerateProductTemplate();
        return File(fileBytes, contentType, "master-product-template.xlsx");
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(ProductImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [RequestFormLimits(MultipartBodyLengthLimit = ProductImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [ProducesResponseType(typeof(ProductImportResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status413PayloadTooLarge)]
    public async Task<IActionResult> Import(IFormFile? file)
    {
        if (file is null || file.Length == 0)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Chưa chọn file tải lên",
                Detail = "Vui lòng chọn file .xlsx hoặc .csv có dữ liệu."
            });
        }

        if (file.Length > ProductImportLimits.MAX_FILE_SIZE_BYTES)
        {
            return StatusCode(StatusCodes.Status413PayloadTooLarge, new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = "File vượt quá dung lượng cho phép",
                Detail = "Vui lòng chọn file không lớn hơn 10 MB."
            });
        }

        try
        {
            await using var fileStream = file.OpenReadStream();
            var response = await _productService.ImportProductsAsync(
                fileStream,
                Path.GetExtension(file.FileName));
            return Ok(response);
        }
        catch (InvalidDataException exception)
        {
            var isConflict = exception.Data["IsConflict"] is true;
            var errors = exception.Data["Errors"] as IReadOnlyList<ProductImportErrorResponse> ?? [];
            var statusCode = isConflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = isConflict ? "Sản phẩm bị trùng" : "File tải lên chưa hợp lệ",
                Detail = exception.Message
            };
            problemDetails.Extensions["errors"] = errors;

            return StatusCode(statusCode, problemDetails);
        }
    }
}

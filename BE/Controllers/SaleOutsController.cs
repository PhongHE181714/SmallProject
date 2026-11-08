using System.Globalization;
using BE.DTOs.SaleOuts;
using BE.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace BE.Controllers;

[ApiController]
[Route("api/sale-outs")]
public sealed class SaleOutsController : ControllerBase
{
    private readonly ISaleOutService _saleOutService;

    public SaleOutsController(ISaleOutService saleOutService)
    {
        _saleOutService = saleOutService;
    }

    [HttpPost]
    [ProducesResponseType(typeof(SaleOutCreateResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateSaleOutRequest request)
    {
        try
        {
            var response = await _saleOutService.CreateAsync(request);
            return StatusCode(StatusCodes.Status201Created, response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thông tin đơn xuất hàng chưa hợp lệ",
                Detail = exception.Message
            });
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Đơn xuất hàng đã tồn tại",
                Detail = exception.Message
            });
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
    }

    [HttpGet]
    [ProducesResponseType(typeof(SaleOutSearchResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Search([FromQuery] SaleOutSearchRequest request)
    {
        try
        {
            var response = await _saleOutService.SearchAsync(request);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thông tin tìm kiếm Sale Out không hợp lệ",
                Detail = exception.Message
            });
        }
    }

    [HttpGet("print-options")]
    [ProducesResponseType(typeof(SaleOutPrintOptionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SearchPrintOptions([FromQuery] SaleOutPrintOptionsRequest request)
    {
        try
        {
            var response = await _saleOutService.SearchPrintOptionsAsync(request);
            return Ok(response);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thông tin tìm phiếu in không hợp lệ",
                Detail = exception.Message
            });
        }
    }

    [HttpGet("print/{saleOutNo}/pdf")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadPrintPdf(string saleOutNo)
    {
        try
        {
            var pdf = await _saleOutService.GeneratePrintPdfAsync(saleOutNo);
            if (pdf is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Không tìm thấy phiếu xuất hàng đang hoạt động"
                });
            }

            var safeFileName = string.Concat(saleOutNo.Where(character => char.IsLetterOrDigit(character) || character is '-' or '_'));
            return File(pdf, "application/pdf", $"phieu-xuat-hang-{safeFileName}.pdf");
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Mã số phiếu không hợp lệ",
                Detail = exception.Message
            });
        }
    }

    [HttpGet("revenue-report")]
    [Produces("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadRevenueReport(
        [FromQuery] string? timeBegin,
        [FromQuery] string? timeEnd)
    {
        if (!TryParseRevenueReportDate(timeBegin, out var parsedTimeBegin)
            || !TryParseRevenueReportDate(timeEnd, out var parsedTimeEnd))
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Khoảng ngày báo cáo chưa hợp lệ",
                Detail = "Vui lòng truyền đủ ngày theo định dạng dd/MM/yyyy hoặc yyyy-MM-dd."
            });
        }

        try
        {
            var fileBytes = await _saleOutService.GenerateRevenueReportAsync(parsedTimeBegin, parsedTimeEnd);
            if (fileBytes is null)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Không có dữ liệu báo cáo doanh thu",
                    Detail = "Không tìm thấy bản ghi xuất hàng trong khoảng thời gian đã chọn."
                });
            }

            var fileName = $"bao-cao-doanh-thu-{parsedTimeBegin:yyyyMMdd}-{parsedTimeEnd:yyyyMMdd}.xlsx";
            return File(
                fileBytes,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                fileName);
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Khoảng ngày báo cáo chưa hợp lệ",
                Detail = exception.Message
            });
        }
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

    [HttpGet("order/{saleOutNo}")]
    [ProducesResponseType(typeof(IReadOnlyList<SaleOutListItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(string saleOutNo)
    {
        var response = await _saleOutService.GetOrderAsync(saleOutNo);
        if (response.Count == 0)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy đơn xuất hàng"
            });
        }

        return Ok(response);
    }

    [HttpPut("order/{saleOutNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> UpdateOrder(string saleOutNo, [FromBody] UpdateSaleOutOrderRequest request)
    {
        try
        {
            var updated = await _saleOutService.UpdateOrderAsync(saleOutNo, request);
            if (!updated)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Không tìm thấy đơn xuất hàng"
                });
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Danh sách sản phẩm chưa hợp lệ",
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
        catch (KeyNotFoundException exception)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy sản phẩm",
                Detail = exception.Message
            });
        }
    }

    [HttpDelete("order/{saleOutNo}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteOrder(string saleOutNo)
    {
        var deleted = await _saleOutService.DeleteOrderAsync(saleOutNo);
        if (!deleted)
        {
            return NotFound(new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = "Không tìm thấy đơn xuất hàng đang hoạt động"
            });
        }

        return NoContent();
    }

    [HttpGet("template")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult DownloadTemplate()
    {
        const string contentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
        var fileBytes = _saleOutService.GenerateImportTemplate();
        return File(fileBytes, contentType, "sale-out-template.xlsx");
    }

    [HttpPost("import")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(SaleOutImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [RequestFormLimits(MultipartBodyLengthLimit = SaleOutImportLimits.MAX_REQUEST_SIZE_BYTES)]
    [ProducesResponseType(typeof(SaleOutImportResponse), StatusCodes.Status200OK)]
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

        if (file.Length > SaleOutImportLimits.MAX_FILE_SIZE_BYTES)
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
            var response = await _saleOutService.ImportAsync(fileStream, Path.GetExtension(file.FileName));
            return Ok(response);
        }
        catch (InvalidDataException exception)
        {
            var isConflict = exception.Data["IsConflict"] is true;
            var errors = exception.Data["Errors"] as IReadOnlyList<SaleOutImportErrorResponse> ?? [];
            var statusCode = isConflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = isConflict ? "Thông tin đơn hàng không khớp hoặc sản phẩm đã tồn tại" : "File tải lên chưa hợp lệ",
                Detail = exception.Message
            };
            problemDetails.Extensions["errors"] = errors;
            return StatusCode(statusCode, problemDetails);
        }
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateSaleOutRequest request)
    {
        try
        {
            var updated = await _saleOutService.UpdateAsync(id, request);
            if (!updated)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Không tìm thấy đơn xuất hàng"
                });
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Thông tin đơn xuất hàng chưa hợp lệ",
                Detail = exception.Message
            });
        }
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id)
    {
        try
        {
            var deleted = await _saleOutService.DeleteAsync(id);
            if (!deleted)
            {
                return NotFound(new ProblemDetails
                {
                    Status = StatusCodes.Status404NotFound,
                    Title = "Không tìm thấy đơn xuất hàng đang hoạt động"
                });
            }

            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Mã đơn xuất hàng chưa hợp lệ",
                Detail = exception.Message
            });
        }
    }
}

using System.Text.Json;
using BE.BuildingBlocks.SharedKernel;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace BE.BuildingBlocks.Middlewares;

public sealed class ApiExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ApiExceptionHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (PayloadTooLargeException exception)
        {
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status413PayloadTooLarge,
                Title = exception.Title,
                Detail = exception.Message
            });
        }
        catch (InvalidDataException exception)
        {
            var isConflict = exception.Data["IsConflict"] is true;
            var statusCode = isConflict
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;
            var defaultTitle = isConflict ? "Dữ liệu bị trùng" : "File tải lên chưa hợp lệ";
            var title = exception.Data["ProblemTitle"] as string ?? defaultTitle;

            var problemDetails = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = exception.Message
            };

            if (exception.Data["Errors"] is { } errors)
            {
                problemDetails.Extensions["errors"] = errors;
            }

            await WriteProblemDetailsAsync(context, problemDetails);
        }
        catch (ArgumentException exception)
        {
            var title = exception.Data["ProblemTitle"] as string ?? "Dữ liệu gửi lên chưa hợp lệ";
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status400BadRequest,
                Title = title,
                Detail = exception.Message
            });
        }
        catch (KeyNotFoundException exception)
        {
            var title = exception.Data["ProblemTitle"] as string ?? "Không tìm thấy dữ liệu";
            var detail = string.IsNullOrWhiteSpace(exception.Message) ? null : exception.Message;
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status404NotFound,
                Title = title,
                Detail = detail
            });
        }
        catch (InvalidOperationException exception)
        {
            var title = exception.Data["ProblemTitle"] as string ?? "Dữ liệu bị xung đột";
            await WriteProblemDetailsAsync(context, new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = title,
                Detail = exception.Message
            });
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, ProblemDetails problemDetails)
    {
        context.Response.StatusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        context.Response.ContentType = "application/problem+json; charset=utf-8";
        await JsonSerializer.SerializeAsync(context.Response.Body, problemDetails);
    }
}

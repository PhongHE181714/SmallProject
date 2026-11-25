using System.Net.Http.Headers;
using System.Text.Json;
using BE.ApiGateway.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace BE.ApiGateway.Middleware;

public sealed class ApiGatewayProxyMiddleware
{
    private static readonly HashSet<string> HOP_BY_HOP_HEADERS = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection",
        "Keep-Alive",
        "Proxy-Authenticate",
        "Proxy-Authorization",
        "TE",
        "Trailer",
        "Transfer-Encoding",
        "Upgrade",
        "Host"
    };

    private readonly RequestDelegate _next;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly GatewayOptions _gatewayOptions;
    private readonly ILogger<ApiGatewayProxyMiddleware> _logger;

    public ApiGatewayProxyMiddleware(
        RequestDelegate next,
        IHttpClientFactory httpClientFactory,
        IOptions<GatewayOptions> gatewayOptions,
        ILogger<ApiGatewayProxyMiddleware> logger)
    {
        _next = next;
        _httpClientFactory = httpClientFactory;
        _gatewayOptions = gatewayOptions.Value;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var requestPath = context.Request.Path.Value ?? string.Empty;
        var matchedRoute = _gatewayOptions.Routes.FirstOrDefault(route =>
            requestPath.StartsWith(route.PathPrefix, StringComparison.OrdinalIgnoreCase));

        if (matchedRoute is null)
        {
            await _next(context);
            return;
        }

        var targetUrl = $"{matchedRoute.DownstreamBaseUrl.TrimEnd('/')}{requestPath}{context.Request.QueryString}";
        _logger.LogInformation(
            "[API Gateway] Điều phối {Method} {Path} -> {ServiceName} ({TargetUrl})",
            context.Request.Method,
            requestPath,
            matchedRoute.ServiceName,
            targetUrl);

        using var targetRequest = CreateProxyHttpRequest(context, targetUrl);
        using var httpClient = _httpClientFactory.CreateClient("GatewayProxyClient");

        try
        {
            using var responseMessage = await httpClient.SendAsync(
                targetRequest,
                HttpCompletionOption.ResponseHeadersRead,
                context.RequestAborted);

            await CopyProxyHttpResponseAsync(context, responseMessage);
        }
        catch (HttpRequestException exception)
        {
            _logger.LogError(
                exception,
                "[API Gateway] Không thể kết nối tới {ServiceName} tại {DownstreamBaseUrl}.",
                matchedRoute.ServiceName,
                matchedRoute.DownstreamBaseUrl);

            context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
            context.Response.ContentType = "application/problem+json; charset=utf-8";
            await JsonSerializer.SerializeAsync(context.Response.Body, new ProblemDetails
            {
                Status = StatusCodes.Status503ServiceUnavailable,
                Title = $"Microservice {matchedRoute.ServiceName} chưa sẵn sàng",
                Detail = $"API Gateway không thể kết nối tới {matchedRoute.ServiceName} tại {matchedRoute.DownstreamBaseUrl}. Hãy đảm bảo service này đang chạy."
            });
        }
    }

    private static HttpRequestMessage CreateProxyHttpRequest(HttpContext context, string targetUrl)
    {
        var requestMessage = new HttpRequestMessage(
            new HttpMethod(context.Request.Method),
            new Uri(targetUrl));

        if (HasRequestBody(context.Request.Method)
            && (context.Request.ContentLength > 0 || context.Request.Headers.ContainsKey("Transfer-Encoding")))
        {
            requestMessage.Content = new StreamContent(context.Request.Body);
            if (!string.IsNullOrWhiteSpace(context.Request.ContentType))
            {
                requestMessage.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(context.Request.ContentType);
            }
        }

        foreach (var header in context.Request.Headers)
        {
            if (HOP_BY_HOP_HEADERS.Contains(header.Key))
            {
                continue;
            }

            if (!requestMessage.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()))
            {
                requestMessage.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        return requestMessage;
    }

    private static async Task CopyProxyHttpResponseAsync(
        HttpContext context,
        HttpResponseMessage responseMessage)
    {
        context.Response.StatusCode = (int)responseMessage.StatusCode;

        foreach (var header in responseMessage.Headers)
        {
            if (!HOP_BY_HOP_HEADERS.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        foreach (var header in responseMessage.Content.Headers)
        {
            if (!HOP_BY_HOP_HEADERS.Contains(header.Key))
            {
                context.Response.Headers[header.Key] = header.Value.ToArray();
            }
        }

        context.Response.Headers.Remove("transfer-encoding");
        await responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
    }

    private static bool HasRequestBody(string method)
    {
        return !HttpMethods.IsGet(method)
            && !HttpMethods.IsHead(method)
            && !HttpMethods.IsDelete(method)
            && !HttpMethods.IsTrace(method);
    }
}

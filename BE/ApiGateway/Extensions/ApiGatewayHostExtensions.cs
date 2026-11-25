using BE.ApiGateway.Configuration;
using BE.ApiGateway.Middleware;
using Microsoft.Extensions.Options;

namespace BE.ApiGateway.Extensions;

public static class ApiGatewayHostExtensions
{
    private const long MAX_PROXY_BODY_SIZE_BYTES = 11 * 1024 * 1024;

    public static IServiceCollection AddApiGateway(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<GatewayOptions>(
            configuration.GetSection(GatewayOptions.SECTION_NAME));
        services.AddHttpClient("GatewayProxyClient", client =>
        {
            client.Timeout = TimeSpan.FromMinutes(2);
        });
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static WebApplication UseApiGateway(this WebApplication app)
    {
        app.Use(async (context, next) =>
        {
            var maxRequestBodySizeFeature = context.Features.Get<Microsoft.AspNetCore.Http.Features.IHttpMaxRequestBodySizeFeature>();
            if (maxRequestBodySizeFeature is { IsReadOnly: false })
            {
                maxRequestBodySizeFeature.MaxRequestBodySize = MAX_PROXY_BODY_SIZE_BYTES;
            }

            await next();
        });

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.MapGet("/api/gateway/routes", (IOptions<GatewayOptions> options) => Results.Ok(options.Value.Routes));
        app.UseMiddleware<ApiGatewayProxyMiddleware>();

        return app;
    }
}

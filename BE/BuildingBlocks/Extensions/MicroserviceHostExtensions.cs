using System.Reflection;
using BE.BuildingBlocks.CQRS;
using BE.BuildingBlocks.EventBus.Abstractions;
using BE.BuildingBlocks.EventBus.RabbitMQ;
using BE.BuildingBlocks.Middlewares;
using BE.Data;
using BE.Data.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BE.BuildingBlocks.Extensions;

public static class MicroserviceHostExtensions
{
    private static readonly Type[] HANDLER_OPEN_GENERIC_TYPES =
    [
        typeof(ICommandHandler<,>),
        typeof(IQueryHandler<,>),
        typeof(IIntegrationEventHandler<>)
    ];

    public static IServiceCollection AddMicroserviceHost(
        this IServiceCollection services,
        IConfiguration configuration,
        params Assembly[] assemblies)
    {
        services.AddControllers()
            .AddApplicationPart(typeof(EventBusConsumerController).Assembly);
        services.ConfigureInvalidModelStateProblemDetails();
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();
        services.AddHttpClient();

        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();
        services.AddScoped<ISender, Mediator>();

        services.Configure<RabbitMqOptions>(
            configuration.GetSection(RabbitMqOptions.SECTION_NAME));
        services.AddSingleton<RabbitMqEventBus>();
        services.AddSingleton<IEventBus>(provider => provider.GetRequiredService<RabbitMqEventBus>());
        services.AddHostedService<RabbitMqBackgroundConsumer>();

        foreach (var assembly in assemblies.Distinct())
        {
            services.RegisterAssemblyHandlersAndServices(assembly);
        }

        return services;
    }

    public static WebApplication UseMicroserviceHost(this WebApplication app)
    {
        app.UseMiddleware<ApiExceptionHandlingMiddleware>();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        app.UseAuthorization();
        app.MapControllers();

        return app;
    }

    public static IServiceCollection RegisterAssemblyHandlersAndServices(
        this IServiceCollection services,
        Assembly assembly)
    {
        var concreteTypes = assembly
            .GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && !typeof(IHostedService).IsAssignableFrom(type))
            .ToArray();

        foreach (var implementationType in concreteTypes)
        {
            var interfaces = implementationType.GetInterfaces();

            foreach (var interfaceType in interfaces)
            {
                if (interfaceType.IsGenericType
                    && HANDLER_OPEN_GENERIC_TYPES.Contains(interfaceType.GetGenericTypeDefinition()))
                {
                    services.AddScoped(interfaceType, implementationType);
                }
            }

            if (!implementationType.Name.EndsWith("Repository", StringComparison.Ordinal)
                && !implementationType.Name.EndsWith("Service", StringComparison.Ordinal))
            {
                continue;
            }

            var matchingInterface = interfaces.FirstOrDefault(
                interfaceType => interfaceType.Name == $"I{implementationType.Name}");
            if (matchingInterface is null)
            {
                continue;
            }

            if (implementationType.Name.Contains("SnapshotRepository", StringComparison.Ordinal))
            {
                services.AddSingleton(matchingInterface, implementationType);
            }
            else
            {
                services.AddScoped(matchingInterface, implementationType);
            }
        }

        return services;
    }

    private static void ConfigureInvalidModelStateProblemDetails(this IServiceCollection services)
    {
        services.Configure<ApiBehaviorOptions>(options =>
        {
            options.InvalidModelStateResponseFactory = context =>
            {
                var invalidFields = context.ModelState
                    .Where(entry => entry.Value?.Errors.Count > 0)
                    .Select(entry => entry.Key)
                    .ToArray();

                var messages = new List<string>();
                if (invalidFields.Any(field => field.Contains("quantityPerBox", StringComparison.OrdinalIgnoreCase)))
                {
                    messages.Add("Số lượng/thùng phải là số nguyên không âm, ví dụ 24.");
                }

                if (invalidFields.Any(field => field.Contains("productWeight", StringComparison.OrdinalIgnoreCase)))
                {
                    messages.Add("Trọng lượng phải là số hợp lệ; có thể nhập số thập phân như 0.6.");
                }

                if (messages.Count == 0)
                {
                    messages.Add(invalidFields.Any(field => field.Equals("request", StringComparison.OrdinalIgnoreCase))
                        ? "Vui lòng gửi thông tin sản phẩm bằng JSON hợp lệ."
                        : "Một hoặc nhiều trường có định dạng không hợp lệ. Hãy kiểm tra dữ liệu rồi thử lại.");
                }

                return new BadRequestObjectResult(new ProblemDetails
                {
                    Status = StatusCodes.Status400BadRequest,
                    Title = "Dữ liệu gửi lên chưa hợp lệ",
                    Detail = string.Join(" ", messages.Distinct())
                });
            };
        });
    }
}

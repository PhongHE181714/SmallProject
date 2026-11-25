using System.Text.Json;
using BE.BuildingBlocks.EventBus.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BE.BuildingBlocks.EventBus.RabbitMQ;

public sealed class RabbitMqBackgroundConsumer : BackgroundService
{
    private readonly RabbitMqEventBus _eventBus;
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly RabbitMqOptions _options;
    private readonly ILogger<RabbitMqBackgroundConsumer> _logger;

    public RabbitMqBackgroundConsumer(
        RabbitMqEventBus eventBus,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<RabbitMqOptions> options,
        ILogger<RabbitMqBackgroundConsumer> logger)
    {
        _eventBus = eventBus;
        _serviceScopeFactory = serviceScopeFactory;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation(
            "[RabbitMQ Consumer] Đang lắng nghe Queue '{QueueName}' gắn với Exchange '{ExchangeName}' ({Host}:{Port}).",
            _options.OrderingQueueName,
            _options.ExchangeName,
            _options.HostName,
            _options.Port);

        await foreach (var envelope in _eventBus.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                await DispatchEnvelopeAsync(envelope, stoppingToken);
            }
            catch (Exception exception)
            {
                _logger.LogError(
                    exception,
                    "[RabbitMQ Consumer] Lỗi khi xử lý message EventId={EventId}, RoutingKey={RoutingKey}.",
                    envelope.EventId,
                    envelope.RoutingKey);
            }
        }
    }

    private async Task DispatchEnvelopeAsync(RabbitMqEnvelope envelope, CancellationToken cancellationToken)
    {
        var eventType = Type.GetType(envelope.EventTypeName);
        if (eventType is null)
        {
            return;
        }

        var integrationEvent = JsonSerializer.Deserialize(envelope.PayloadJson, eventType);
        if (integrationEvent is null)
        {
            return;
        }

        await using var scope = _serviceScopeFactory.CreateAsyncScope();
        var handlerInterfaceType = typeof(IIntegrationEventHandler<>).MakeGenericType(eventType);
        var handlers = scope.ServiceProvider.GetServices(handlerInterfaceType);
        var handleMethod = handlerInterfaceType.GetMethod(nameof(IIntegrationEventHandler<IntegrationEvent>.HandleAsync));

        if (handleMethod is null)
        {
            return;
        }

        foreach (var handler in handlers)
        {
            if (handler is null)
            {
                continue;
            }

            var task = (Task?)handleMethod.Invoke(handler, [integrationEvent, cancellationToken]);
            if (task is not null)
            {
                await task;
            }
        }
    }
}

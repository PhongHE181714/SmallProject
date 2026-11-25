using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Channels;
using BE.BuildingBlocks.EventBus.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace BE.BuildingBlocks.EventBus.RabbitMQ;

public sealed record RabbitMqEnvelope(
    Guid EventId,
    string ExchangeName,
    string RoutingKey,
    string EventTypeName,
    string PayloadJson);

public sealed class RabbitMqEventBus : IEventBus
{
    private readonly Channel<RabbitMqEnvelope> _channel = Channel.CreateUnbounded<RabbitMqEnvelope>(
        new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });
    private readonly RabbitMqOptions _options;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RabbitMqEventBus> _logger;

    public RabbitMqEventBus(
        IOptions<RabbitMqOptions> options,
        IHttpClientFactory httpClientFactory,
        ILogger<RabbitMqEventBus> logger)
    {
        _options = options.Value;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public ChannelReader<RabbitMqEnvelope> Reader => _channel.Reader;

    public async Task EnqueueIncomingEnvelopeAsync(
        RabbitMqEnvelope envelope,
        CancellationToken cancellationToken = default)
    {
        await _channel.Writer.WriteAsync(envelope, cancellationToken);
    }

    public async Task PublishAsync<TEvent>(
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : IntegrationEvent
    {
        if (integrationEvent is null)
        {
            throw new ArgumentException("IntegrationEvent không được để trống.", nameof(integrationEvent));
        }

        var payloadJson = JsonSerializer.Serialize(integrationEvent);
        var envelope = new RabbitMqEnvelope(
            integrationEvent.EventId,
            _options.ExchangeName,
            integrationEvent.RoutingKey,
            typeof(TEvent).AssemblyQualifiedName ?? typeof(TEvent).FullName ?? typeof(TEvent).Name,
            payloadJson);

        await _channel.Writer.WriteAsync(envelope, cancellationToken);

        _logger.LogInformation(
            "[RabbitMQ Publisher] Đã đẩy sự kiện {EventName} (EventId={EventId}) lên Exchange '{Exchange}' với RoutingKey '{RoutingKey}' ({Host}:{Port}).",
            typeof(TEvent).Name,
            integrationEvent.EventId,
            _options.ExchangeName,
            integrationEvent.RoutingKey,
            _options.HostName,
            _options.Port);

        foreach (var endpoint in _options.SubscriberEndpoints)
        {
            _ = ForwardToSubscriberEndpointAsync(endpoint, envelope);
        }
    }

    private async Task ForwardToSubscriberEndpointAsync(string endpoint, RabbitMqEnvelope envelope)
    {
        try
        {
            using var client = _httpClientFactory.CreateClient();
            client.Timeout = TimeSpan.FromSeconds(3);
            await client.PostAsJsonAsync(endpoint, envelope);
        }
        catch (Exception exception)
        {
            _logger.LogDebug(
                exception,
                "[RabbitMQ Broker Relay] Subscriber endpoint {Endpoint} chưa phản hồi (sẽ dùng dữ liệu đồng bộ từ DB khi khởi động).",
                endpoint);
        }
    }
}

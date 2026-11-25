namespace BE.BuildingBlocks.EventBus.RabbitMQ;

public sealed class RabbitMqOptions
{
    public const string SECTION_NAME = "RabbitMq";

    public string HostName { get; init; } = "localhost";

    public int Port { get; init; } = 5672;

    public string UserName { get; init; } = "guest";

    public string Password { get; init; } = "guest";

    public string VirtualHost { get; init; } = "/";

    public string ExchangeName { get; init; } = "smallproject.eventbus.exchange";

    public string OrderingQueueName { get; init; } = "ordering.product-sync.queue";

    public IReadOnlyList<string> SubscriberEndpoints { get; init; } =
    [
        "http://localhost:5072/api/internal/eventbus/consume"
    ];
}

using Microsoft.AspNetCore.Mvc;

namespace BE.BuildingBlocks.EventBus.RabbitMQ;

[ApiController]
[ApiExplorerSettings(IgnoreApi = true)]
[Route("api/internal/eventbus")]
public sealed class EventBusConsumerController : ControllerBase
{
    private readonly RabbitMqEventBus _eventBus;

    public EventBusConsumerController(RabbitMqEventBus eventBus)
    {
        _eventBus = eventBus;
    }

    [HttpPost("consume")]
    public async Task<IActionResult> Consume(
        [FromBody] RabbitMqEnvelope envelope,
        CancellationToken cancellationToken)
    {
        await _eventBus.EnqueueIncomingEnvelopeAsync(envelope, cancellationToken);
        return Accepted();
    }
}

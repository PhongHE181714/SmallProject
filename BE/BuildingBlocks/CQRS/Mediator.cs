using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BE.BuildingBlocks.CQRS;

public sealed class Mediator : ISender
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<Mediator> _logger;

    public Mediator(IServiceProvider serviceProvider, ILogger<Mediator> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task<TResponse> SendCommandAsync<TCommand, TResponse>(
        TCommand command,
        CancellationToken cancellationToken = default)
        where TCommand : ICommand<TResponse>
    {
        if (command is null)
        {
            throw new ArgumentException("Command không được để trống.", nameof(command));
        }

        var handler = _serviceProvider.GetRequiredService<ICommandHandler<TCommand, TResponse>>();
        _logger.LogDebug("Đang thực thi Command {CommandName}", typeof(TCommand).Name);
        return await handler.HandleAsync(command, cancellationToken);
    }

    public async Task<TResponse> SendQueryAsync<TQuery, TResponse>(
        TQuery query,
        CancellationToken cancellationToken = default)
        where TQuery : IQuery<TResponse>
    {
        if (query is null)
        {
            throw new ArgumentException("Query không được để trống.", nameof(query));
        }

        var handler = _serviceProvider.GetRequiredService<IQueryHandler<TQuery, TResponse>>();
        _logger.LogDebug("Đang thực thi Query {QueryName}", typeof(TQuery).Name);
        return await handler.HandleAsync(query, cancellationToken);
    }
}

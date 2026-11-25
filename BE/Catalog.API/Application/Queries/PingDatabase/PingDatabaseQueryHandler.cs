using BE.BuildingBlocks.CQRS;
using BE.Services.CatalogService.Application.DTOs;
using BE.Services.CatalogService.Domain.Repositories;

namespace BE.Services.CatalogService.Application.Queries.PingDatabase;

public sealed class PingDatabaseQueryHandler : IQueryHandler<PingDatabaseQuery, DatabasePingResponse>
{
    private readonly IDatabaseRepository _databaseRepository;

    public PingDatabaseQueryHandler(IDatabaseRepository databaseRepository)
    {
        _databaseRepository = databaseRepository;
    }

    public async Task<DatabasePingResponse> HandleAsync(
        PingDatabaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var databaseName = await _databaseRepository.GetDatabaseNameAsync();

        return new DatabasePingResponse
        {
            Connected = true,
            Database = databaseName
        };
    }
}

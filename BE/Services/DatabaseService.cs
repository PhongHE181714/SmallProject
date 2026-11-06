using BE.DTOs.Database;
using BE.Repositories.Interfaces;
using BE.Services.Interfaces;

namespace BE.Services;

public sealed class DatabaseService : IDatabaseService
{
    private readonly IDatabaseRepository _databaseRepository;

    public DatabaseService(IDatabaseRepository databaseRepository)
    {
        _databaseRepository = databaseRepository;
    }

    public async Task<DatabasePingResponse> PingAsync()
    {
        var databaseName = await _databaseRepository.GetDatabaseNameAsync();

        return new DatabasePingResponse
        {
            Connected = true,
            Database = databaseName
        };
    }
}

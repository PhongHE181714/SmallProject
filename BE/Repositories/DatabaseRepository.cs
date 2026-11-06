using BE.Data;
using BE.Data.Interfaces;
using BE.Repositories.Interfaces;
using Dapper;

namespace BE.Repositories;

public sealed class DatabaseRepository : IDatabaseRepository
{
    private const string GET_DATABASE_NAME_SQL = "SELECT DB_NAME();";

    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public DatabaseRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public async Task<string> GetDatabaseNameAsync()
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        var databaseName = await connection.ExecuteScalarAsync<string>(GET_DATABASE_NAME_SQL);

        return databaseName
            ?? throw new InvalidOperationException("SQL Server không trả về tên database.");
    }
}

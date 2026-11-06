using Microsoft.Data.SqlClient;
using BE.Data.Interfaces;

namespace BE.Data;

public sealed class SqlConnectionFactory : ISqlConnectionFactory
{
    private const string CONNECTION_NAME = "DefaultConnection";

    private readonly string _connectionString;

    public SqlConnectionFactory(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString(CONNECTION_NAME)
            ?? throw new InvalidOperationException(
                $"ConnectionStrings:{CONNECTION_NAME} chưa được cấu hình.");
    }

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}

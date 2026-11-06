using Microsoft.Data.SqlClient;

namespace BE.Data.Interfaces;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}

using BE.Data.Interfaces;
using BE.Repositories.Interfaces;

namespace BE.Repositories.SaleOuts;

public sealed partial class SaleOutRepository : ISaleOutRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public SaleOutRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }
}


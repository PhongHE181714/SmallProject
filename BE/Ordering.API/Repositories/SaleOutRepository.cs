using BE.Data.Interfaces;
using BE.Services.OrderingService.Domain.Repositories;

namespace BE.Services.OrderingService.Repositories;

public sealed partial class SaleOutRepository : ISaleOutRepository
{
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public SaleOutRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }
}

using System.Collections.Concurrent;
using BE.Data.Interfaces;
using BE.Services.OrderingService.Domain.ProductReplica;
using Dapper;

namespace BE.Services.OrderingService.Repositories;

public sealed class ProductSnapshotRepository : IProductSnapshotRepository
{
    private const int PRODUCT_CODE_QUERY_BATCH_SIZE = 1000;

    private readonly ConcurrentDictionary<Guid, ProductSnapshot> _snapshotsById = new();
    private readonly ISqlConnectionFactory _sqlConnectionFactory;

    public ProductSnapshotRepository(ISqlConnectionFactory sqlConnectionFactory)
    {
        _sqlConnectionFactory = sqlConnectionFactory;
    }

    public void UpsertSnapshot(ProductSnapshot snapshot)
    {
        _snapshotsById[snapshot.Id] = snapshot;
    }

    public void RemoveSnapshot(Guid productId)
    {
        _snapshotsById.TryRemove(productId, out _);
    }

    public async Task<ProductSnapshot?> FindActiveByIdAsync(Guid productId)
    {
        if (_snapshotsById.TryGetValue(productId, out var cachedSnapshot))
        {
            return cachedSnapshot.IsActive ? cachedSnapshot : null;
        }

        const string sql = """
            SELECT Id, ProductCode, ProductName, Unit, QuantityPerBox, IsActive
            FROM dbo.MasterProduct
            WHERE Id = @ProductId AND IsActive = 1;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        var snapshot = await connection.QuerySingleOrDefaultAsync<ProductSnapshot>(
            sql,
            new { ProductId = productId });

        if (snapshot is not null)
        {
            _snapshotsById[snapshot.Id] = snapshot;
        }

        return snapshot;
    }

    public async Task<IReadOnlyList<ProductSnapshot>> FindActiveByCodesAsync(
        IReadOnlyCollection<string> productCodes)
    {
        if (productCodes.Count == 0)
        {
            return Array.Empty<ProductSnapshot>();
        }

        const string sql = """
            SELECT Id, ProductCode, ProductName, Unit, QuantityPerBox, IsActive
            FROM dbo.MasterProduct
            WHERE IsActive = 1 AND ProductCode IN @ProductCodes;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        var products = new List<ProductSnapshot>();
        foreach (var productCodeBatch in productCodes.Chunk(PRODUCT_CODE_QUERY_BATCH_SIZE))
        {
            var batchProducts = await connection.QueryAsync<ProductSnapshot>(
                sql,
                new { ProductCodes = productCodeBatch });
            foreach (var product in batchProducts)
            {
                _snapshotsById[product.Id] = product;
                products.Add(product);
            }
        }

        return products;
    }
}

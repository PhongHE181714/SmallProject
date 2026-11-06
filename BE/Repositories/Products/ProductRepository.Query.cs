using BE.Data;
using BE.Models.Common;
using BE.Models.MasterProduct;
using Dapper;

namespace BE.Repositories.Products;

public sealed partial class ProductRepository
{
    private const int PRODUCT_CODE_QUERY_BATCH_SIZE = 1000;

    public async Task<IReadOnlyList<MasterProduct>> FindActiveByCodesAsync(
        IReadOnlyCollection<string> productCodes)
    {
        const string sql = """
            SELECT Id, ProductCode, ProductName, Unit, Specification,
                   QuantityPerBox, ProductWeight, IsActive
            FROM dbo.MasterProduct
            WHERE IsActive = 1 AND ProductCode IN @ProductCodes;
            """;

        if (productCodes.Count == 0)
        {
            return Array.Empty<MasterProduct>();
        }

        await using var connection = _sqlConnectionFactory.CreateConnection();
        var products = new List<MasterProduct>();
        foreach (var productCodeBatch in productCodes.Chunk(PRODUCT_CODE_QUERY_BATCH_SIZE))
        {
            var batchProducts = await connection.QueryAsync<MasterProduct>(
                sql,
                new { ProductCodes = productCodeBatch });
            products.AddRange(batchProducts);
        }

        return products;
    }

    public async Task<MasterProduct?> FindActiveByIdAsync(Guid productId)
    {
        const string sql = """
            SELECT Id, ProductCode, ProductName, Unit, Specification,
                   QuantityPerBox, ProductWeight, IsActive
            FROM dbo.MasterProduct
            WHERE Id = @ProductId AND IsActive = 1;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        return await connection.QuerySingleOrDefaultAsync<MasterProduct>(sql, new { ProductId = productId });
    }

    public async Task<PagedResult<MasterProduct>> SearchAsync(ProductSearchCriteria criteria)
    {
        var whereClause = BuildWhereClause(criteria.SearchField);
        var sql = $"""
            SELECT COUNT_BIG(1)
            FROM dbo.MasterProduct
            {whereClause};

            SELECT Id, ProductCode, ProductName, Unit, Specification, QuantityPerBox, ProductWeight, IsActive
            FROM dbo.MasterProduct
            {whereClause}
            ORDER BY ProductCode ASC, Id ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        var parameters = new DynamicParameters();
        parameters.Add("Offset", criteria.Offset);
        parameters.Add("PageSize", criteria.PageSize);

        if (criteria.SearchText is not null)
        {
            parameters.Add("SearchPattern", $"%{SqlLikePattern.Escape(criteria.SearchText)}%");
        }

        if (criteria.SearchNumber is not null)
        {
            parameters.Add("SearchNumber", criteria.SearchNumber);
        }

        await using var connection = _sqlConnectionFactory.CreateConnection();
        using var results = await connection.QueryMultipleAsync(sql, parameters);
        var totalItems = await results.ReadSingleAsync<long>();
        var items = (await results.ReadAsync<MasterProduct>()).AsList();

        return new PagedResult<MasterProduct>
        {
            Items = items,
            TotalItems = totalItems
        };
    }

    private static string BuildWhereClause(ProductSearchField? searchField)
    {
        return searchField switch
        {
            ProductSearchField.ProductCode => $"WHERE ProductCode LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            ProductSearchField.ProductName => $"WHERE ProductName LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            ProductSearchField.Unit => $"WHERE Unit LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            ProductSearchField.Specification => $"WHERE Specification LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            ProductSearchField.QuantityPerBox => "WHERE QuantityPerBox = @SearchNumber",
            ProductSearchField.ProductWeight => "WHERE ProductWeight = @SearchNumber",
            null => string.Empty,
            _ => throw new InvalidOperationException("Trường tìm kiếm sản phẩm chưa được hỗ trợ.")
        };
    }

}

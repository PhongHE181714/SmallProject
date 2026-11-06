using BE.Models.MasterProduct;
using BE.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BE.Repositories.Products;

public sealed partial class ProductRepository
{
    public async Task CreateAsync(MasterProduct product)
    {
        const string sql = """
            INSERT INTO dbo.MasterProduct
            (
                Id, ProductCode, ProductName, Unit, Specification,
                QuantityPerBox, ProductWeight, IsActive
            )
            VALUES
            (
                @Id, @ProductCode, @ProductName, @Unit, @Specification,
                @QuantityPerBox, @ProductWeight, @IsActive
            );
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        try
        {
            await connection.ExecuteAsync(sql, product);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            var duplicateFields = await FindDuplicateFieldsAsync(
                connection,
                product.ProductCode,
                product.ProductName);

            if (duplicateFields.Count > 0)
            {
                throw new InvalidOperationException("Mã hoặc tên sản phẩm đã tồn tại.");
            }

            throw;
        }
    }

    public async Task UpdateAsync(MasterProduct product)
    {
        const string sql = """
        UPDATE dbo.MasterProduct
        SET
            ProductCode = @ProductCode,
            ProductName = @ProductName,
            Unit = @Unit,
            Specification = @Specification,
            QuantityPerBox = @QuantityPerBox,
            ProductWeight = @ProductWeight,
            IsActive = @IsActive
        WHERE Id = @Id;
        """;
        await using var connection = _sqlConnectionFactory.CreateConnection();
        try
        {
            await connection.ExecuteAsync(sql,product);
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
           var duplicateFields = await FindDuplicateFieldsForUpdateAsync(
            product.Id,
            product.ProductCode,
            product.ProductName
           );
           if(duplicateFields.Count > 0)
            {
                throw new InvalidOperationException("Mã hoặc tên sản phẩm đã tồn tại.");
            }

            throw;
        }
    }

    public async Task<IReadOnlyList<string>> FindDuplicateFieldsForUpdateAsync( Guid currentId, string? productCode,string? productName )
    {
        const string sql = """
        SELECT N'ProductCode' AS FieldName
        WHERE @ProductCode IS NOT NULL
          AND EXISTS
          (
              SELECT 1
              FROM dbo.MasterProduct
              WHERE ProductCode = @ProductCode AND Id <> @CurrentId
          )
        UNION ALL
        SELECT N'ProductName' AS FieldName
        WHERE @ProductName IS NOT NULL
          AND EXISTS
          (
              SELECT 1
              FROM dbo.MasterProduct
              WHERE ProductName = @ProductName AND Id <> @CurrentId
          );
        """;
        await using var connection = _sqlConnectionFactory.CreateConnection();
        var duplicateFields = await connection.QueryAsync<string>(sql, new
        {
           CurrentId = currentId,
           ProductCode = productCode,
           ProductName = productName
        });
        return duplicateFields.AsList();
    }

    public async Task<IReadOnlyList<string>> FindDuplicateFieldsAsync(
        string? productCode,
        string? productName)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        return await FindDuplicateFieldsAsync(connection, productCode, productName);
    }

    public async Task<bool> ExistsAsync(Guid productId)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.MasterProduct
                WHERE Id = @ProductId
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { ProductId = productId });
    }

    public async Task<bool> HasRelatedTransactionsAsync(Guid productId)
    {
        const string sql = """
            SELECT CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.SaleOut
                WHERE ProductId = @ProductId
            ) THEN CAST(1 AS BIT) ELSE CAST(0 AS BIT) END;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        return await connection.ExecuteScalarAsync<bool>(sql, new { ProductId = productId });
    }

    public async Task<bool> DeleteAsync(Guid productId)
    {
        const string sql = """
            DELETE FROM dbo.MasterProduct
            WHERE Id = @ProductId;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        var rowsAffected = await connection.ExecuteAsync(sql, new { ProductId = productId });
        return rowsAffected > 0;
    }

    private static async Task<IReadOnlyList<string>> FindDuplicateFieldsAsync(
        System.Data.IDbConnection connection,
        string? productCode,
        string? productName)
    {
        const string sql = """
            SELECT N'ProductCode' AS FieldName
            WHERE @ProductCode IS NOT NULL
              AND EXISTS
              (
                  SELECT 1
                  FROM dbo.MasterProduct
                  WHERE ProductCode = @ProductCode
              )
            UNION ALL
            SELECT N'ProductName' AS FieldName
            WHERE @ProductName IS NOT NULL
              AND EXISTS
              (
                  SELECT 1
                  FROM dbo.MasterProduct
                  WHERE ProductName = @ProductName
              );
            """;

        var duplicateFields = await connection.QueryAsync<string>(sql, new
        {
            ProductCode = productCode,
            ProductName = productName
        });

        return duplicateFields.AsList();
    }

}

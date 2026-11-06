using BE.Models.MasterProduct;
using Dapper;
using Microsoft.Data.SqlClient;
using System.Data;

namespace BE.Repositories.Products;

public sealed partial class ProductRepository
{
    public async Task<IReadOnlyList<ProductImportDuplicate>> ImportAsync(
        IReadOnlyList<MasterProduct> products)
    {
        const string findDuplicatesSql = """
            SELECT N'ProductCode' AS FieldName, ProductCode AS Value
            FROM dbo.MasterProduct
            WHERE ProductCode IN @ProductCodes
            UNION ALL
            SELECT N'ProductName' AS FieldName, ProductName AS Value
            FROM dbo.MasterProduct
            WHERE ProductName IN @ProductNames;
            """;

        const string insertSql = """
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
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        var duplicates = (await connection.QueryAsync<ProductImportDuplicate>(
            findDuplicatesSql,
            new
            {
                ProductCodes = products.Select(product => product.ProductCode).ToArray(),
                ProductNames = products.Select(product => product.ProductName).ToArray()
            },
            transaction)).AsList();

        if (duplicates.Count > 0)
        {
            await transaction.RollbackAsync();
            return duplicates;
        }

        await connection.ExecuteAsync(insertSql, products, transaction);
        await transaction.CommitAsync();
        return Array.Empty<ProductImportDuplicate>();
    }

}

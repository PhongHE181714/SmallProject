using System.Data;
using System.Globalization;
using BE.Models.SaleOut;
using Dapper;
using Microsoft.Data.SqlClient;

namespace BE.Repositories.SaleOuts;

public sealed partial class SaleOutRepository
{
    private const int IMPORT_DUPLICATE_QUERY_BATCH_SIZE = 500;

    public async Task<IReadOnlyList<SaleOutImportDuplicate>> ImportAsync(
        IReadOnlyList<SaleOut> saleOuts)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        var conflicts = new List<SaleOutImportDuplicate>();
        foreach (var batch in saleOuts.Chunk(IMPORT_DUPLICATE_QUERY_BATCH_SIZE))
        {
            var valueRows = new List<string>(batch.Length);
            var parameters = new DynamicParameters();
            for (var index = 0; index < batch.Length; index++)
            {
                valueRows.Add($"(@CustomerPoNo{index}, @ProductId{index}, @OrderDate{index}, @CustomerName{index})");
                parameters.Add($"CustomerPoNo{index}", batch[index].CustomerPoNo);
                parameters.Add($"ProductId{index}", batch[index].ProductId);
                parameters.Add($"OrderDate{index}", int.Parse(batch[index].OrderDate!.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture));
                parameters.Add($"CustomerName{index}", batch[index].CustomerName);
            }

            var findDuplicatesSql = $"""
                SELECT incoming.CustomerPoNo,
                       incoming.ProductId,
                       CAST(MAX(CASE
                           WHEN saleOut.IsActive = 1
                               AND (ISNULL(saleOut.OrderDate, -1) <> incoming.OrderDate
                                   OR CONVERT(varbinary(400), ISNULL(saleOut.CustomerName, N''))
                                       <> CONVERT(varbinary(400), incoming.CustomerName))
                           THEN 1 ELSE 0 END) AS bit) AS IsOrderHeaderMismatch
                FROM (VALUES {string.Join(", ", valueRows)})
                    AS incoming(CustomerPoNo, ProductId, OrderDate, CustomerName)
                INNER JOIN dbo.SaleOut AS saleOut WITH (UPDLOCK, HOLDLOCK)
                    ON saleOut.CustomerPoNo = incoming.CustomerPoNo
                    AND (saleOut.ProductId = incoming.ProductId
                        OR (saleOut.IsActive = 1
                            AND (ISNULL(saleOut.OrderDate, -1) <> incoming.OrderDate
                                OR CONVERT(varbinary(400), ISNULL(saleOut.CustomerName, N''))
                                    <> CONVERT(varbinary(400), incoming.CustomerName))))
                GROUP BY incoming.CustomerPoNo, incoming.ProductId
                HAVING MAX(CASE
                    WHEN saleOut.ProductId = incoming.ProductId THEN 1
                    WHEN saleOut.IsActive = 1
                        AND (ISNULL(saleOut.OrderDate, -1) <> incoming.OrderDate
                            OR CONVERT(varbinary(400), ISNULL(saleOut.CustomerName, N''))
                                <> CONVERT(varbinary(400), incoming.CustomerName))
                    THEN 1 ELSE 0 END) = 1;
                """;
            var batchConflicts = await connection.QueryAsync<SaleOutImportDuplicate>(
                findDuplicatesSql,
                parameters,
                transaction);
            conflicts.AddRange(batchConflicts);
        }

        if (conflicts.Count > 0)
        {
            await transaction.RollbackAsync();
            return conflicts;
        }

        var data = new DataTable();
        data.Columns.Add("Id", typeof(Guid));
        data.Columns.Add("CustomerPoNo", typeof(string));
        data.Columns.Add("OrderDate", typeof(int));
        data.Columns.Add("CustomerName", typeof(string));
        data.Columns.Add("ProductId", typeof(Guid));
        data.Columns.Add("Quantity", typeof(decimal));
        data.Columns.Add("Price", typeof(decimal));
        data.Columns.Add("Amount", typeof(decimal));
        data.Columns.Add("QuantityPerBox", typeof(decimal));
        data.Columns.Add("BoxQuantity", typeof(decimal));
        data.Columns.Add("IsActive", typeof(bool));

        foreach (var saleOut in saleOuts)
        {
            data.Rows.Add(
                saleOut.Id,
                saleOut.CustomerPoNo,
                int.Parse(saleOut.OrderDate!.Value.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture),
                saleOut.CustomerName,
                saleOut.ProductId,
                saleOut.Quantity,
                saleOut.Price,
                saleOut.Amount,
                saleOut.QuantityPerBox,
                saleOut.BoxQuantity,
                saleOut.IsActive);
        }

        using (var bulkCopy = new SqlBulkCopy(
            connection,
            SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers,
            transaction))
        {
            bulkCopy.DestinationTableName = "dbo.SaleOut";
            foreach (DataColumn column in data.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(data);
        }

        await transaction.CommitAsync();
        return Array.Empty<SaleOutImportDuplicate>();
    }

    public async Task<bool> SoftDeleteAsync(Guid id)
    {
        const string sql = """
            UPDATE dbo.SaleOut
            SET IsActive = 0
            WHERE Id = @Id AND IsActive = 1;

            SELECT CAST(CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END AS bit);
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<bool>(sql, new { Id = id });
    }

    public async Task<bool> SoftDeleteOrderAsync(string saleOutNo)
    {
        const string sql = """
            UPDATE dbo.SaleOut
            SET IsActive = 0
            WHERE SaleOutNo = @SaleOutNo AND IsActive = 1;

            SELECT CAST(CASE WHEN @@ROWCOUNT > 0 THEN 1 ELSE 0 END AS bit);
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<bool>(sql, new { SaleOutNo = saleOutNo });
    }

    public async Task<bool> UpdateAsync(
        Guid id,
        decimal quantity,
        decimal quantityPerBox,
        decimal price,
        decimal amount,
        decimal boxQuantity)
    {
        const string sql = """
            UPDATE dbo.SaleOut
            SET Quantity = @Quantity,
                QuantityPerBox = @QuantityPerBox,
                Price = @Price,
                Amount = @Amount,
                BoxQuantity = @BoxQuantity
            WHERE Id = @Id AND IsActive = 1;

            SELECT CAST(CASE WHEN @@ROWCOUNT = 1 THEN 1 ELSE 0 END AS bit);
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        return await connection.QuerySingleAsync<bool>(sql, new
        {
            Id = id,
            Quantity = quantity,
            QuantityPerBox = quantityPerBox,
            Price = price,
            Amount = amount,
            BoxQuantity = boxQuantity
        });
    }

    public async Task<string?> CreateAsync(IReadOnlyList<SaleOut> saleOuts)
    {
        const string duplicateSql = """
            SELECT CAST(CASE WHEN EXISTS
            (
                SELECT 1
                FROM dbo.SaleOut AS saleOut WITH (UPDLOCK, HOLDLOCK)
                WHERE saleOut.CustomerPoNo = @CustomerPoNo
            ) THEN 1 ELSE 0 END AS bit);
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        var isDuplicate = await connection.QuerySingleAsync<bool>(
            duplicateSql,
            new { CustomerPoNo = saleOuts[0].CustomerPoNo },
            transaction);

        if (isDuplicate)
        {
            await transaction.RollbackAsync();
            return null;
        }

        try
        {
            using (var bulkCopy = new SqlBulkCopy(
                connection,
                SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers,
                transaction))
            {
                bulkCopy.DestinationTableName = "dbo.SaleOut";
                var data = CreateSaleOutDataTable(saleOuts);
                foreach (DataColumn column in data.Columns)
                {
                    bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
                }

                await bulkCopy.WriteToServerAsync(data);
            }

            var saleOutNo = await connection.QuerySingleAsync<string>(
                "SELECT TOP (1) SaleOutNo FROM dbo.SaleOut WHERE CustomerPoNo = @CustomerPoNo ORDER BY Id;",
                new { CustomerPoNo = saleOuts[0].CustomerPoNo },
                transaction);
            await transaction.CommitAsync();
            return saleOutNo;
        }
        catch (SqlException exception) when (exception.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync();
            return null;
        }
    }

    public async Task<IReadOnlyList<SaleOut>> GetOrderAsync(string saleOutNo)
    {
        const string sql = """
            SELECT saleOut.Id,
                   saleOut.SaleOutNo,
                   saleOut.CustomerPoNo,
                   TRY_CONVERT(date, CONVERT(char(8), saleOut.OrderDate), 112) AS OrderDate,
                   saleOut.CustomerName,
                   saleOut.ProductId,
                   product.ProductCode,
                   product.ProductName,
                   product.Unit,
                   saleOut.Quantity,
                   saleOut.Price,
                   saleOut.Amount,
                   saleOut.QuantityPerBox,
                   saleOut.BoxQuantity,
                   saleOut.IsActive
            FROM dbo.SaleOut AS saleOut
            LEFT JOIN dbo.MasterProduct AS product ON product.Id = saleOut.ProductId
            WHERE saleOut.SaleOutNo = @SaleOutNo AND saleOut.IsActive = 1
            ORDER BY saleOut.Id;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        var saleOuts = await connection.QueryAsync<SaleOut>(sql, new { SaleOutNo = saleOutNo });
        return saleOuts.AsList();
    }

    public async Task<bool> UpdateOrderAsync(string saleOutNo, IReadOnlyList<SaleOut> saleOuts)
    {
        await using var connection = _sqlConnectionFactory.CreateConnection();
        await connection.OpenAsync();
        await using var transaction = (SqlTransaction)await connection.BeginTransactionAsync(IsolationLevel.Serializable);

        var existingIds = (await connection.QueryAsync<Guid>(
            "SELECT Id FROM dbo.SaleOut WITH (UPDLOCK, HOLDLOCK) WHERE SaleOutNo = @SaleOutNo AND IsActive = 1;",
            new { SaleOutNo = saleOutNo },
            transaction)).ToHashSet();
        if (existingIds.Count == 0 || saleOuts.Any(item => item.Id != Guid.Empty && !existingIds.Contains(item.Id)))
        {
            await transaction.RollbackAsync();
            return false;
        }

        var retainedIds = saleOuts.Where(item => item.Id != Guid.Empty).Select(item => item.Id).ToHashSet();
        var removedIds = existingIds.Except(retainedIds).ToArray();
        if (removedIds.Length > 0)
        {
            await connection.ExecuteAsync(
                "UPDATE dbo.SaleOut SET IsActive = 0 WHERE Id IN @Ids;",
                new { Ids = removedIds },
                transaction);
        }

        foreach (var saleOut in saleOuts.Where(item => item.Id != Guid.Empty))
        {
            await connection.ExecuteAsync(
                """
                UPDATE dbo.SaleOut
                SET ProductId = @ProductId,
                    Quantity = @Quantity,
                    Price = @Price,
                    Amount = @Amount,
                    QuantityPerBox = @QuantityPerBox,
                    BoxQuantity = @BoxQuantity
                WHERE Id = @Id AND SaleOutNo = @SaleOutNo AND IsActive = 1;
                """,
                new
                {
                    saleOut.Id,
                    saleOut.ProductId,
                    saleOut.Quantity,
                    saleOut.Price,
                    saleOut.Amount,
                    saleOut.QuantityPerBox,
                    saleOut.BoxQuantity,
                    SaleOutNo = saleOutNo
                },
                transaction);
        }

        var newItems = saleOuts.Where(item => item.Id == Guid.Empty).ToArray();
        if (newItems.Length > 0)
        {
            using var bulkCopy = new SqlBulkCopy(
                connection,
                SqlBulkCopyOptions.CheckConstraints | SqlBulkCopyOptions.FireTriggers,
                transaction);
            bulkCopy.DestinationTableName = "dbo.SaleOut";
            var data = CreateSaleOutDataTable(newItems);
            foreach (DataColumn column in data.Columns)
            {
                bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
            }

            await bulkCopy.WriteToServerAsync(data);
        }

        await transaction.CommitAsync();
        return true;
    }

    private static DataTable CreateSaleOutDataTable(IReadOnlyList<SaleOut> saleOuts)
    {
        var data = new DataTable();
        data.Columns.Add("Id", typeof(Guid));
        data.Columns.Add("CustomerPoNo", typeof(string));
        data.Columns.Add("OrderDate", typeof(int));
        data.Columns.Add("CustomerName", typeof(string));
        data.Columns.Add("ProductId", typeof(Guid));
        data.Columns.Add("Quantity", typeof(decimal));
        data.Columns.Add("Price", typeof(decimal));
        data.Columns.Add("Amount", typeof(decimal));
        data.Columns.Add("QuantityPerBox", typeof(decimal));
        data.Columns.Add("BoxQuantity", typeof(decimal));
        data.Columns.Add("IsActive", typeof(bool));

        foreach (var saleOut in saleOuts)
        {
            var orderDate = saleOut.OrderDate is { } date
                ? int.Parse(date.ToString("yyyyMMdd", CultureInfo.InvariantCulture), CultureInfo.InvariantCulture)
                : (int?)null;
            data.Rows.Add(
                saleOut.Id == Guid.Empty ? Guid.NewGuid() : saleOut.Id,
                saleOut.CustomerPoNo,
                orderDate,
                saleOut.CustomerName,
                saleOut.ProductId,
                saleOut.Quantity,
                saleOut.Price,
                saleOut.Amount,
                saleOut.QuantityPerBox,
                saleOut.BoxQuantity,
                saleOut.IsActive);
        }

        return data;
    }
}

using BE.Data;
using BE.Models.Common;
using BE.Models.SaleOut;
using Dapper;

namespace BE.Repositories.SaleOuts;

public sealed partial class SaleOutRepository
{
    public async Task<PagedResult<SaleOutOrder>> SearchPrintOptionsAsync(
        string? searchText,
        long offset,
        int pageSize)
    {
        const string sql = """
            ;WITH ActiveOrders AS
            (
                SELECT saleOut.SaleOutNo,
                       MAX(saleOut.CustomerPoNo) AS CustomerPoNo,
                       MAX(saleOut.OrderDate) AS OrderDateValue,
                       MAX(saleOut.CustomerName) AS CustomerName,
                       COUNT_BIG(1) AS ProductCount,
                       SUM(COALESCE(saleOut.Amount, 0)) AS TotalAmount
                FROM dbo.SaleOut AS saleOut
                WHERE saleOut.IsActive = 1
                  AND saleOut.SaleOutNo IS NOT NULL
                  AND (@SearchPattern IS NULL
                      OR saleOut.SaleOutNo LIKE @SearchPattern ESCAPE '\')
                GROUP BY saleOut.SaleOutNo
            )
            SELECT COUNT_BIG(1) FROM ActiveOrders;

            ;WITH ActiveOrders AS
            (
                SELECT saleOut.SaleOutNo,
                       MAX(saleOut.CustomerPoNo) AS CustomerPoNo,
                       MAX(saleOut.OrderDate) AS OrderDateValue,
                       MAX(saleOut.CustomerName) AS CustomerName,
                       COUNT_BIG(1) AS ProductCount,
                       SUM(COALESCE(saleOut.Amount, 0)) AS TotalAmount
                FROM dbo.SaleOut AS saleOut
                WHERE saleOut.IsActive = 1
                  AND saleOut.SaleOutNo IS NOT NULL
                  AND (@SearchPattern IS NULL
                      OR saleOut.SaleOutNo LIKE @SearchPattern ESCAPE '\')
                GROUP BY saleOut.SaleOutNo
            )
            SELECT SaleOutNo,
                   CustomerPoNo,
                   TRY_CONVERT(date, CONVERT(char(8), OrderDateValue), 112) AS OrderDate,
                   CustomerName,
                   ProductCount,
                   TotalAmount
            FROM ActiveOrders
            ORDER BY OrderDateValue DESC, SaleOutNo DESC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
            """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        using var results = await connection.QueryMultipleAsync(sql, new
        {
            SearchPattern = searchText is null ? null : $"%{SqlLikePattern.Escape(searchText)}%",
            Offset = offset,
            PageSize = pageSize
        });

        return new PagedResult<SaleOutOrder>
        {
            TotalItems = await results.ReadSingleAsync<long>(),
            Items = (await results.ReadAsync<SaleOutOrder>()).AsList()
        };
    }

    public async Task<PagedResult<SaleOutRevenueReportItem>> RevenueReportAsync(
       DateOnly timeBegin,
       DateOnly timeEnd,
       long offset,
       int pageSize)
    {
        const string sql = """
            ;WITH RevenueItems AS
            (
                SELECT product.ProductCode,
                       product.ProductName,
                       SUM(COALESCE(saleOut.Quantity, 0)) AS Quantity,
                       SUM(COALESCE(saleOut.Amount, 0)) AS Amount
                FROM dbo.SaleOut AS saleOut
                INNER JOIN dbo.MasterProduct AS product ON product.Id = saleOut.ProductId
                WHERE saleOut.IsActive = 1
                  AND saleOut.OrderDate >= @TimeBegin
                  AND saleOut.OrderDate <= @TimeEnd
                GROUP BY product.ProductCode, product.ProductName
            )
            SELECT COUNT_BIG(1) FROM RevenueItems;

            ;WITH RevenueItems AS
            (
                SELECT product.ProductCode,
                       product.ProductName,
                       SUM(COALESCE(saleOut.Quantity, 0)) AS Quantity,
                       SUM(COALESCE(saleOut.Amount, 0)) AS Amount
                FROM dbo.SaleOut AS saleOut
                INNER JOIN dbo.MasterProduct AS product ON product.Id = saleOut.ProductId
                WHERE saleOut.IsActive = 1
                  AND saleOut.OrderDate >= @TimeBegin
                  AND saleOut.OrderDate <= @TimeEnd
                GROUP BY product.ProductCode, product.ProductName
            )
            SELECT ProductCode, ProductName, Quantity, Amount
            FROM RevenueItems
            ORDER BY ProductCode ASC, ProductName ASC
            OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
           """;

        await using var connection = _sqlConnectionFactory.CreateConnection();
        using var results = await connection.QueryMultipleAsync(sql, new
        {
            TimeBegin = int.Parse(timeBegin.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture),
            TimeEnd = int.Parse(timeEnd.ToString("yyyyMMdd", System.Globalization.CultureInfo.InvariantCulture), System.Globalization.CultureInfo.InvariantCulture),
            Offset = offset,
            PageSize = pageSize
        });

        return new PagedResult<SaleOutRevenueReportItem>
        {
            TotalItems = await results.ReadSingleAsync<long>(),
            Items = (await results.ReadAsync<SaleOutRevenueReportItem>()).AsList()
        };
    }



    public async Task<PagedResult<SaleOutOrder>> SearchAsync(SaleOutSearchCriteria criteria)
    {
        const string fromClause = """
            FROM dbo.SaleOut AS saleOut
            LEFT JOIN dbo.MasterProduct AS product ON product.Id = saleOut.ProductId
            """;
        var whereClause = BuildWhereClause(criteria.SearchField);
        var sql = $"""
        ;WITH MatchedOrders AS
        (
            SELECT DISTINCT saleOut.SaleOutNo
            {fromClause}
            {whereClause}
        )
        SELECT COUNT_BIG(1) FROM MatchedOrders;

        ;WITH MatchedOrders AS
        (
            SELECT DISTINCT saleOut.SaleOutNo
            {fromClause}
            {whereClause}
        )
        SELECT orderSaleOut.SaleOutNo,
               MAX(orderSaleOut.CustomerPoNo) AS CustomerPoNo,
               TRY_CONVERT(date, CONVERT(char(8), MAX(orderSaleOut.OrderDate)), 112) AS OrderDate,
               MAX(orderSaleOut.CustomerName) AS CustomerName,
               COUNT_BIG(1) AS ProductCount,
               SUM(COALESCE(orderSaleOut.Amount, 0)) AS TotalAmount
        FROM dbo.SaleOut AS orderSaleOut
        INNER JOIN MatchedOrders AS matched ON matched.SaleOutNo = orderSaleOut.SaleOutNo
        WHERE orderSaleOut.IsActive = 1
        GROUP BY orderSaleOut.SaleOutNo
        ORDER BY MAX(orderSaleOut.OrderDate) DESC, orderSaleOut.SaleOutNo DESC
        OFFSET @Offset ROWS FETCH NEXT @PageSize ROWS ONLY;
        """;

        var parameters = new DynamicParameters();
        parameters.Add("Offset", criteria.Offset);
        parameters.Add("PageSize", criteria.PageSize);

        if (criteria.SearchText is not null)
        {
            parameters.Add("SearchPattern", $"%{SqlLikePattern.Escape(criteria.SearchText)}%");
        }

        await using var connection = _sqlConnectionFactory.CreateConnection();
        long totalItems;
        List<SaleOutOrder> orders;
        using (var results = await connection.QueryMultipleAsync(sql, parameters))
        {
            totalItems = await results.ReadSingleAsync<long>();
            orders = (await results.ReadAsync<SaleOutOrder>()).AsList();
        }

        var saleOutNos = orders
            .Select(order => order.SaleOutNo)
            .Where(saleOutNo => saleOutNo is not null)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var productsByOrder = new Dictionary<string, SaleOut[]>(StringComparer.OrdinalIgnoreCase);
        if (saleOutNos.Length > 0)
        {
            const string productsSql = """
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
                WHERE saleOut.SaleOutNo IN @SaleOutNos AND saleOut.IsActive = 1
                ORDER BY saleOut.SaleOutNo, saleOut.Id;
                """;
            var products = await connection.QueryAsync<SaleOut>(productsSql, new { SaleOutNos = saleOutNos });
            productsByOrder = products
                .GroupBy(product => product.SaleOutNo!, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.ToArray(), StringComparer.OrdinalIgnoreCase);
        }

        var items = orders.Select(order => new SaleOutOrder
        {
            SaleOutNo = order.SaleOutNo,
            CustomerPoNo = order.CustomerPoNo,
            OrderDate = order.OrderDate,
            CustomerName = order.CustomerName,
            ProductCount = order.ProductCount,
            TotalAmount = order.TotalAmount,
            Products = order.SaleOutNo is not null && productsByOrder.TryGetValue(order.SaleOutNo, out var products)
                ? products
                : Array.Empty<SaleOut>()
        }).ToArray();

        return new PagedResult<SaleOutOrder>
        {
            Items = items,
            TotalItems = totalItems
        };
    }

    private static string BuildWhereClause(SaleOutSearchField? searchField)
    {
        return searchField switch
        {
            SaleOutSearchField.ProductCode => $"WHERE saleOut.IsActive = 1 AND product.ProductCode LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            SaleOutSearchField.SaleOutNo => $"WHERE saleOut.IsActive = 1 AND saleOut.SaleOutNo LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            SaleOutSearchField.ProductName => $"WHERE saleOut.IsActive = 1 AND product.ProductName LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            SaleOutSearchField.CustomerPoNo => $"WHERE saleOut.IsActive = 1 AND saleOut.CustomerPoNo LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            SaleOutSearchField.CustomerName => $"WHERE saleOut.IsActive = 1 AND saleOut.CustomerName LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            SaleOutSearchField.OrderDate => $"WHERE saleOut.IsActive = 1 AND CONVERT(varchar(8), saleOut.OrderDate) LIKE @SearchPattern ESCAPE '{SqlLikePattern.ESCAPE_CHARACTER}'",
            null => "WHERE saleOut.IsActive = 1",
            _ => throw new InvalidOperationException("Trường tìm kiếm Sale Out chưa được hỗ trợ.")
        };
    }
}

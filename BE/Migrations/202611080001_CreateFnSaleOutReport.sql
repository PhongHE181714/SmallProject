USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    ;THROW 51024, 'Table dbo.SaleOut does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.MasterProduct', N'U') IS NULL
BEGIN
    ;THROW 51025, 'Table dbo.MasterProduct does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51026, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

CREATE OR ALTER FUNCTION dbo.fnSaleOutReport
(
    @StartDate INT,
    @EndDate INT
)
RETURNS TABLE
AS
RETURN
(
    SELECT product.ProductCode,
           product.ProductName,
           SUM(COALESCE(saleOut.Quantity, 0)) AS Quantity,
           SUM(COALESCE(saleOut.Amount, 0)) AS Amount
    FROM dbo.SaleOut AS saleOut
    INNER JOIN dbo.MasterProduct AS product ON product.Id = saleOut.ProductId
    WHERE saleOut.IsActive = 1
      AND saleOut.OrderDate >= @StartDate
      AND saleOut.OrderDate <= @EndDate
    GROUP BY product.ProductCode, product.ProductName
);
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.__SchemaMigrations
    WHERE Version = N'202611080001'
)
BEGIN
    INSERT INTO dbo.__SchemaMigrations (Version, Description)
    VALUES
    (
        N'202611080001',
        N'Create dbo.fnSaleOutReport for aggregated SaleOut revenue by product and date range.'
    );
END;
GO

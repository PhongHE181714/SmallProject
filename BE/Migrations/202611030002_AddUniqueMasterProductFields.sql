USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.MasterProduct', N'U') IS NULL
BEGIN
    ;THROW 51004, 'Table dbo.MasterProduct does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51005, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611030002'
    )
    BEGIN
        ;WITH DuplicatedProductCodes AS
        (
            SELECT
                Id,
                ProductCode,
                ROW_NUMBER() OVER (PARTITION BY ProductCode ORDER BY Id) AS DuplicateOrder
            FROM dbo.MasterProduct
            WHERE ProductCode IS NOT NULL
        )
        UPDATE product
        SET ProductCode = CONCAT(
            LEFT(duplicates.ProductCode, 13),
            N'-',
            REPLACE(CONVERT(NVARCHAR(36), duplicates.Id), N'-', N'')
        )
        OUTPUT deleted.Id, deleted.ProductCode, inserted.ProductCode
        FROM dbo.MasterProduct AS product
        INNER JOIN DuplicatedProductCodes AS duplicates ON duplicates.Id = product.Id
        WHERE duplicates.DuplicateOrder > 1;

        IF EXISTS
        (
            SELECT ProductCode
            FROM dbo.MasterProduct
            WHERE ProductCode IS NOT NULL
            GROUP BY ProductCode
            HAVING COUNT_BIG(1) > 1
        )
        BEGIN
            ;THROW 51006, 'Duplicate ProductCode values remain after assigning generated codes. Resolve them before applying this migration.', 1;
        END;

        IF EXISTS
        (
            SELECT ProductName
            FROM dbo.MasterProduct
            WHERE ProductName IS NOT NULL
            GROUP BY ProductName
            HAVING COUNT_BIG(1) > 1
        )
        BEGIN
            ;THROW 51007, 'Duplicate ProductName values exist. Clean them before applying this migration.', 1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.MasterProduct')
              AND name = N'INDEX_MasterProduct_ProductCode'
        )
        BEGIN
            CREATE UNIQUE INDEX INDEX_MasterProduct_ProductCode
                ON dbo.MasterProduct (ProductCode)
                WHERE ProductCode IS NOT NULL;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.MasterProduct')
              AND name = N'INDEX_MasterProduct_ProductName'
        )
        BEGIN
            CREATE UNIQUE INDEX INDEX_MasterProduct_ProductName
                ON dbo.MasterProduct (ProductName)
                WHERE ProductName IS NOT NULL;
        END;

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611030002',
            N'Add filtered unique indexes for non-null MasterProduct ProductCode and ProductName.'
        );
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0
        ROLLBACK TRANSACTION;

    THROW;
END CATCH;
GO

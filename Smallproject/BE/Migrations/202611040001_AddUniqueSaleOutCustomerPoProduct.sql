USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    ;THROW 51008, 'Table dbo.SaleOut does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51009, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611040001'
    )
    BEGIN
        IF EXISTS
        (
            SELECT CustomerPoNo, ProductId
            FROM dbo.SaleOut
            WHERE CustomerPoNo IS NOT NULL AND ProductId IS NOT NULL
            GROUP BY CustomerPoNo, ProductId
            HAVING COUNT_BIG(1) > 1
        )
        BEGIN
            ;THROW 51010, 'Duplicate CustomerPoNo and ProductId pairs exist in dbo.SaleOut. Resolve them before applying this migration.', 1;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.SaleOut')
              AND name = N'INDEX_SaleOut_CustomerPoNo_ProductId'
        )
        BEGIN
            CREATE UNIQUE INDEX INDEX_SaleOut_CustomerPoNo_ProductId
                ON dbo.SaleOut (CustomerPoNo, ProductId)
                WHERE CustomerPoNo IS NOT NULL AND ProductId IS NOT NULL;
        END;

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611040001',
            N'Ensure a customer PO and product pair can only be used once in SaleOut.'
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

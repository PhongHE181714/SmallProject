USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    ;THROW 51011, 'Table dbo.SaleOut does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51012, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611040002'
    )
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM dbo.SaleOut
            WHERE Quantity > 0
              AND QuantityPerBox > 0
              AND CEILING(Quantity / QuantityPerBox) > 999999999999999.999
        )
        BEGIN
            ;THROW 51013, 'A calculated BoxQuantity exceeds the supported DECIMAL(18, 3) range.', 1;
        END;

        UPDATE dbo.SaleOut
        SET BoxQuantity = CEILING(Quantity / QuantityPerBox)
        WHERE Quantity > 0
          AND QuantityPerBox > 0;

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611040002',
            N'Recalculate existing SaleOut BoxQuantity values by rounding up.'
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

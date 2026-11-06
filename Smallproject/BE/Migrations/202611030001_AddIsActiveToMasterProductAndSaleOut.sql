USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.MasterProduct', N'U') IS NULL
BEGIN
    ;THROW 51001, 'Table dbo.MasterProduct does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    ;THROW 51002, 'Table dbo.SaleOut does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51003, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611030001'
    )
    BEGIN
        IF COL_LENGTH(N'dbo.MasterProduct', N'IsActive') IS NULL
        BEGIN
            ALTER TABLE dbo.MasterProduct
                ADD IsActive BIT NOT NULL
                    CONSTRAINT DF_MasterProduct_IsActive DEFAULT (1) WITH VALUES;
        END;

        IF COL_LENGTH(N'dbo.SaleOut', N'IsActive') IS NULL
        BEGIN
            ALTER TABLE dbo.SaleOut
                ADD IsActive BIT NOT NULL
                    CONSTRAINT DF_SaleOut_IsActive DEFAULT (1) WITH VALUES;
        END;

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611030001',
            N'Add IsActive to MasterProduct and SaleOut for soft deletion; existing records remain active.'
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

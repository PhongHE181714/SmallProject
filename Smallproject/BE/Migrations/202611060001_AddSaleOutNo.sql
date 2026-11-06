USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    ;THROW 51014, 'Table dbo.SaleOut does not exist. Run the initial migration first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51015, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611060001'
    )
    BEGIN
        IF COL_LENGTH(N'dbo.SaleOut', N'SaleOutNo') IS NULL
        BEGIN
            ALTER TABLE dbo.SaleOut
                ADD SaleOutNo NVARCHAR(50) NULL;
        END;

        IF NOT EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.SaleOut')
              AND name = N'INDEX_SaleOut_SaleOutNo'
        )
        BEGIN
            EXEC sys.sp_executesql N'
                CREATE UNIQUE INDEX INDEX_SaleOut_SaleOutNo
                    ON dbo.SaleOut (SaleOutNo)
                    WHERE SaleOutNo IS NOT NULL;';
        END;

        EXEC sys.sp_executesql N'
            CREATE OR ALTER TRIGGER dbo.TR_SaleOut_GenerateSaleOutNo
            ON dbo.SaleOut
            AFTER INSERT
            AS
            BEGIN
                SET NOCOUNT ON;

                DECLARE @YearMonth CHAR(6) = CONVERT(CHAR(6), SYSDATETIME(), 112);
                DECLARE @Prefix NVARCHAR(9) = N''STO'' + @YearMonth;
                DECLARE @LockResource NVARCHAR(50) = N''SaleOutNo:'' + @YearMonth;
                DECLARE @LockResult INT;

                EXEC @LockResult = sys.sp_getapplock
                    @Resource = @LockResource,
                    @LockMode = N''Exclusive'',
                    @LockOwner = N''Transaction'',
                    @LockTimeout = 10000;

                IF @LockResult < 0
                BEGIN
                    ;THROW 51016, ''Could not acquire the SaleOutNo generation lock.'', 1;
                END;

                DECLARE @LastNumber BIGINT = ISNULL
                (
                    (
                        SELECT MAX(TRY_CONVERT(BIGINT, SUBSTRING(SaleOutNo, 10, 42)))
                        FROM dbo.SaleOut WITH (UPDLOCK, HOLDLOCK)
                        WHERE SaleOutNo LIKE @Prefix + N''%''
                    ),
                    0
                );

                ;WITH NumberedInsertedRows AS
                (
                    SELECT Id,
                           ROW_NUMBER() OVER (ORDER BY Id) AS RowNumber
                    FROM inserted
                )
                UPDATE saleOut
                SET SaleOutNo = @Prefix + RIGHT
                (
                    REPLICATE(N''0'', 4) + CONVERT(NVARCHAR(20), @LastNumber + numbered.RowNumber),
                    CASE
                        WHEN @LastNumber + numbered.RowNumber < 10000 THEN 4
                        ELSE LEN(CONVERT(NVARCHAR(20), @LastNumber + numbered.RowNumber))
                    END
                )
                FROM dbo.SaleOut AS saleOut
                INNER JOIN NumberedInsertedRows AS numbered ON numbered.Id = saleOut.Id;
            END;';

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611060001',
            N'Add SaleOutNo and generate monthly STO numbers for new SaleOut rows.'
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

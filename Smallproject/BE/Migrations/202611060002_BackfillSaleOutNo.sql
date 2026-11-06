USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    ;THROW 51017, 'Table dbo.SaleOut does not exist. Run the initial migration first.', 1;
END;

IF COL_LENGTH(N'dbo.SaleOut', N'SaleOutNo') IS NULL
BEGIN
    ;THROW 51018, 'Column dbo.SaleOut.SaleOutNo does not exist. Run migration 202611060001 first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51019, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611060002'
    )
    BEGIN
        ;WITH SaleOutDates AS
        (
            SELECT saleOut.Id,
                   COALESCE
                   (
                       TRY_CONVERT(DATE, CONVERT(CHAR(8), saleOut.OrderDate), 112),
                       CONVERT(DATE, SYSDATETIME())
                   ) AS ReferenceDate
            FROM dbo.SaleOut AS saleOut
            WHERE saleOut.SaleOutNo IS NULL
        ),
        SaleOutMonths AS
        (
            SELECT Id,
                   ReferenceDate,
                   CONVERT(CHAR(6), ReferenceDate, 112) AS YearMonth
            FROM SaleOutDates
        ),
        NumberedSaleOuts AS
        (
            SELECT Id,
                   YearMonth,
                   ROW_NUMBER() OVER
                   (
                       PARTITION BY YearMonth
                       ORDER BY ReferenceDate, Id
                   ) AS RowNumber
            FROM SaleOutMonths
        ),
        SaleOutNumbers AS
        (
            SELECT numbered.Id,
                   numbered.YearMonth,
                   numbered.RowNumber,
                   ISNULL(existing.MaxNumber, 0) + numbered.RowNumber AS SaleOutSequence
            FROM NumberedSaleOuts AS numbered
            OUTER APPLY
            (
                SELECT MAX(TRY_CONVERT(BIGINT, SUBSTRING(existingSaleOut.SaleOutNo, 10, 42))) AS MaxNumber
                FROM dbo.SaleOut AS existingSaleOut WITH (UPDLOCK, HOLDLOCK)
                WHERE existingSaleOut.SaleOutNo LIKE N'STO' + numbered.YearMonth + N'%'
            ) AS existing
        )
        UPDATE saleOut
        SET SaleOutNo = N'STO' + saleOutNumbers.YearMonth + RIGHT
        (
            REPLICATE(N'0', 4) + CONVERT(NVARCHAR(20), saleOutNumbers.SaleOutSequence),
            CASE
                WHEN saleOutNumbers.SaleOutSequence < 10000 THEN 4
                ELSE LEN(CONVERT(NVARCHAR(20), saleOutNumbers.SaleOutSequence))
            END
        )
        FROM dbo.SaleOut AS saleOut
        INNER JOIN SaleOutNumbers AS saleOutNumbers ON saleOutNumbers.Id = saleOut.Id;

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611060002',
            N'Backfill monthly SaleOutNo values for existing SaleOut records using OrderDate.'
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

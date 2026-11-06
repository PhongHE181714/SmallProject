USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    ;THROW 51020, 'Table dbo.SaleOut does not exist. Run the initial migration first.', 1;
END;

IF COL_LENGTH(N'dbo.SaleOut', N'SaleOutNo') IS NULL
BEGIN
    ;THROW 51021, 'Column dbo.SaleOut.SaleOutNo does not exist. Run migration 202611060001 first.', 1;
END;

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    ;THROW 51022, 'Migration history table dbo.__SchemaMigrations does not exist. Run the initial migration first.', 1;
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611060003'
    )
    BEGIN
        IF EXISTS
        (
            SELECT 1
            FROM sys.indexes
            WHERE object_id = OBJECT_ID(N'dbo.SaleOut')
              AND name = N'INDEX_SaleOut_SaleOutNo'
        )
        BEGIN
            DROP INDEX INDEX_SaleOut_SaleOutNo ON dbo.SaleOut;
        END;

        ;WITH SaleOutDates AS
        (
            SELECT saleOut.Id,
                   saleOut.CustomerPoNo,
                   CASE
                       WHEN saleOut.CustomerPoNo IS NULL
                           THEN N'ID:' + CONVERT(NVARCHAR(36), saleOut.Id)
                       ELSE N'PO:' + saleOut.CustomerPoNo
                   END AS OrderKey,
                   COALESCE
                   (
                       TRY_CONVERT(DATE, CONVERT(CHAR(8), saleOut.OrderDate), 112),
                       CONVERT(DATE, SYSDATETIME())
                   ) AS ReferenceDate
            FROM dbo.SaleOut AS saleOut
        ),
        OrderDates AS
        (
            SELECT OrderKey,
                   MIN(ReferenceDate) AS ReferenceDate
            FROM SaleOutDates
            GROUP BY OrderKey
        ),
        NumberedOrders AS
        (
            SELECT OrderKey,
                   CONVERT(CHAR(6), ReferenceDate, 112) AS YearMonth,
                   ROW_NUMBER() OVER
                   (
                       PARTITION BY CONVERT(CHAR(6), ReferenceDate, 112)
                       ORDER BY OrderKey
                   ) AS RowNumber
            FROM OrderDates
        ),
        OrderNumbers AS
        (
            SELECT numbered.OrderKey,
                   numbered.YearMonth,
                   ISNULL(existing.MaxNumber, 0) + numbered.RowNumber AS SaleOutSequence
            FROM NumberedOrders AS numbered
            OUTER APPLY
            (
                SELECT MAX(TRY_CONVERT(BIGINT, SUBSTRING(saleOut.SaleOutNo, 10, 42))) AS MaxNumber
                FROM dbo.SaleOut AS saleOut WITH (UPDLOCK, HOLDLOCK)
                WHERE saleOut.SaleOutNo LIKE N'STO' + numbered.YearMonth + N'%'
            ) AS existing
        )
        UPDATE saleOut
        SET SaleOutNo = N'STO' + orderNumbers.YearMonth + RIGHT
        (
            REPLICATE(N'0', 4) + CONVERT(NVARCHAR(20), orderNumbers.SaleOutSequence),
            CASE
                WHEN orderNumbers.SaleOutSequence < 10000 THEN 4
                ELSE LEN(CONVERT(NVARCHAR(20), orderNumbers.SaleOutSequence))
            END
        )
        FROM dbo.SaleOut AS saleOut
        INNER JOIN SaleOutDates AS saleOutDates ON saleOutDates.Id = saleOut.Id
        INNER JOIN OrderNumbers AS orderNumbers ON orderNumbers.OrderKey = saleOutDates.OrderKey;

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
                    ;THROW 51023, ''Could not acquire the SaleOutNo generation lock.'', 1;
                END;

                DECLARE @OrderGroups TABLE
                (
                    CustomerPoNo NVARCHAR(50) NULL,
                    RepresentativeId NVARCHAR(36) NOT NULL,
                    ExistingSaleOutNo NVARCHAR(50) NULL,
                    GeneratedSaleOutNo NVARCHAR(50) NULL
                );

                INSERT INTO @OrderGroups (CustomerPoNo, RepresentativeId)
                SELECT CustomerPoNo, MIN(CONVERT(NVARCHAR(36), Id))
                FROM inserted
                GROUP BY CustomerPoNo;

                UPDATE orderGroup
                SET ExistingSaleOutNo = existing.SaleOutNo
                FROM @OrderGroups AS orderGroup
                OUTER APPLY
                (
                    SELECT TOP (1) saleOut.SaleOutNo
                    FROM dbo.SaleOut AS saleOut WITH (UPDLOCK, HOLDLOCK)
                    WHERE saleOut.SaleOutNo IS NOT NULL
                      AND saleOut.CustomerPoNo = orderGroup.CustomerPoNo
                      AND saleOut.Id NOT IN (SELECT Id FROM inserted)
                    ORDER BY saleOut.Id
                ) AS existing;

                DECLARE @LastNumber BIGINT = ISNULL
                (
                    (
                        SELECT MAX(TRY_CONVERT(BIGINT, SUBSTRING(SaleOutNo, 10, 42)))
                        FROM dbo.SaleOut WITH (UPDLOCK, HOLDLOCK)
                        WHERE SaleOutNo LIKE @Prefix + N''%''
                    ),
                    0
                );

                ;WITH NewOrderNumbers AS
                (
                    SELECT RepresentativeId,
                           ROW_NUMBER() OVER (ORDER BY RepresentativeId) AS RowNumber
                    FROM @OrderGroups
                    WHERE ExistingSaleOutNo IS NULL
                )
                UPDATE orderGroup
                SET GeneratedSaleOutNo = @Prefix + RIGHT
                (
                    REPLICATE(N''0'', 4) + CONVERT(NVARCHAR(20), @LastNumber + newOrderNumbers.RowNumber),
                    CASE
                        WHEN @LastNumber + newOrderNumbers.RowNumber < 10000 THEN 4
                        ELSE LEN(CONVERT(NVARCHAR(20), @LastNumber + newOrderNumbers.RowNumber))
                    END
                )
                FROM @OrderGroups AS orderGroup
                INNER JOIN NewOrderNumbers AS newOrderNumbers
                    ON newOrderNumbers.RepresentativeId = orderGroup.RepresentativeId;

                UPDATE saleOut
                SET SaleOutNo = COALESCE(orderGroup.ExistingSaleOutNo, orderGroup.GeneratedSaleOutNo)
                FROM dbo.SaleOut AS saleOut
                INNER JOIN inserted ON inserted.Id = saleOut.Id
                INNER JOIN @OrderGroups AS orderGroup
                    ON (orderGroup.CustomerPoNo = saleOut.CustomerPoNo)
                    OR (orderGroup.CustomerPoNo IS NULL AND saleOut.CustomerPoNo IS NULL);
            END;';

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611060003',
            N'Group SaleOut product rows under one SaleOutNo per customer PO and update monthly generation.'
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

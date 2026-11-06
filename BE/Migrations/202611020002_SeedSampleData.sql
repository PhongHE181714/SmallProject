USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.__SchemaMigrations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.__SchemaMigrations
    (
        Version       NVARCHAR(50)  NOT NULL,
        Description   NVARCHAR(300) NOT NULL,
        AppliedAtUtc  DATETIME2(0)  NOT NULL
            CONSTRAINT DF_SchemaMigrations_AppliedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT PK_SchemaMigrations PRIMARY KEY (Version)
    );
END;
GO

SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611020001'
    )
    BEGIN
        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611020001',
            N'Baseline: SmallProject tables were created from the supplied schema.'
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM dbo.__SchemaMigrations
        WHERE Version = N'202611020002'
    )
    BEGIN
        INSERT INTO dbo.MasterProduct
        (
            Id, ProductCode, ProductName, Unit, Specification,
            QuantityPerBox, ProductWeight
        )
        VALUES
        (
            'A1000000-0000-0000-0000-000000000001', N'P001',
            N'Cà phê rang xay 250g', N'Hộp', N'250 g x 12 hộp/thùng',
            12.000, 0.250
        ),
        (
            'A1000000-0000-0000-0000-000000000002', N'P002',
            N'Trà xanh túi lọc', N'Hộp', N'2 g x 25 túi x 20 hộp/thùng',
            20.000, 0.050
        ),
        (
            'A1000000-0000-0000-0000-000000000003', N'P003',
            N'Mật ong hoa cà phê 500ml', N'Chai', N'500 ml x 12 chai/thùng',
            12.000, 0.600
        );

        ;WITH ProductNumbers AS
        (
            SELECT 4 AS Number
            UNION ALL
            SELECT Number + 1
            FROM ProductNumbers
            WHERE Number < 10
        )
        INSERT INTO dbo.MasterProduct
        (
            Id, ProductCode, ProductName, Unit, Specification,
            QuantityPerBox, ProductWeight
        )
        SELECT
            CONVERT(uniqueidentifier, CONCAT(
                'C3000000-0000-0000-0000-',
                RIGHT('000000000000' + CONVERT(VARCHAR(12), Number), 12)
            )),
            CONCAT(N'P', RIGHT('000' + CONVERT(VARCHAR(3), Number), 3)),
            CONCAT(N'Sản phẩm mẫu ', RIGHT('000' + CONVERT(VARCHAR(3), Number), 3)),
            CASE Number % 3
                WHEN 0 THEN N'Hộp'
                WHEN 1 THEN N'Chai'
                ELSE N'Túi'
            END,
            CONCAT(N'Quy cách mẫu ', Number),
            CONVERT(DECIMAL(18, 3), 10 + (Number % 20)),
            CONVERT(DECIMAL(18, 3), 0.100 + ((Number % 10) * 0.100))
        FROM ProductNumbers
        OPTION (MAXRECURSION 0);

        INSERT INTO dbo.SaleOut
        (
            Id, CustomerPoNo, OrderDate, CustomerName, ProductId,
            Quantity, Price, Amount, QuantityPerBox, BoxQuantity
        )
        VALUES
        (
            'B1000000-0000-0000-0000-000000000001', N'PO-20261102-001',
            20261102, N'Công ty An Phát',
            'A1000000-0000-0000-0000-000000000001',
            24.000, 85000.000, 2040000.000, 12.000, 2.000
        ),
        (
            'B1000000-0000-0000-0000-000000000002', N'PO-20261102-001',
            20261102, N'Công ty An Phát',
            'A1000000-0000-0000-0000-000000000002',
            40.000, 65000.000, 2600000.000, 20.000, 2.000
        ),
        (
            'B1000000-0000-0000-0000-000000000003', N'PO-20261102-002',
            20261102, N'Cửa hàng Minh Long',
            'A1000000-0000-0000-0000-000000000003',
            12.000, 120000.000, 1440000.000, 12.000, 1.000
        );

        ;WITH SaleNumbers AS
        (
            SELECT 4 AS Number
            UNION ALL
            SELECT Number + 1
            FROM SaleNumbers
            WHERE Number < 10
        )
        INSERT INTO dbo.SaleOut
        (
            Id, CustomerPoNo, OrderDate, CustomerName, ProductId,
            Quantity, Price, Amount, QuantityPerBox, BoxQuantity
        )
        SELECT
            CONVERT(uniqueidentifier, CONCAT(
                'B1000000-0000-0000-0000-',
                RIGHT('000000000000' + CONVERT(VARCHAR(12), Number), 12)
            )),
            CONCAT(N'PO-20261102-', RIGHT('000' + CONVERT(VARCHAR(3), Number), 3)),
            20261102 + Number,
            CONCAT(N'Công ty mẫu ', RIGHT('00' + CONVERT(VARCHAR(2), Number), 2)),
            CONVERT(uniqueidentifier, CONCAT(
                'C3000000-0000-0000-0000-',
                RIGHT('000000000000' + CONVERT(VARCHAR(12), Number), 12)
            )),
            Sample.BoxQuantity * Sample.QuantityPerBox,
            Sample.Price,
            Sample.BoxQuantity * Sample.QuantityPerBox * Sample.Price,
            Sample.QuantityPerBox,
            Sample.BoxQuantity
        FROM SaleNumbers
        CROSS APPLY
        (
            VALUES
            (
                CONVERT(DECIMAL(18, 3), 1 + (SaleNumbers.Number % 3)),
                CONVERT(DECIMAL(18, 3), 10 + (SaleNumbers.Number % 20)),
                CONVERT(DECIMAL(18, 3), 50000 + (SaleNumbers.Number * 1000))
            )
        ) AS Sample (BoxQuantity, QuantityPerBox, Price)
        OPTION (MAXRECURSION 0);

        INSERT INTO dbo.__SchemaMigrations (Version, Description)
        VALUES
        (
            N'202611020002',
            N'Insert ten sample products and ten sample SaleOut rows.'
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

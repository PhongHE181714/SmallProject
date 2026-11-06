IF DB_ID(N'SmallProject') IS NULL
    CREATE DATABASE [SmallProject];
GO

USE [SmallProject];
GO

IF OBJECT_ID(N'dbo.MasterProduct', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.MasterProduct
    (
        Id              UNIQUEIDENTIFIER NOT NULL,
        ProductCode     NVARCHAR(50)     NULL,
        ProductName     NVARCHAR(200)    NULL,
        Unit            NVARCHAR(50)     NULL,
        Specification   NVARCHAR(200)    NULL,
        QuantityPerBox  DECIMAL(18, 3)   NULL,
        ProductWeight   DECIMAL(18, 3)   NULL,
        CONSTRAINT PK_MasterProduct PRIMARY KEY (Id)
    );
END;
GO

IF OBJECT_ID(N'dbo.SaleOut', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SaleOut
    (
        Id              UNIQUEIDENTIFIER NOT NULL,
        CustomerPoNo    NVARCHAR(50)     NULL,
        OrderDate       INT              NULL,
        CustomerName    NVARCHAR(200)    NULL,
        ProductId       UNIQUEIDENTIFIER NULL,
        Quantity        DECIMAL(18, 3)   NULL,
        Price           DECIMAL(18, 3)   NULL,
        Amount          DECIMAL(18, 3)   NULL,
        QuantityPerBox  DECIMAL(18, 3)   NULL,
        BoxQuantity     DECIMAL(18, 3)   NULL,
        CONSTRAINT PK_SaleOut PRIMARY KEY (Id),
        CONSTRAINT FK_SaleOut_MasterProduct
            FOREIGN KEY (ProductId) REFERENCES dbo.MasterProduct(Id)
    );
END;
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
        N'Create MasterProduct and SaleOut with primary and foreign keys.'
    );
END;
GO

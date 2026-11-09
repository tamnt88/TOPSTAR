SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.BrandPackagePurchases', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BrandPackagePurchases
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BrandPackagePurchases PRIMARY KEY,
            BrandProfileId UNIQUEIDENTIFIER NOT NULL,
            PackagePolicyId UNIQUEIDENTIFIER NOT NULL,
            CampaignId UNIQUEIDENTIFIER NULL,
            OrderNumber VARCHAR(40) NOT NULL,
            PackageCodeSnapshot VARCHAR(80) NOT NULL,
            PackageNameSnapshot NVARCHAR(150) NOT NULL,
            AmountSnapshot DECIMAL(18,2) NOT NULL,
            ValidityDaysSnapshot INT NULL,
            Status VARCHAR(20) NOT NULL,
            PaymentReference VARCHAR(100) NULL,
            PurchasedAtUtc DATETIME2 NOT NULL,
            PaidAtUtc DATETIME2 NULL,
            ExpiresAtUtc DATETIME2 NULL,
            ConsumedAtUtc DATETIME2 NULL,
            CreatedAtUtc DATETIME2 NOT NULL,
            UpdatedAtUtc DATETIME2 NULL,
            CONSTRAINT FK_BrandPackagePurchases_Brand FOREIGN KEY(BrandProfileId) REFERENCES dbo.BrandProfiles(Id),
            CONSTRAINT FK_BrandPackagePurchases_Policy FOREIGN KEY(PackagePolicyId) REFERENCES dbo.CampaignPackagePolicies(Id),
            CONSTRAINT FK_BrandPackagePurchases_Campaign FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id),
            CONSTRAINT UQ_BrandPackagePurchases_OrderNumber UNIQUE(OrderNumber),
            CONSTRAINT CK_BrandPackagePurchases_Amount CHECK(AmountSnapshot > 0),
            CONSTRAINT CK_BrandPackagePurchases_Validity CHECK(ValidityDaysSnapshot IS NULL OR ValidityDaysSnapshot > 0),
            CONSTRAINT CK_BrandPackagePurchases_Status CHECK(Status IN ('Pending','Paid','Consumed','Expired','Cancelled')),
            CONSTRAINT CK_BrandPackagePurchases_Dates CHECK(
                (PaidAtUtc IS NULL OR PaidAtUtc >= PurchasedAtUtc) AND
                (ExpiresAtUtc IS NULL OR PaidAtUtc IS NOT NULL) AND
                (ConsumedAtUtc IS NULL OR PaidAtUtc IS NOT NULL) AND
                ((Status = 'Consumed' AND CampaignId IS NOT NULL AND ConsumedAtUtc IS NOT NULL) OR Status <> 'Consumed')
            )
        );
        CREATE INDEX IX_BrandPackagePurchases_Brand_Status ON dbo.BrandPackagePurchases(BrandProfileId, Status, PurchasedAtUtc DESC);
        CREATE UNIQUE INDEX UX_BrandPackagePurchases_Campaign ON dbo.BrandPackagePurchases(CampaignId) WHERE CampaignId IS NOT NULL;
    END;

    IF OBJECT_ID(N'dbo.BrandPackagePurchaseHistories', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.BrandPackagePurchaseHistories
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_BrandPackagePurchaseHistories PRIMARY KEY,
            PurchaseId UNIQUEIDENTIFIER NOT NULL,
            FromStatus VARCHAR(20) NULL,
            ToStatus VARCHAR(20) NOT NULL,
            Note NVARCHAR(1000) NOT NULL,
            ActorUserId UNIQUEIDENTIFIER NULL,
            CreatedAtUtc DATETIME2 NOT NULL,
            CONSTRAINT FK_BrandPackagePurchaseHistories_Purchase FOREIGN KEY(PurchaseId) REFERENCES dbo.BrandPackagePurchases(Id),
            CONSTRAINT FK_BrandPackagePurchaseHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_BrandPackagePurchaseHistories_Status CHECK(
                (FromStatus IS NULL OR FromStatus IN ('Pending','Paid','Consumed','Expired','Cancelled')) AND
                ToStatus IN ('Pending','Paid','Consumed','Expired','Cancelled')),
            CONSTRAINT CK_BrandPackagePurchaseHistories_Note CHECK(LEN(LTRIM(RTRIM(Note))) BETWEEN 5 AND 1000)
        );
        CREATE INDEX IX_BrandPackagePurchaseHistories_Purchase_Created ON dbo.BrandPackagePurchaseHistories(PurchaseId, CreatedAtUtc);
    END;

    IF OBJECT_ID(N'dbo.Menus', N'U') IS NOT NULL
        UPDATE dbo.Menus SET Title=N'Tài chính Brand', Icon='fa-solid fa-wallet'
        WHERE Code='MENU_BRAND_BILLING';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

CREATE OR ALTER TRIGGER dbo.TR_BrandPackagePurchaseHistories_AppendOnly
ON dbo.BrandPackagePurchaseHistories
AFTER UPDATE, DELETE
AS
BEGIN
    SET NOCOUNT ON;
    IF EXISTS (SELECT 1 FROM deleted)
        THROW 51201, N'Lịch sử mua gói chỉ được phép ghi thêm.', 1;
END;
GO

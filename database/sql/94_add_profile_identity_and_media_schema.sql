/*
    94_add_profile_identity_and_media_schema.sql
    Đồng bộ các cột hồ sơ mà ứng dụng hiện tại đang sử dụng.

    An toàn khi chạy lại:
    - Chỉ thêm cột/bảng/index khi chưa tồn tại.
    - Không cập nhật hoặc xóa dữ liệu lịch sử.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.Users', N'U') IS NULL
        THROW 51000, N'Không tìm thấy bảng dbo.Users.', 1;

    IF OBJECT_ID(N'dbo.KocProfiles', N'U') IS NULL
        THROW 51001, N'Không tìm thấy bảng dbo.KocProfiles.', 1;

    IF OBJECT_ID(N'dbo.BrandProfiles', N'U') IS NULL
        THROW 51002, N'Không tìm thấy bảng dbo.BrandProfiles.', 1;

    IF COL_LENGTH(N'dbo.Users', N'DateOfBirth') IS NULL
        ALTER TABLE dbo.Users ADD DateOfBirth DATETIME2(7) NULL;

    IF COL_LENGTH(N'dbo.KocProfiles', N'AvatarUrl') IS NULL
        ALTER TABLE dbo.KocProfiles ADD AvatarUrl NVARCHAR(500) NULL;

    IF COL_LENGTH(N'dbo.BrandProfiles', N'TaxCompanyName') IS NULL
        ALTER TABLE dbo.BrandProfiles ADD TaxCompanyName NVARCHAR(255) NULL;

    IF COL_LENGTH(N'dbo.BrandProfiles', N'TaxCode') IS NULL
        ALTER TABLE dbo.BrandProfiles ADD TaxCode NVARCHAR(64) NULL;

    IF COL_LENGTH(N'dbo.BrandProfiles', N'TaxInvoiceAddress') IS NULL
        ALTER TABLE dbo.BrandProfiles ADD TaxInvoiceAddress NVARCHAR(500) NULL;

    IF COL_LENGTH(N'dbo.BrandProfiles', N'BusinessLicenseFileUrl') IS NULL
        ALTER TABLE dbo.BrandProfiles ADD BusinessLicenseFileUrl NVARCHAR(500) NULL;

    IF OBJECT_ID(N'dbo.KocMediaAssets', N'U') IS NULL
    BEGIN
        CREATE TABLE dbo.KocMediaAssets
        (
            Id UNIQUEIDENTIFIER NOT NULL,
            KocProfileId UNIQUEIDENTIFIER NOT NULL,
            MediaType NVARCHAR(50) NOT NULL,
            FileUrl NVARCHAR(500) NOT NULL,
            SortOrder INT NOT NULL
                CONSTRAINT DF_KocMediaAssets_SortOrder DEFAULT (0),
            IsVisible BIT NOT NULL
                CONSTRAINT DF_KocMediaAssets_IsVisible DEFAULT (1),
            CreatedAtUtc DATETIME2(7) NOT NULL
                CONSTRAINT DF_KocMediaAssets_CreatedAtUtc DEFAULT (SYSUTCDATETIME()),
            UpdatedAtUtc DATETIME2(7) NULL,
            CONSTRAINT PK_KocMediaAssets PRIMARY KEY (Id),
            CONSTRAINT FK_KocMediaAssets_KocProfiles
                FOREIGN KEY (KocProfileId) REFERENCES dbo.KocProfiles(Id)
        );
    END;

    IF NOT EXISTS
    (
        SELECT 1
        FROM sys.indexes
        WHERE object_id = OBJECT_ID(N'dbo.KocMediaAssets')
          AND name = N'IX_KocMediaAssets_KocProfileId_MediaType'
    )
    BEGIN
        EXEC(N'CREATE INDEX IX_KocMediaAssets_KocProfileId_MediaType
               ON dbo.KocMediaAssets(KocProfileId, MediaType);');
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'Đã đồng bộ schema hồ sơ người dùng, KOC và Brand.' AS Result;

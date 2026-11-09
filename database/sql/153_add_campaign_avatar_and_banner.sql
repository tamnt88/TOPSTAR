/* TOPSTAR - Campaign avatar and banner. Safe to rerun; deploy before app binaries. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.Campaigns', N'U') IS NULL
    THROW 51000, N'Thiếu bảng dbo.Campaigns.', 1;
GO
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.Campaigns', N'AvatarUrl') IS NULL
    ALTER TABLE dbo.Campaigns ADD AvatarUrl nvarchar(500) NULL;
IF COL_LENGTH(N'dbo.Campaigns', N'BannerUrl') IS NULL
    ALTER TABLE dbo.Campaigns ADD BannerUrl nvarchar(500) NULL;
COMMIT TRANSACTION;
GO
SELECT name, max_length, is_nullable
FROM sys.columns
WHERE object_id = OBJECT_ID(N'dbo.Campaigns') AND name IN (N'AvatarUrl', N'BannerUrl');
GO

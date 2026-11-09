/* TOPSTAR - Page content for SEO configuration. Safe to rerun. */
SET XACT_ABORT ON;
GO
IF OBJECT_ID(N'dbo.PublicPageSeoConfigs', N'U') IS NULL
 THROW 51000, N'Thiếu bảng dbo.PublicPageSeoConfigs. Hãy cập nhật cấu hình SEO trước.', 1;
GO
BEGIN TRANSACTION;
IF COL_LENGTH(N'dbo.PublicPageSeoConfigs', N'PageTitle') IS NULL
 ALTER TABLE dbo.PublicPageSeoConfigs ADD PageTitle nvarchar(200) NULL;
IF COL_LENGTH(N'dbo.PublicPageSeoConfigs', N'ShortDescription') IS NULL
 ALTER TABLE dbo.PublicPageSeoConfigs ADD ShortDescription nvarchar(1000) NULL;
IF COL_LENGTH(N'dbo.PublicPageSeoConfigs', N'PageContent') IS NULL
 ALTER TABLE dbo.PublicPageSeoConfigs ADD PageContent nvarchar(max) NULL;
IF COL_LENGTH(N'dbo.PublicPageSeoConfigs', N'ViewCount') IS NULL
 ALTER TABLE dbo.PublicPageSeoConfigs ADD ViewCount bigint NOT NULL CONSTRAINT DF_PublicPageSeoConfigs_ViewCount DEFAULT 0;
COMMIT TRANSACTION;
GO

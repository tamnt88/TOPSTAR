/* TOPSTAR - Allow multiple industry names. Safe to rerun; preserves existing values. */
SET XACT_ABORT ON;
GO
IF COL_LENGTH(N'dbo.BrandProfiles', N'Industry') IS NULL
 THROW 51000, N'Thiếu cột dbo.BrandProfiles.Industry.', 1;
GO
IF COL_LENGTH(N'dbo.BrandProfiles', N'Industry') BETWEEN 1 AND 3999
 ALTER TABLE dbo.BrandProfiles ALTER COLUMN Industry nvarchar(2000) NULL;
GO

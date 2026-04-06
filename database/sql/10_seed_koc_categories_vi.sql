/*
Seed v10 - Danh mục ngành hàng KOC (Tiếng Việt)
Date: 2026-04-04
Run after: 00_create_topstar_v1.sql
*/

SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

;WITH SourceData AS (
    SELECT N'beauty-skincare' AS Code, N'Làm đẹp & Chăm sóc da' AS Name UNION ALL
    SELECT N'fashion-style', N'Thời trang & Phong cách' UNION ALL
    SELECT N'food-beverage', N'Ẩm thực & Đồ uống' UNION ALL
    SELECT N'tech-gadgets', N'Công nghệ & Thiết bị' UNION ALL
    SELECT N'lifestyle', N'Phong cách sống' UNION ALL
    SELECT N'fitness-health', N'Sức khỏe & Thể chất' UNION ALL
    SELECT N'travel', N'Du lịch & Trải nghiệm' UNION ALL
    SELECT N'education', N'Giáo dục & Học tập' UNION ALL
    SELECT N'gaming', N'Gaming & Esports' UNION ALL
    SELECT N'mom-baby', N'Mẹ & Bé' UNION ALL
    SELECT N'home-decor', N'Nhà cửa & Nội thất' UNION ALL
    SELECT N'automotive', N'Ô tô & Xe máy'
)
MERGE dbo.KocCategories AS target
USING SourceData AS source
ON target.Code = source.Code
WHEN MATCHED THEN
    UPDATE SET
        target.Name = source.Name,
        target.UpdatedAtUtc = SYSUTCDATETIME()
WHEN NOT MATCHED BY TARGET THEN
    INSERT (Id, Code, Name, CreatedAtUtc)
    VALUES (NEWID(), source.Code, source.Name, SYSUTCDATETIME());
GO

COMMIT TRANSACTION;
GO


IF COL_LENGTH('dbo.BrandProfiles', 'LogoUrl') IS NULL
BEGIN
    ALTER TABLE dbo.BrandProfiles ADD LogoUrl nvarchar(500) NULL;
END;

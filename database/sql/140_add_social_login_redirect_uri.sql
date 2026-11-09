IF COL_LENGTH(N'dbo.SocialLoginConfigurations', N'RedirectUri') IS NULL
BEGIN
    ALTER TABLE dbo.SocialLoginConfigurations ADD RedirectUri NVARCHAR(1000) NULL;
END;
GO

UPDATE dbo.SocialLoginConfigurations
SET RedirectUri = CASE Provider
    WHEN N'Google' THEN N'https://localhost:7161/signin-google'
    WHEN N'Facebook' THEN N'https://localhost:7161/signin-facebook'
    ELSE RedirectUri
END,
UpdatedAtUtc = SYSUTCDATETIME()
WHERE Provider IN (N'Google', N'Facebook') AND NULLIF(RedirectUri, N'') IS NULL;

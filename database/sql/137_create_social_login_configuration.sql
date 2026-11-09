IF OBJECT_ID(N'dbo.SocialLoginConfigurations', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SocialLoginConfigurations
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SocialLoginConfigurations PRIMARY KEY,
        Provider NVARCHAR(30) NOT NULL,
        ClientId NVARCHAR(500) NULL,
        ClientSecret NVARCHAR(1000) NULL,
        LoginEnabled BIT NOT NULL CONSTRAINT DF_SocialLoginConfigurations_LoginEnabled DEFAULT (0),
        RegistrationEnabled BIT NOT NULL CONSTRAINT DF_SocialLoginConfigurations_RegistrationEnabled DEFAULT (0),
        BrandEnabled BIT NOT NULL CONSTRAINT DF_SocialLoginConfigurations_BrandEnabled DEFAULT (1),
        KocEnabled BIT NOT NULL CONSTRAINT DF_SocialLoginConfigurations_KocEnabled DEFAULT (1),
        UpdatedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIME2 NOT NULL,
        UpdatedAtUtc DATETIME2 NULL
    );
    CREATE UNIQUE INDEX UX_SocialLoginConfigurations_Provider ON dbo.SocialLoginConfigurations(Provider);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.SocialLoginConfigurations WHERE Provider = N'Google')
    INSERT dbo.SocialLoginConfigurations(Id,Provider,LoginEnabled,RegistrationEnabled,BrandEnabled,KocEnabled,CreatedAtUtc)
    VALUES(NEWID(),N'Google',1,1,1,1,SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM dbo.SocialLoginConfigurations WHERE Provider = N'Facebook')
    INSERT dbo.SocialLoginConfigurations(Id,Provider,LoginEnabled,RegistrationEnabled,BrandEnabled,KocEnabled,CreatedAtUtc)
    VALUES(NEWID(),N'Facebook',1,1,1,1,SYSUTCDATETIME());

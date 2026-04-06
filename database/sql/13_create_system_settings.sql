SET NOCOUNT ON;
GO

IF OBJECT_ID('dbo.SystemEmailAccounts', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemEmailAccounts
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SystemEmailAccounts PRIMARY KEY,
        Name NVARCHAR(200) NOT NULL,
        EmailAddress NVARCHAR(255) NOT NULL,
        DisplayName NVARCHAR(200) NOT NULL,
        SmtpHost NVARCHAR(200) NULL,
        SmtpPort INT NULL,
        SmtpUsername NVARCHAR(255) NULL,
        SmtpPassword NVARCHAR(500) NULL,
        SmtpEnableSsl BIT NOT NULL CONSTRAINT DF_SystemEmailAccounts_SmtpEnableSsl DEFAULT(1),
        ImapHost NVARCHAR(200) NULL,
        ImapPort INT NULL,
        ImapUsername NVARCHAR(255) NULL,
        ImapPassword NVARCHAR(500) NULL,
        ImapEnableSsl BIT NOT NULL CONSTRAINT DF_SystemEmailAccounts_ImapEnableSsl DEFAULT(1),
        IsDefaultSender BIT NOT NULL CONSTRAINT DF_SystemEmailAccounts_IsDefaultSender DEFAULT(0),
        IsDefaultReceiver BIT NOT NULL CONSTRAINT DF_SystemEmailAccounts_IsDefaultReceiver DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_SystemEmailAccounts_IsActive DEFAULT(1),
        ViewCount BIGINT NOT NULL CONSTRAINT DF_SystemEmailAccounts_ViewCount DEFAULT(0),
        IsVisible BIT NOT NULL CONSTRAINT DF_SystemEmailAccounts_IsVisible DEFAULT(1),
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SystemEmailAccounts_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(0) NULL
    );
    CREATE INDEX IX_SystemEmailAccounts_EmailAddress ON dbo.SystemEmailAccounts(EmailAddress);
END
GO

IF OBJECT_ID('dbo.SystemShippingConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemShippingConfigs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SystemShippingConfigs PRIMARY KEY,
        ProviderCode NVARCHAR(100) NOT NULL,
        ProviderName NVARCHAR(200) NOT NULL,
        ApiBaseUrl NVARCHAR(500) NOT NULL,
        ApiToken NVARCHAR(500) NULL,
        ShopId NVARCHAR(100) NULL,
        ClientId NVARCHAR(255) NULL,
        ClientSecret NVARCHAR(500) NULL,
        WebhookUrl NVARCHAR(500) NULL,
        WebhookSecret NVARCHAR(500) NULL,
        IsSandbox BIT NOT NULL CONSTRAINT DF_SystemShippingConfigs_IsSandbox DEFAULT(0),
        IsDefault BIT NOT NULL CONSTRAINT DF_SystemShippingConfigs_IsDefault DEFAULT(0),
        IsActive BIT NOT NULL CONSTRAINT DF_SystemShippingConfigs_IsActive DEFAULT(1),
        ViewCount BIGINT NOT NULL CONSTRAINT DF_SystemShippingConfigs_ViewCount DEFAULT(0),
        IsVisible BIT NOT NULL CONSTRAINT DF_SystemShippingConfigs_IsVisible DEFAULT(1),
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SystemShippingConfigs_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(0) NULL
    );
    CREATE UNIQUE INDEX UX_SystemShippingConfigs_ProviderCode ON dbo.SystemShippingConfigs(ProviderCode);
END
GO

IF OBJECT_ID('dbo.SystemWebsiteConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.SystemWebsiteConfigs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SystemWebsiteConfigs PRIMARY KEY,
        WebsiteName NVARCHAR(200) NOT NULL,
        ShortName NVARCHAR(100) NULL,
        Domain NVARCHAR(255) NULL,
        Address NVARCHAR(500) NULL,
        ContactEmail NVARCHAR(255) NULL,
        Hotline NVARCHAR(100) NULL,
        LogoUrl NVARCHAR(500) NULL,
        FaviconUrl NVARCHAR(500) NULL,
        DefaultShareImageUrl NVARCHAR(500) NULL,
        CopyrightText NVARCHAR(500) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_SystemWebsiteConfigs_IsActive DEFAULT(1),
        ViewCount BIGINT NOT NULL CONSTRAINT DF_SystemWebsiteConfigs_ViewCount DEFAULT(0),
        IsVisible BIT NOT NULL CONSTRAINT DF_SystemWebsiteConfigs_IsVisible DEFAULT(1),
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_SystemWebsiteConfigs_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(0) NULL
    );
END
GO

IF OBJECT_ID('dbo.PublicPageSeoConfigs', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.PublicPageSeoConfigs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PublicPageSeoConfigs PRIMARY KEY,
        PageKey NVARCHAR(120) NOT NULL,
        PagePath NVARCHAR(300) NOT NULL,
        SeoTitle NVARCHAR(300) NULL,
        SeoDescription NVARCHAR(800) NULL,
        SeoKeywords NVARCHAR(1000) NULL,
        CanonicalUrl NVARCHAR(500) NULL,
        OgTitle NVARCHAR(300) NULL,
        OgDescription NVARCHAR(800) NULL,
        OgImageUrl NVARCHAR(500) NULL,
        TwitterTitle NVARCHAR(300) NULL,
        TwitterDescription NVARCHAR(800) NULL,
        TwitterImageUrl NVARCHAR(500) NULL,
        JsonLd NVARCHAR(MAX) NULL,
        IsActive BIT NOT NULL CONSTRAINT DF_PublicPageSeoConfigs_IsActive DEFAULT(1),
        ViewCount BIGINT NOT NULL CONSTRAINT DF_PublicPageSeoConfigs_ViewCount DEFAULT(0),
        IsVisible BIT NOT NULL CONSTRAINT DF_PublicPageSeoConfigs_IsVisible DEFAULT(1),
        CreatedAtUtc DATETIME2(0) NOT NULL CONSTRAINT DF_PublicPageSeoConfigs_CreatedAtUtc DEFAULT(SYSUTCDATETIME()),
        UpdatedAtUtc DATETIME2(0) NULL
    );
    CREATE UNIQUE INDEX UX_PublicPageSeoConfigs_PageKey ON dbo.PublicPageSeoConfigs(PageKey);
    CREATE UNIQUE INDEX UX_PublicPageSeoConfigs_PagePath ON dbo.PublicPageSeoConfigs(PagePath);
END
GO

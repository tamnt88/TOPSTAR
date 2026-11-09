SET XACT_ABORT ON;
BEGIN TRAN;

IF OBJECT_ID(N'dbo.SocialPlatformConnections', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SocialPlatformConnections
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SocialPlatformConnections PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Platform NVARCHAR(40) NOT NULL,
        PlatformUserId NVARCHAR(200) NOT NULL,
        Username NVARCHAR(200) NOT NULL,
        ProfileUrl NVARCHAR(1000) NULL,
        AccessTokenEncrypted NVARCHAR(MAX) NOT NULL,
        RefreshTokenEncrypted NVARCHAR(MAX) NULL,
        AccessTokenExpiresAtUtc DATETIME2 NULL,
        Scopes NVARCHAR(2000) NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_SocialPlatformConnections_Status DEFAULT(N'Active'),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SocialPlatformConnections_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        UpdatedAtUtc DATETIME2 NULL,
        LastSyncedAtUtc DATETIME2 NULL,
        LastError NVARCHAR(2000) NULL,
        CONSTRAINT FK_SocialPlatformConnections_Users FOREIGN KEY (UserId) REFERENCES dbo.Users(Id)
    );

    CREATE UNIQUE INDEX UX_SocialPlatformConnections_UserId_Platform
        ON dbo.SocialPlatformConnections(UserId, Platform);
END;

IF OBJECT_ID(N'dbo.SocialSyncJobs', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SocialSyncJobs
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SocialSyncJobs PRIMARY KEY,
        RequestedByUserId UNIQUEIDENTIFIER NULL,
        RequestedByRole NVARCHAR(30) NOT NULL,
        Scope NVARCHAR(40) NOT NULL,
        KocProfileId UNIQUEIDENTIFIER NULL,
        CampaignId UNIQUEIDENTIFIER NULL,
        Status NVARCHAR(30) NOT NULL CONSTRAINT DF_SocialSyncJobs_Status DEFAULT(N'Pending'),
        ErrorMessage NVARCHAR(2000) NULL,
        RequestedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SocialSyncJobs_RequestedAtUtc DEFAULT SYSUTCDATETIME(),
        StartedAtUtc DATETIME2 NULL,
        FinishedAtUtc DATETIME2 NULL,
        CONSTRAINT FK_SocialSyncJobs_Users FOREIGN KEY (RequestedByUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_SocialSyncJobs_KocProfiles FOREIGN KEY (KocProfileId) REFERENCES dbo.KocProfiles(Id),
        CONSTRAINT FK_SocialSyncJobs_Campaigns FOREIGN KEY (CampaignId) REFERENCES dbo.Campaigns(Id)
    );

    CREATE INDEX IX_SocialSyncJobs_Status_RequestedAtUtc ON dbo.SocialSyncJobs(Status, RequestedAtUtc);
    CREATE INDEX IX_SocialSyncJobs_KocProfileId ON dbo.SocialSyncJobs(KocProfileId);
    CREATE INDEX IX_SocialSyncJobs_CampaignId ON dbo.SocialSyncJobs(CampaignId);
END;

IF OBJECT_ID(N'dbo.SocialProfileMetricSnapshots', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.SocialProfileMetricSnapshots
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_SocialProfileMetricSnapshots PRIMARY KEY,
        KocProfileId UNIQUEIDENTIFIER NOT NULL,
        Platform NVARCHAR(40) NOT NULL,
        SnapshotAtUtc DATETIME2 NOT NULL,
        Followers INT NULL,
        AvgViews INT NULL,
        AvgEngagementRate DECIMAL(10,4) NULL,
        Reach INT NULL,
        Impressions INT NULL,
        Likes BIGINT NULL,
        Comments BIGINT NULL,
        Shares BIGINT NULL,
        PostsCount INT NULL,
        DataSource NVARCHAR(50) NOT NULL CONSTRAINT DF_SocialProfileMetricSnapshots_DataSource DEFAULT(N'OAuthApi'),
        ConfidenceScore INT NOT NULL CONSTRAINT DF_SocialProfileMetricSnapshots_ConfidenceScore DEFAULT(95),
        CreatedAtUtc DATETIME2 NOT NULL CONSTRAINT DF_SocialProfileMetricSnapshots_CreatedAtUtc DEFAULT SYSUTCDATETIME(),
        CONSTRAINT FK_SocialProfileMetricSnapshots_KocProfiles FOREIGN KEY (KocProfileId) REFERENCES dbo.KocProfiles(Id)
    );

    CREATE INDEX IX_SocialProfileMetricSnapshots_Koc_Platform_SnapshotAt
        ON dbo.SocialProfileMetricSnapshots(KocProfileId, Platform, SnapshotAtUtc);
END;

COMMIT TRAN;

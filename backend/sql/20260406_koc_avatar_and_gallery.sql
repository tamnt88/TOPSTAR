-- 20260406_koc_avatar_and_gallery.sql

IF COL_LENGTH('KocProfiles', 'AvatarUrl') IS NULL
BEGIN
    ALTER TABLE [dbo].[KocProfiles]
    ADD [AvatarUrl] NVARCHAR(500) NULL;
END

IF OBJECT_ID(N'dbo.KocMediaAssets', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KocMediaAssets]
    (
        [Id] UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
        [MediaType] NVARCHAR(50) NOT NULL,
        [FileUrl] NVARCHAR(500) NOT NULL,
        [SortOrder] INT NOT NULL CONSTRAINT [DF_KocMediaAssets_SortOrder] DEFAULT 0,
        [IsVisible] BIT NOT NULL CONSTRAINT [DF_KocMediaAssets_IsVisible] DEFAULT 1,
        [CreatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_KocMediaAssets_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2 NULL,
        CONSTRAINT [FK_KocMediaAssets_KocProfile] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
    );

    CREATE INDEX [IX_KocMediaAssets_KocProfileId_MediaType] ON [dbo].[KocMediaAssets]([KocProfileId], [MediaType]);
END

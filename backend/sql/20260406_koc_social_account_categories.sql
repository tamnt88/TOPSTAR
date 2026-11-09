-- 20260406_koc_social_account_categories.sql
-- Store category selections by social account (per-platform) instead of global per-profile.

IF OBJECT_ID(N'dbo.KocSocialAccountCategories', N'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[KocSocialAccountCategories]
    (
        [SocialAccountId] UNIQUEIDENTIFIER NOT NULL,
        [CategoryId] UNIQUEIDENTIFIER NOT NULL,
        [CreatedAtUtc] DATETIME2 NOT NULL CONSTRAINT [DF_KocSocialAccountCategories_CreatedAtUtc] DEFAULT SYSUTCDATETIME(),
        [UpdatedAtUtc] DATETIME2 NULL,
        CONSTRAINT [PK_KocSocialAccountCategories] PRIMARY KEY ([SocialAccountId], [CategoryId]),
        CONSTRAINT [FK_KocSocialAccountCategories_Social] FOREIGN KEY ([SocialAccountId]) REFERENCES [dbo].[KocSocialAccounts]([Id]),
        CONSTRAINT [FK_KocSocialAccountCategories_Category] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[KocCategories]([Id])
    );
END

-- Backfill existing global categories to primary social account first.
;WITH primary_social AS
(
    SELECT
        s.Id AS SocialAccountId,
        s.KocProfileId,
        ROW_NUMBER() OVER (
            PARTITION BY s.KocProfileId
            ORDER BY CASE WHEN s.IsPrimary = 1 THEN 0 ELSE 1 END, s.CreatedAtUtc, s.Id
        ) AS rn
    FROM dbo.KocSocialAccounts s
),
source_rows AS
(
    SELECT ps.SocialAccountId, pc.CategoryId
    FROM primary_social ps
    JOIN dbo.KocProfileCategories pc ON pc.KocProfileId = ps.KocProfileId
    WHERE ps.rn = 1
)
INSERT INTO dbo.KocSocialAccountCategories (SocialAccountId, CategoryId, CreatedAtUtc)
SELECT sr.SocialAccountId, sr.CategoryId, SYSUTCDATETIME()
FROM source_rows sr
WHERE NOT EXISTS
(
    SELECT 1
    FROM dbo.KocSocialAccountCategories t
    WHERE t.SocialAccountId = sr.SocialAccountId
      AND t.CategoryId = sr.CategoryId
);

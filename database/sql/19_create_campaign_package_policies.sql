SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRANSACTION;

IF OBJECT_ID('dbo.CampaignPackagePolicies', 'U') IS NULL
BEGIN
    CREATE TABLE [dbo].[CampaignPackagePolicies] (
        [Id] UNIQUEIDENTIFIER NOT NULL,
        [Code] NVARCHAR(30) NOT NULL,
        [Name] NVARCHAR(150) NOT NULL,
        [MinBudgetAmount] DECIMAL(18,2) NOT NULL,
        [MaxBudgetAmount] DECIMAL(18,2) NULL,
        [BrandSelectionEnabled] BIT NOT NULL,
        [SelectionQuota] INT NULL,
        [IsActive] BIT NOT NULL,
        [CreatedAtUtc] DATETIME2(0) NOT NULL,
        [UpdatedAtUtc] DATETIME2(0) NULL,
        CONSTRAINT [PK_CampaignPackagePolicies] PRIMARY KEY ([Id]),
        CONSTRAINT [UQ_CampaignPackagePolicies_Code] UNIQUE ([Code]),
        CONSTRAINT [CK_CampaignPackagePolicies_Budget] CHECK ([MinBudgetAmount] >= 0 AND ([MaxBudgetAmount] IS NULL OR [MaxBudgetAmount] >= [MinBudgetAmount])),
        CONSTRAINT [CK_CampaignPackagePolicies_Quota] CHECK ([SelectionQuota] IS NULL OR [SelectionQuota] > 0)
    );
END;

IF COL_LENGTH('dbo.Shortlists', 'FinalizedAtUtc') IS NULL
    ALTER TABLE [dbo].[Shortlists] ADD [FinalizedAtUtc] DATETIME2(0) NULL;
IF COL_LENGTH('dbo.Shortlists', 'FinalizedByUserId') IS NULL
    ALTER TABLE [dbo].[Shortlists] ADD [FinalizedByUserId] UNIQUEIDENTIFIER NULL;
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_Shortlists_FinalizedByUser')
    ALTER TABLE [dbo].[Shortlists] ADD CONSTRAINT [FK_Shortlists_FinalizedByUser] FOREIGN KEY ([FinalizedByUserId]) REFERENCES [dbo].[Users]([Id]);

MERGE [dbo].[CampaignPackagePolicies] AS target
USING (VALUES
    ('ASSIGNED_UNDER_5M', N'Admin phân công KOC - dưới 5 triệu', CAST(0 AS DECIMAL(18,2)), CAST(4999999.99 AS DECIMAL(18,2)), CAST(0 AS BIT), CAST(NULL AS INT)),
    ('ASSIGNED_5_TO_20M', N'Admin phân công KOC - từ 5 đến dưới 20 triệu', CAST(5000000 AS DECIMAL(18,2)), CAST(19999999.99 AS DECIMAL(18,2)), CAST(0 AS BIT), CAST(NULL AS INT)),
    ('SELECT_20M', N'Brand chọn tối đa 3 KOC', CAST(20000000 AS DECIMAL(18,2)), CAST(49999999.99 AS DECIMAL(18,2)), CAST(1 AS BIT), 3),
    ('SELECT_50M', N'Brand chọn tối đa 5 KOC', CAST(50000000 AS DECIMAL(18,2)), CAST(99999999.99 AS DECIMAL(18,2)), CAST(1 AS BIT), 5),
    ('SELECT_100M', N'Brand chọn tối đa 10 KOC', CAST(100000000 AS DECIMAL(18,2)), CAST(NULL AS DECIMAL(18,2)), CAST(1 AS BIT), 10)
) AS source ([Code],[Name],[MinBudgetAmount],[MaxBudgetAmount],[BrandSelectionEnabled],[SelectionQuota])
ON target.[Code] = source.[Code]
WHEN MATCHED THEN UPDATE SET [Name]=source.[Name], [MinBudgetAmount]=source.[MinBudgetAmount], [MaxBudgetAmount]=source.[MaxBudgetAmount], [BrandSelectionEnabled]=source.[BrandSelectionEnabled], [SelectionQuota]=source.[SelectionQuota], [IsActive]=1, [UpdatedAtUtc]=SYSUTCDATETIME()
WHEN NOT MATCHED THEN INSERT ([Id],[Code],[Name],[MinBudgetAmount],[MaxBudgetAmount],[BrandSelectionEnabled],[SelectionQuota],[IsActive],[CreatedAtUtc]) VALUES (NEWID(),source.[Code],source.[Name],source.[MinBudgetAmount],source.[MaxBudgetAmount],source.[BrandSelectionEnabled],source.[SelectionQuota],1,SYSUTCDATETIME());

COMMIT TRANSACTION;

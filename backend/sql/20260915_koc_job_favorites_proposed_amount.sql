SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Applications', 'ProposedAmount') IS NULL
BEGIN
    ALTER TABLE dbo.Applications ADD ProposedAmount decimal(18,2) NULL;
END;

IF OBJECT_ID('dbo.KocCampaignFavorites', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.KocCampaignFavorites
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_KocCampaignFavorites PRIMARY KEY,
        KocProfileId uniqueidentifier NOT NULL,
        CampaignId uniqueidentifier NOT NULL,
        CreatedAtUtc datetime2 NOT NULL,
        CONSTRAINT FK_KocCampaignFavorites_KocProfiles_KocProfileId FOREIGN KEY (KocProfileId) REFERENCES dbo.KocProfiles(Id) ON DELETE CASCADE,
        CONSTRAINT FK_KocCampaignFavorites_Campaigns_CampaignId FOREIGN KEY (CampaignId) REFERENCES dbo.Campaigns(Id) ON DELETE CASCADE
    );
    CREATE UNIQUE INDEX IX_KocCampaignFavorites_KocProfileId_CampaignId ON dbo.KocCampaignFavorites(KocProfileId, CampaignId);
END;

COMMIT TRANSACTION;

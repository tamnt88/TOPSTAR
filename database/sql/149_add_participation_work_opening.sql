IF COL_LENGTH('dbo.Participations', 'WorkOpenedAtUtc') IS NULL
    ALTER TABLE dbo.Participations ADD WorkOpenedAtUtc datetime2 NULL;
GO
IF COL_LENGTH('dbo.Participations', 'WorkOpenedByUserId') IS NULL
    ALTER TABLE dbo.Participations ADD WorkOpenedByUserId uniqueidentifier NULL;
GO
UPDATE p
SET WorkOpenedAtUtc = COALESCE(p.UpdatedAtUtc, p.ConfirmedAtUtc, p.CreatedAtUtc)
FROM dbo.Participations p
JOIN dbo.Campaigns c ON c.Id = p.CampaignId
WHERE p.WorkOpenedAtUtc IS NULL
  AND p.Status IN ('Confirmed', 'Completed')
  AND c.Status IN ('InProgress', 'Completed');
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Participations_Campaign_WorkOpened' AND object_id = OBJECT_ID('dbo.Participations'))
    CREATE INDEX IX_Participations_Campaign_WorkOpened ON dbo.Participations(CampaignId, WorkOpenedAtUtc) INCLUDE(Status, KocProfileId);
GO

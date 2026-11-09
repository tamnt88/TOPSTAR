/*
 Refresh deterministic seed campaigns so KOC discovery always has future jobs.
 Safe to rerun in a development database.
*/
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Today DATE = CONVERT(DATE, SYSUTCDATETIME());
    DECLARE @EligibleCount INT =
    (
        SELECT COUNT(*)
        FROM dbo.Campaigns
        WHERE Title LIKE N'Chiến dịch Seed %'
          AND Status = 'Published'
          AND IsVisible = 1
          AND StartDate > @Today
          AND (EndDate IS NULL OR EndDate >= StartDate)
    );

    IF @EligibleCount < 60
    BEGIN
        DECLARE @Targets TABLE
        (
            CampaignId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
            RowNo INT NOT NULL
        );

        INSERT @Targets(CampaignId, RowNo)
        SELECT Id, ROW_NUMBER() OVER (ORDER BY CreatedAtUtc DESC, Id)
        FROM dbo.Campaigns
        WHERE Title LIKE N'Chiến dịch Seed %';

        DECLARE @Changed TABLE
        (
            CampaignId UNIQUEIDENTIFIER NOT NULL,
            FromStatus NVARCHAR(50) NULL,
            ToStatus NVARCHAR(50) NOT NULL
        );

        UPDATE campaign
        SET campaign.Status = 'Published',
            campaign.IsVisible = 1,
            campaign.StartDate = DATEADD(DAY, 1 + ((target.RowNo - 1) % 45), @Today),
            campaign.EndDate = DATEADD(DAY, 31 + ((target.RowNo - 1) % 45), @Today),
            campaign.ReviewNote = N'Dữ liệu seed đang tuyển dành cho màn hình cơ hội KOC.',
            campaign.UpdatedAtUtc = SYSUTCDATETIME()
        OUTPUT inserted.Id, deleted.Status, inserted.Status
            INTO @Changed(CampaignId, FromStatus, ToStatus)
        FROM dbo.Campaigns campaign
        JOIN @Targets target ON target.CampaignId = campaign.Id
        WHERE target.RowNo <= 120;

        INSERT dbo.CampaignStatusHistories
            (Id, CampaignId, FromStatus, ToStatus, Note, ActorUserId, CreatedAtUtc)
        SELECT NEWID(), changed.CampaignId, changed.FromStatus, changed.ToStatus,
               N'Làm mới dữ liệu seed để kiểm thử danh sách cơ hội KOC.', NULL, SYSUTCDATETIME()
        FROM @Changed changed
        WHERE ISNULL(changed.FromStatus, '') <> changed.ToStatus;
    END;

    COMMIT TRANSACTION;

    SELECT
        COUNT(*) AS EligibleFutureCampaigns,
        MIN(StartDate) AS NearestStartDate,
        MAX(StartDate) AS FurthestStartDate
    FROM dbo.Campaigns
    WHERE Title LIKE N'Chiến dịch Seed %'
      AND Status = 'Published'
      AND IsVisible = 1
      AND StartDate > @Today
      AND (EndDate IS NULL OR EndDate >= StartDate);
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

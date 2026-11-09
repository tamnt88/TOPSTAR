SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID('dbo.Participations', 'U') IS NULL
    THROW 51000, N'Không tìm thấy bảng dbo.Participations. Dừng triển khai.', 1;

IF OBJECT_ID('dbo.Campaigns', 'U') IS NULL
    THROW 51000, N'Không tìm thấy bảng dbo.Campaigns. Dừng triển khai.', 1;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Participations', 'WorkOpenedAtUtc') IS NULL
        ALTER TABLE dbo.Participations ADD WorkOpenedAtUtc datetime2 NULL;

    IF COL_LENGTH('dbo.Participations', 'WorkOpenedByUserId') IS NULL
        ALTER TABLE dbo.Participations ADD WorkOpenedByUserId uniqueidentifier NULL;

    IF NOT EXISTS (
        SELECT 1
        FROM sys.check_constraints
        WHERE name = 'CK_Participations_Status'
          AND parent_object_id = OBJECT_ID('dbo.Participations')
          AND definition LIKE '%Invited%'
          AND definition LIKE '%Declined%'
          AND definition LIKE '%Removed%'
    )
    BEGIN
        IF EXISTS (
            SELECT 1 FROM sys.check_constraints
            WHERE name = 'CK_Participations_Status'
              AND parent_object_id = OBJECT_ID('dbo.Participations')
        )
            ALTER TABLE dbo.Participations DROP CONSTRAINT CK_Participations_Status;

        ALTER TABLE dbo.Participations WITH CHECK
            ADD CONSTRAINT CK_Participations_Status
            CHECK (Status IN ('Invited', 'Confirmed', 'Declined', 'Removed', 'Completed', 'Cancelled'));

        ALTER TABLE dbo.Participations CHECK CONSTRAINT CK_Participations_Status;
    END;

    EXEC sys.sp_executesql N'
        UPDATE p
        SET WorkOpenedAtUtc = COALESCE(p.UpdatedAtUtc, p.ConfirmedAtUtc, p.CreatedAtUtc)
        FROM dbo.Participations p
        JOIN dbo.Campaigns c ON c.Id = p.CampaignId
        WHERE p.WorkOpenedAtUtc IS NULL
          AND p.Status IN (''Confirmed'', ''Completed'')
          AND c.Status IN (''InProgress'', ''Completed'');';

    IF NOT EXISTS (
        SELECT 1 FROM sys.indexes
        WHERE name = 'IX_Participations_Campaign_WorkOpened'
          AND object_id = OBJECT_ID('dbo.Participations')
    )
        EXEC sys.sp_executesql N'
            CREATE INDEX IX_Participations_Campaign_WorkOpened
            ON dbo.Participations(CampaignId, WorkOpenedAtUtc)
            INCLUDE(Status, KocProfileId);';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    COL_LENGTH('dbo.Participations', 'WorkOpenedAtUtc') AS WorkOpenedAtUtcBytes,
    COL_LENGTH('dbo.Participations', 'WorkOpenedByUserId') AS WorkOpenedByUserIdBytes,
    (SELECT definition FROM sys.check_constraints
     WHERE name = 'CK_Participations_Status'
       AND parent_object_id = OBJECT_ID('dbo.Participations')) AS ParticipationStatusConstraint,
    (SELECT COUNT(*) FROM sys.indexes
     WHERE name = 'IX_Participations_Campaign_WorkOpened'
       AND object_id = OBJECT_ID('dbo.Participations')) AS WorkOpenedIndexCount;
/* Also deploy invitation statuses for the append-only history table. */
GO
SET XACT_ABORT ON;
IF OBJECT_ID(N'dbo.ParticipationStatusHistories', N'U') IS NULL
    THROW 51000, N'Thiếu bảng dbo.ParticipationStatusHistories. Hãy chạy script 29 trước.', 1;

BEGIN TRY
    BEGIN TRANSACTION;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ParticipationStatusHistories') AND name = N'CK_ParticipationStatusHistories_From')
        ALTER TABLE dbo.ParticipationStatusHistories DROP CONSTRAINT CK_ParticipationStatusHistories_From;
    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID(N'dbo.ParticipationStatusHistories') AND name = N'CK_ParticipationStatusHistories_To')
        ALTER TABLE dbo.ParticipationStatusHistories DROP CONSTRAINT CK_ParticipationStatusHistories_To;

    ALTER TABLE dbo.ParticipationStatusHistories WITH CHECK ADD CONSTRAINT CK_ParticipationStatusHistories_From
        CHECK (FromStatus IS NULL OR FromStatus IN (N'Invited', N'Confirmed', N'Declined', N'Removed', N'Completed', N'Cancelled'));
    ALTER TABLE dbo.ParticipationStatusHistories WITH CHECK ADD CONSTRAINT CK_ParticipationStatusHistories_To
        CHECK (ToStatus IN (N'Invited', N'Confirmed', N'Declined', N'Removed', N'Completed', N'Cancelled'));
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

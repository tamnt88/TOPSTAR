/* TOPSTAR - Allow invitation lifecycle in participation history. Safe to rerun.
   Preserves all existing history; does not confirm KOC or send invitations. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
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

SELECT name, definition, is_disabled, is_not_trusted
FROM sys.check_constraints
WHERE parent_object_id = OBJECT_ID(N'dbo.ParticipationStatusHistories')
  AND name IN (N'CK_ParticipationStatusHistories_From', N'CK_ParticipationStatusHistories_To');
GO


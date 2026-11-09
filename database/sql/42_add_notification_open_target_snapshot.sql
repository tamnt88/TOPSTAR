SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.NotificationEvents','U') IS NULL OR COL_LENGTH('dbo.Notifications','SourceEntityId') IS NULL
        THROW 51046, 'Run migration 41_add_notification_open_history.sql first.', 1;

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.NotificationEvents') AND name='CK_NotificationEvents_Type' AND definition LIKE '%Opened%')
        THROW 51047, 'Migration 41 is incomplete: Opened event type is not allowed.', 1;

    IF COL_LENGTH('dbo.NotificationEvents','TargetUrlSnapshot') IS NULL
        EXEC(N'ALTER TABLE dbo.NotificationEvents ADD TargetUrlSnapshot NVARCHAR(500) NULL;');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.NotificationEvents') AND name='CK_NotificationEvents_OpenTarget')
        EXEC(N'ALTER TABLE dbo.NotificationEvents WITH CHECK ADD CONSTRAINT CK_NotificationEvents_OpenTarget
               CHECK ((EventType=''Opened'' AND TargetUrlSnapshot IS NOT NULL
                       AND LEFT(TargetUrlSnapshot,1)=''/'' AND LEFT(TargetUrlSnapshot,2)<>''//''
                       AND CHARINDEX(NCHAR(92),TargetUrlSnapshot)=0
                       AND CHARINDEX(CHAR(9),TargetUrlSnapshot)=0
                       AND CHARINDEX(CHAR(10),TargetUrlSnapshot)=0
                       AND CHARINDEX(CHAR(13),TargetUrlSnapshot)=0)
                      OR (EventType<>''Opened'' AND TargetUrlSnapshot IS NULL));');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

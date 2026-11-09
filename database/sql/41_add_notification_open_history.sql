SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.NotificationEvents','U') IS NULL OR COL_LENGTH('dbo.Notifications','SourceEntityId') IS NULL
        THROW 51045, 'Run migration 40_add_notification_source_correlation.sql first.', 1;

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.NotificationEvents') AND name='CK_NotificationEvents_Type')
        ALTER TABLE dbo.NotificationEvents DROP CONSTRAINT CK_NotificationEvents_Type;

    ALTER TABLE dbo.NotificationEvents WITH CHECK ADD CONSTRAINT CK_NotificationEvents_Type
        CHECK (EventType IN ('Created','Read','Opened','Hidden','Restored'));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

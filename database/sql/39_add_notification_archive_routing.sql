SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.Notifications', 'U') IS NULL OR OBJECT_ID('dbo.NotificationEvents', 'U') IS NULL
        THROW 51042, 'Run migration 38_create_notification_inbox_history.sql first.', 1;

    IF COL_LENGTH('dbo.Notifications', 'HiddenAtUtc') IS NULL
        EXEC(N'ALTER TABLE dbo.Notifications ADD HiddenAtUtc DATETIME2(7) NULL;');

    IF COL_LENGTH('dbo.Notifications', 'TargetUrl') IS NULL
        EXEC(N'ALTER TABLE dbo.Notifications ADD TargetUrl NVARCHAR(500) NULL;');

    EXEC(N'UPDATE dbo.Notifications
           SET HiddenAtUtc = COALESCE(UpdatedAtUtc, CreatedAtUtc)
           WHERE IsVisible = 0 AND HiddenAtUtc IS NULL;');

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.NotificationEvents') AND name = 'CK_NotificationEvents_Type')
        ALTER TABLE dbo.NotificationEvents DROP CONSTRAINT CK_NotificationEvents_Type;
    ALTER TABLE dbo.NotificationEvents WITH CHECK ADD CONSTRAINT CK_NotificationEvents_Type
        CHECK (EventType IN ('Created', 'Read', 'Hidden', 'Restored'));

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Notifications') AND name = 'CK_Notifications_VisibilityHistory')
        EXEC(N'ALTER TABLE dbo.Notifications WITH CHECK ADD CONSTRAINT CK_Notifications_VisibilityHistory
               CHECK ((IsVisible = 1 AND HiddenAtUtc IS NULL) OR (IsVisible = 0 AND HiddenAtUtc IS NOT NULL));');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id = OBJECT_ID('dbo.Notifications') AND name = 'CK_Notifications_TargetUrl')
        EXEC(N'ALTER TABLE dbo.Notifications WITH CHECK ADD CONSTRAINT CK_Notifications_TargetUrl
               CHECK (TargetUrl IS NULL OR (LEFT(TargetUrl, 1) = ''/'' AND LEFT(TargetUrl, 2) <> ''//''));');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id = OBJECT_ID('dbo.Notifications') AND name = 'IX_Notifications_User_Visible_Created')
        EXEC(N'CREATE INDEX IX_Notifications_User_Visible_Created
               ON dbo.Notifications(UserId, IsVisible, CreatedAtUtc DESC)
               INCLUDE(IsRead, Type, Title, ReadAtUtc, HiddenAtUtc, TargetUrl);');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

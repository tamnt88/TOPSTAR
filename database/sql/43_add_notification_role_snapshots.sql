SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.NotificationEvents','TargetUrlSnapshot') IS NULL OR COL_LENGTH('dbo.Notifications','SourceEntityId') IS NULL
        THROW 51048, 'Run migration 42_add_notification_open_target_snapshot.sql first.', 1;

    IF COL_LENGTH('dbo.Notifications','AudienceRole') IS NULL
        EXEC(N'ALTER TABLE dbo.Notifications ADD AudienceRole NVARCHAR(10) NULL;');
    IF COL_LENGTH('dbo.NotificationEvents','ActorRoleSnapshot') IS NULL
        EXEC(N'ALTER TABLE dbo.NotificationEvents ADD ActorRoleSnapshot NVARCHAR(10) NULL;');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Notifications') AND name='CK_Notifications_AudienceRole')
        EXEC(N'ALTER TABLE dbo.Notifications WITH CHECK ADD CONSTRAINT CK_Notifications_AudienceRole
               CHECK (AudienceRole IS NULL OR AudienceRole IN (''ADMIN'',''BRAND'',''KOC''));');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.NotificationEvents') AND name='CK_NotificationEvents_ActorRole')
        EXEC(N'ALTER TABLE dbo.NotificationEvents WITH CHECK ADD CONSTRAINT CK_NotificationEvents_ActorRole
               CHECK ((EventType=''Created'' AND ActorRoleSnapshot IS NULL) OR
                      (EventType<>''Created'' AND ActorRoleSnapshot IN (''ADMIN'',''BRAND'',''KOC'')));');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Notifications') AND name='IX_Notifications_User_Audience_Visible_Created')
        EXEC(N'CREATE INDEX IX_Notifications_User_Audience_Visible_Created
               ON dbo.Notifications(UserId,AudienceRole,IsVisible,CreatedAtUtc DESC) INCLUDE(IsRead,Type,Title,ReadAtUtc,HiddenAtUtc);');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_Notifications_RequireAudienceRole
           ON dbo.Notifications AFTER INSERT AS
           BEGIN
             SET NOCOUNT ON;
             IF EXISTS(SELECT 1 FROM inserted WHERE AudienceRole IS NULL)
               THROW 51049, ''New notifications require AudienceRole.'', 1;
           END;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_Notifications_ImmutableIdentity
           ON dbo.Notifications AFTER UPDATE AS
           BEGIN
             SET NOCOUNT ON;
             IF EXISTS (
                 SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                 WHERE i.UserId<>d.UserId OR i.Type<>d.Type OR i.Title<>d.Title OR i.Body<>d.Body
                    OR ISNULL(i.AudienceRole,N'''')<>ISNULL(d.AudienceRole,N'''')
                    OR ISNULL(i.TargetUrl,N'''')<>ISNULL(d.TargetUrl,N'''')
                    OR ISNULL(i.SourceEntityType,N'''')<>ISNULL(d.SourceEntityType,N'''')
                    OR ISNULL(CONVERT(NVARCHAR(36),i.SourceEntityId),N'''')<>ISNULL(CONVERT(NVARCHAR(36),d.SourceEntityId),N''''))
               THROW 51044, ''Notification identity, content, audience and source are immutable.'', 1;
           END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

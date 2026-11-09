SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.Notifications', 'U') IS NULL OR COL_LENGTH('dbo.Notifications', 'TargetUrl') IS NULL
        THROW 51043, 'Run migration 39_add_notification_archive_routing.sql first.', 1;

    IF COL_LENGTH('dbo.Notifications', 'SourceEntityType') IS NULL
        EXEC(N'ALTER TABLE dbo.Notifications ADD SourceEntityType NVARCHAR(50) NULL;');

    IF COL_LENGTH('dbo.Notifications', 'SourceEntityId') IS NULL
        EXEC(N'ALTER TABLE dbo.Notifications ADD SourceEntityId UNIQUEIDENTIFIER NULL;');

    IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Notifications') AND name='CK_Notifications_TargetUrl')
        ALTER TABLE dbo.Notifications DROP CONSTRAINT CK_Notifications_TargetUrl;

    EXEC(N'ALTER TABLE dbo.Notifications WITH CHECK ADD CONSTRAINT CK_Notifications_TargetUrl
           CHECK (TargetUrl IS NULL OR
                 (LEFT(TargetUrl,1)=''/'' AND LEFT(TargetUrl,2)<>''//''
                  AND CHARINDEX(NCHAR(92),TargetUrl)=0
                  AND CHARINDEX(CHAR(9),TargetUrl)=0
                  AND CHARINDEX(CHAR(10),TargetUrl)=0
                  AND CHARINDEX(CHAR(13),TargetUrl)=0));');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Notifications') AND name='CK_Notifications_SourcePair')
        EXEC(N'ALTER TABLE dbo.Notifications WITH CHECK ADD CONSTRAINT CK_Notifications_SourcePair
               CHECK ((SourceEntityType IS NULL AND SourceEntityId IS NULL) OR
                      (LEN(LTRIM(RTRIM(SourceEntityType))) BETWEEN 1 AND 50 AND SourceEntityId IS NOT NULL));');

    IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Notifications') AND name='IX_Notifications_Source')
        EXEC(N'CREATE INDEX IX_Notifications_Source ON dbo.Notifications(SourceEntityType,SourceEntityId,CreatedAtUtc DESC)
               WHERE SourceEntityId IS NOT NULL;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_Notifications_ImmutableIdentity
           ON dbo.Notifications AFTER UPDATE AS
           BEGIN
             SET NOCOUNT ON;
             IF EXISTS (
                 SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                 WHERE i.UserId<>d.UserId OR i.Type<>d.Type OR i.Title<>d.Title OR i.Body<>d.Body
                    OR ISNULL(i.TargetUrl,N'''')<>ISNULL(d.TargetUrl,N'''')
                    OR ISNULL(i.SourceEntityType,N'''')<>ISNULL(d.SourceEntityType,N'''')
                    OR ISNULL(CONVERT(NVARCHAR(36),i.SourceEntityId),N'''')<>ISNULL(CONVERT(NVARCHAR(36),d.SourceEntityId),N''''))
               THROW 51044, ''Notification identity, content and source are immutable.'', 1;
           END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

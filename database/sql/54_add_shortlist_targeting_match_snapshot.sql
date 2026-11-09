SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.ShortlistItems','U') IS NULL THROW 51110,'ShortlistItems does not exist.',1;
 IF COL_LENGTH('dbo.ShortlistItems','TargetingMatchScoreSnapshot') IS NULL ALTER TABLE dbo.ShortlistItems ADD TargetingMatchScoreSnapshot DECIMAL(5,2) NULL;
 IF COL_LENGTH('dbo.ShortlistItems','TargetingMatchDetailsJson') IS NULL ALTER TABLE dbo.ShortlistItems ADD TargetingMatchDetailsJson NVARCHAR(MAX) NULL;
 IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.ShortlistItems') AND name='CK_ShortlistItems_TargetingMatchScore') EXEC(N'ALTER TABLE dbo.ShortlistItems WITH CHECK ADD CONSTRAINT CK_ShortlistItems_TargetingMatchScore CHECK(TargetingMatchScoreSnapshot IS NULL OR TargetingMatchScoreSnapshot BETWEEN 0 AND 100);');
 IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.ShortlistItems') AND name='CK_ShortlistItems_TargetingMatchJson') EXEC(N'ALTER TABLE dbo.ShortlistItems WITH CHECK ADD CONSTRAINT CK_ShortlistItems_TargetingMatchJson CHECK((TargetingMatchScoreSnapshot IS NULL AND TargetingMatchDetailsJson IS NULL) OR (TargetingMatchScoreSnapshot IS NOT NULL AND ISJSON(TargetingMatchDetailsJson)=1));');
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.ShortlistItems') AND name='IX_ShortlistItems_MatchScore') EXEC(N'CREATE INDEX IX_ShortlistItems_MatchScore ON dbo.ShortlistItems(ShortlistId,TargetingMatchScoreSnapshot DESC) INCLUDE(ApplicationId,SelectionSource);');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ShortlistItems_ImmutableTargetingSnapshot ON dbo.ShortlistItems AFTER UPDATE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON i.ShortlistId=d.ShortlistId AND i.ApplicationId=d.ApplicationId WHERE ISNULL(i.TargetingMatchScoreSnapshot,-1)<>ISNULL(d.TargetingMatchScoreSnapshot,-1) OR ISNULL(i.TargetingMatchDetailsJson,N'''')<>ISNULL(d.TargetingMatchDetailsJson,N'''')) THROW 51111,''Shortlist targeting snapshot is immutable.'',1; END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

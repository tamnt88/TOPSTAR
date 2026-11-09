SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.AuditLogs','U') IS NULL
  THROW 51085,'AuditLogs table is required.',1;
 IF COL_LENGTH('dbo.AuditLogs','ActorEmailSnapshot') IS NULL
  ALTER TABLE dbo.AuditLogs ADD ActorEmailSnapshot NVARCHAR(256) NULL;
 IF COL_LENGTH('dbo.AuditLogs','ActorRoleSnapshot') IS NULL
  ALTER TABLE dbo.AuditLogs ADD ActorRoleSnapshot NVARCHAR(200) NULL;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AuditLogs_AppendOnly ON dbo.AuditLogs
 AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;
  THROW 51084,''Audit log is append-only and cannot be changed or deleted.'',1;
 END');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

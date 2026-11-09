SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;

 IF OBJECT_ID('dbo.AuditLogs','U') IS NULL
  THROW 51083, 'AuditLogs table is required. Run the base schema first.', 1;

 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AuditLogs') AND name='IX_AuditLogs_CreatedAtUtc')
  CREATE INDEX IX_AuditLogs_CreatedAtUtc ON dbo.AuditLogs(CreatedAtUtc DESC,Id DESC)
   INCLUDE(ActorUserId,EntityName,EntityId,ActionName);

 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.AuditLogs') AND name='IX_AuditLogs_Entity_Action_Created')
  CREATE INDEX IX_AuditLogs_Entity_Action_Created ON dbo.AuditLogs(EntityName,ActionName,CreatedAtUtc DESC)
   INCLUDE(ActorUserId,EntityId);

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AuditLogs_AppendOnly ON dbo.AuditLogs
 AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;
  THROW 51084,''Audit log is append-only and cannot be changed or deleted.'',1;
 END');

 DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
 DECLARE @parent UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus
  WHERE Code IN('MENU_ADMIN_SETTINGS','ADMIN_SETTINGS')
  ORDER BY CASE WHEN Code='MENU_ADMIN_SETTINGS' THEN 0 ELSE 1 END);
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_AUDIT_LOGS')
  INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
  VALUES(NEWID(),'MENU_ADMIN_AUDIT_LOGS',N'Nhật ký hệ thống','/Admin/AuditLogs',
   'fa-solid fa-clock-rotate-left',@parent,10,1,@now);

 DECLARE @role UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN');
 DECLARE @menu UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_AUDIT_LOGS');
 IF @role IS NOT NULL AND @menu IS NOT NULL
    AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@role AND MenuId=@menu)
  INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@role,@menu,@now);

 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

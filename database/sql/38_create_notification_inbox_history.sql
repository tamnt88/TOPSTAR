SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.NotificationEvents','U') IS NULL
 BEGIN
  CREATE TABLE dbo.NotificationEvents(Id UNIQUEIDENTIFIER NOT NULL,NotificationId UNIQUEIDENTIFIER NOT NULL,EventType NVARCHAR(20) NOT NULL,ActorUserId UNIQUEIDENTIFIER NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT PK_NotificationEvents PRIMARY KEY(Id),CONSTRAINT FK_NotificationEvents_Notification FOREIGN KEY(NotificationId) REFERENCES dbo.Notifications(Id),CONSTRAINT FK_NotificationEvents_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_NotificationEvents_Type CHECK(EventType IN('Created','Read','Hidden')));
  CREATE INDEX IX_NotificationEvents_Notification_Created ON dbo.NotificationEvents(NotificationId,CreatedAtUtc);
 END;
 INSERT INTO dbo.NotificationEvents(Id,NotificationId,EventType,ActorUserId,CreatedAtUtc) SELECT NEWID(),n.Id,'Created',NULL,n.CreatedAtUtc FROM dbo.Notifications n WHERE NOT EXISTS(SELECT 1 FROM dbo.NotificationEvents e WHERE e.NotificationId=n.Id AND e.EventType='Created');
 INSERT INTO dbo.NotificationEvents(Id,NotificationId,EventType,ActorUserId,CreatedAtUtc) SELECT NEWID(),n.Id,'Read',n.UserId,COALESCE(n.ReadAtUtc,n.UpdatedAtUtc,n.CreatedAtUtc) FROM dbo.Notifications n WHERE n.IsRead=1 AND NOT EXISTS(SELECT 1 FROM dbo.NotificationEvents e WHERE e.NotificationId=n.Id AND e.EventType='Read');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_NotificationEvents_AppendOnly ON dbo.NotificationEvents INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51041, ''NotificationEvents is append-only.'', 1; END;');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_Notifications_CaptureCreated ON dbo.Notifications AFTER INSERT AS BEGIN SET NOCOUNT ON; INSERT INTO dbo.NotificationEvents(Id,NotificationId,EventType,ActorUserId,CreatedAtUtc) SELECT NEWID(),i.Id,''Created'',NULL,i.CreatedAtUtc FROM inserted i WHERE NOT EXISTS(SELECT 1 FROM dbo.NotificationEvents e WHERE e.NotificationId=i.Id AND e.EventType=''Created''); END;');
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_NOTIFICATIONS') INSERT INTO dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)VALUES(NEWID(),'MENU_ADMIN_NOTIFICATIONS',N'Thông báo','/Admin/Notifications','fa-solid fa-bell',NULL,9,1,SYSUTCDATETIME());
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_BRAND_NOTIFICATIONS') INSERT INTO dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)VALUES(NEWID(),'MENU_BRAND_NOTIFICATIONS',N'Thông báo','/Brand/Notifications','fa-solid fa-bell',NULL,8,1,SYSUTCDATETIME());
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_KOC_NOTIFICATIONS') INSERT INTO dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)VALUES(NEWID(),'MENU_KOC_NOTIFICATIONS',N'Thông báo','/Koc/Notifications','fa-solid fa-bell',NULL,8,1,SYSUTCDATETIME());
 DECLARE @Admin UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN'),@Brand UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='BRAND'),@Koc UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='KOC');
 INSERT INTO dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) SELECT v.RoleId,m.Id,SYSUTCDATETIME() FROM(VALUES(@Admin,'MENU_ADMIN_NOTIFICATIONS'),(@Brand,'MENU_BRAND_NOTIFICATIONS'),(@Koc,'MENU_KOC_NOTIFICATIONS'))v(RoleId,Code) JOIN dbo.Menus m ON m.Code=v.Code WHERE v.RoleId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=v.RoleId AND rm.MenuId=m.Id);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

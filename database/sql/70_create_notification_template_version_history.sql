SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.NotificationTemplates','U') IS NULL
 BEGIN
  CREATE TABLE dbo.NotificationTemplates(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_NotificationTemplates PRIMARY KEY,Type NVARCHAR(50) NOT NULL,AudienceRole NVARCHAR(10) NOT NULL,
   TitleTemplate NVARCHAR(200) NOT NULL,BodyTemplate NVARCHAR(1000) NOT NULL,IsActive BIT NOT NULL,
   CreatedByUserId UNIQUEIDENTIFIER NOT NULL,UpdatedByUserId UNIQUEIDENTIFIER NULL,CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NULL,
   CONSTRAINT UQ_NotificationTemplates_Type_Role UNIQUE(Type,AudienceRole),
   CONSTRAINT FK_NotificationTemplates_Creator FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT FK_NotificationTemplates_Updater FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_NotificationTemplates_Role CHECK(AudienceRole IN('ADMIN','BRAND','KOC')),
   CONSTRAINT CK_NotificationTemplates_Text CHECK(LEN(LTRIM(RTRIM(TitleTemplate))) BETWEEN 1 AND 200 AND LEN(LTRIM(RTRIM(BodyTemplate))) BETWEEN 1 AND 1000));
 END;
 IF OBJECT_ID('dbo.NotificationTemplateHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.NotificationTemplateHistories(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_NotificationTemplateHistories PRIMARY KEY,TemplateId UNIQUEIDENTIFIER NOT NULL,
   TitleTemplate NVARCHAR(200) NOT NULL,BodyTemplate NVARCHAR(1000) NOT NULL,IsActive BIT NOT NULL,Reason NVARCHAR(1000) NOT NULL,
   ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_NotificationTemplateHistories_Template FOREIGN KEY(TemplateId) REFERENCES dbo.NotificationTemplates(Id),
   CONSTRAINT FK_NotificationTemplateHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_NotificationTemplateHistories_Text CHECK(LEN(LTRIM(RTRIM(TitleTemplate))) BETWEEN 1 AND 200 AND LEN(LTRIM(RTRIM(BodyTemplate))) BETWEEN 1 AND 1000),
   CONSTRAINT CK_NotificationTemplateHistories_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 5 AND 1000));
  CREATE INDEX IX_NotificationTemplateHistories_Template_Created ON dbo.NotificationTemplateHistories(TemplateId,CreatedAtUtc);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_NotificationTemplateHistories_AppendOnly ON dbo.NotificationTemplateHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;THROW 51000,N''Version history của notification template chỉ được ghi thêm.'',1;END;');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_NotificationTemplates_ProtectIdentity ON dbo.NotificationTemplates AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;IF NOT EXISTS(SELECT 1 FROM inserted) THROW 51000,N''Không được xóa notification template; hãy tắt IsActive.'',1;IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.Type<>d.Type OR i.AudienceRole<>d.AudienceRole OR i.CreatedByUserId<>d.CreatedByUserId OR i.CreatedAtUtc<>d.CreatedAtUtc) THROW 51000,N''Type, audience và thông tin tạo template là bất biến.'',1;END;');
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME(),@parent UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code IN('MENU_ADMIN_SETTINGS','ADMIN_SETTINGS') ORDER BY CASE WHEN Code='MENU_ADMIN_SETTINGS' THEN 0 ELSE 1 END);
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_NOTIFICATION_TEMPLATES')
  INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_ADMIN_NOTIFICATION_TEMPLATES',N'Mẫu thông báo','/Admin/Settings/NotificationTemplates','fa-solid fa-message',@parent,9,1,@now);
 DECLARE @role UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN'),@menu UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_NOTIFICATION_TEMPLATES');
 IF @role IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@role AND MenuId=@menu) INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@role,@menu,@now);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK TRANSACTION;THROW;END CATCH;

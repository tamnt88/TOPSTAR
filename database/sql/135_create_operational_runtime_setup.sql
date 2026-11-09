SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.OperationalRuntimePolicies','U') IS NULL
 CREATE TABLE dbo.OperationalRuntimePolicies(
  Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalRuntimePolicies PRIMARY KEY,
  ReviewEditHours INT NOT NULL, BookingLeadMinutes INT NOT NULL, BookingMaxHours INT NOT NULL,
  MaxPayoutAccounts INT NOT NULL, OtpRequestLimit INT NOT NULL, OtpWindowMinutes INT NOT NULL,
  OtpExpiryMinutes INT NOT NULL, OtpMaxAttempts INT NOT NULL, UpdatedByUserId UNIQUEIDENTIFIER NULL,
  CreatedAtUtc DATETIME2(7) NOT NULL, UpdatedAtUtc DATETIME2(7) NOT NULL,
  CONSTRAINT FK_OperationalRuntimePolicies_User FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id),
  CONSTRAINT CK_OperationalRuntimePolicies_Values CHECK(ReviewEditHours BETWEEN 1 AND 168 AND BookingLeadMinutes BETWEEN 1 AND 1440 AND BookingMaxHours BETWEEN 1 AND 168 AND MaxPayoutAccounts BETWEEN 1 AND 20 AND OtpRequestLimit BETWEEN 1 AND 20 AND OtpWindowMinutes BETWEEN 1 AND 1440 AND OtpExpiryMinutes BETWEEN 1 AND 60 AND OtpMaxAttempts BETWEEN 1 AND 20));
 IF OBJECT_ID('dbo.OperationalRuntimePolicyHistories','U') IS NULL
 CREATE TABLE dbo.OperationalRuntimePolicyHistories(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalRuntimePolicyHistories PRIMARY KEY,PolicyId UNIQUEIDENTIFIER NOT NULL,SnapshotJson NVARCHAR(4000) NOT NULL,Reason NVARCHAR(500) NOT NULL,ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_OperationalRuntimePolicyHistories_Policy FOREIGN KEY(PolicyId) REFERENCES dbo.OperationalRuntimePolicies(Id),CONSTRAINT FK_OperationalRuntimePolicyHistories_User FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id));
 IF NOT EXISTS(SELECT 1 FROM dbo.OperationalRuntimePolicies)
 INSERT dbo.OperationalRuntimePolicies VALUES(NEWID(),24,15,24,5,3,10,5,5,NULL,SYSUTCDATETIME(),SYSUTCDATETIME());

 IF OBJECT_ID('dbo.OperationalCatalogItems','U') IS NULL
 CREATE TABLE dbo.OperationalCatalogItems(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalCatalogItems PRIMARY KEY,CatalogType VARCHAR(40) NOT NULL,Code VARCHAR(80) NOT NULL,Name NVARCHAR(150) NOT NULL,Description NVARCHAR(500) NULL,DefaultSeverity VARCHAR(20) NULL,DefaultPenaltyPoints INT NULL,SortOrder INT NOT NULL,IsActive BIT NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT UQ_OperationalCatalogItems_Type_Code UNIQUE(CatalogType,Code),CONSTRAINT CK_OperationalCatalogItems_Type CHECK(CatalogType IN('VIOLATION_CATEGORY','DELIVERABLE_TYPE','SHIPMENT_CHECKPOINT')),CONSTRAINT CK_OperationalCatalogItems_Points CHECK(DefaultPenaltyPoints IS NULL OR DefaultPenaltyPoints BETWEEN 0 AND 100));
 IF OBJECT_ID('dbo.OperationalCatalogHistories','U') IS NULL
 CREATE TABLE dbo.OperationalCatalogHistories(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalCatalogHistories PRIMARY KEY,CatalogItemId UNIQUEIDENTIFIER NOT NULL,Action VARCHAR(20) NOT NULL,SnapshotJson NVARCHAR(4000) NOT NULL,Reason NVARCHAR(500) NOT NULL,ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_OperationalCatalogHistories_User FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id));

 DECLARE @seed TABLE(CatalogType VARCHAR(40),Code VARCHAR(80),Name NVARCHAR(150),Severity VARCHAR(20),Points INT,SortOrder INT);
 INSERT @seed VALUES
 ('VIOLATION_CATEGORY','late-delivery',N'Trễ thời hạn bàn giao','Medium',10,10),('VIOLATION_CATEGORY','fraud',N'Gian lận dữ liệu','High',50,20),('VIOLATION_CATEGORY','policy-breach',N'Vi phạm chính sách nội dung','High',30,30),
 ('DELIVERABLE_TYPE','video',N'Video',NULL,NULL,10),('DELIVERABLE_TYPE','image-post',N'Bài đăng hình ảnh',NULL,NULL,20),('DELIVERABLE_TYPE','livestream',N'Livestream',NULL,NULL,30),('DELIVERABLE_TYPE','short-video',N'Video ngắn',NULL,NULL,40),
 ('SHIPMENT_CHECKPOINT','PickedUp',N'Đã lấy hàng',NULL,NULL,10),('SHIPMENT_CHECKPOINT','AtHub',N'Đang tại kho',NULL,NULL,20),('SHIPMENT_CHECKPOINT','OutForDelivery',N'Đang giao hàng',NULL,NULL,30),('SHIPMENT_CHECKPOINT','DeliveryAttempt',N'Đã thử giao',NULL,NULL,40),('SHIPMENT_CHECKPOINT','Exception',N'Có sự cố',NULL,NULL,50),('SHIPMENT_CHECKPOINT','Other',N'Cập nhật khác',NULL,NULL,60);
 INSERT dbo.OperationalCatalogItems(Id,CatalogType,Code,Name,DefaultSeverity,DefaultPenaltyPoints,SortOrder,IsActive,CreatedAtUtc,UpdatedAtUtc)
 SELECT NEWID(),s.CatalogType,s.Code,s.Name,s.Severity,s.Points,s.SortOrder,1,SYSUTCDATETIME(),SYSUTCDATETIME() FROM @seed s WHERE NOT EXISTS(SELECT 1 FROM dbo.OperationalCatalogItems x WHERE x.CatalogType=s.CatalogType AND x.Code=s.Code);

 DECLARE @parent UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS');
 DECLARE @menu UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS_OPERATIONAL');
 IF @menu IS NULL BEGIN SET @menu=NEWID();INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(@menu,'MENU_ADMIN_SETTINGS_OPERATIONAL',N'Cấu hình vận hành','/Admin/Settings/OperationalSetup','fa-solid fa-sliders',@parent,36,1,SYSUTCDATETIME());END
 ELSE UPDATE dbo.Menus SET Title=N'Cấu hình vận hành',Path='/Admin/Settings/OperationalSetup',Icon='fa-solid fa-sliders',ParentId=@parent,IsVisible=1 WHERE Id=@menu;
 INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) SELECT r.Id,@menu,SYSUTCDATETIME() FROM dbo.Roles r WHERE UPPER(r.Code)='ADMIN' AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=r.Id AND rm.MenuId=@menu);
 COMMIT;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK;THROW;END CATCH;
GO

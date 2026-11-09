SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.UserViolations','U') IS NULL
 BEGIN
  CREATE TABLE dbo.UserViolations(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserViolations PRIMARY KEY,
   UserId UNIQUEIDENTIFIER NOT NULL,Category NVARCHAR(100) NOT NULL,Severity VARCHAR(20) NOT NULL,PenaltyPoints INT NOT NULL,Reason NVARCHAR(2000) NOT NULL,
   Status VARCHAR(20) NOT NULL,ResolutionNote NVARCHAR(1000) NULL,CreatedByUserId UNIQUEIDENTIFIER NOT NULL,RevokedByUserId UNIQUEIDENTIFIER NULL,RevokedAtUtc DATETIME2(7) NULL,CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NULL,
   CONSTRAINT FK_UserViolations_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),CONSTRAINT FK_UserViolations_Creator FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),CONSTRAINT FK_UserViolations_Revoker FOREIGN KEY(RevokedByUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_UserViolations_Severity CHECK(Severity IN('Low','Medium','High')),CONSTRAINT CK_UserViolations_Penalty CHECK(PenaltyPoints BETWEEN 0 AND 100),CONSTRAINT CK_UserViolations_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 10 AND 2000),
   CONSTRAINT CK_UserViolations_Status CHECK(Status IN('Active','Revoked')),CONSTRAINT CK_UserViolations_Resolution CHECK((Status='Active' AND ResolutionNote IS NULL AND RevokedByUserId IS NULL AND RevokedAtUtc IS NULL) OR(Status='Revoked' AND LEN(LTRIM(RTRIM(ResolutionNote))) BETWEEN 10 AND 1000 AND RevokedByUserId IS NOT NULL AND RevokedAtUtc IS NOT NULL)));
  CREATE INDEX IX_UserViolations_User_Status_Created ON dbo.UserViolations(UserId,Status,CreatedAtUtc DESC) INCLUDE(PenaltyPoints,Severity,Category);
 END;
 IF OBJECT_ID('dbo.UserViolationHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.UserViolationHistories(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserViolationHistories PRIMARY KEY,ViolationId UNIQUEIDENTIFIER NOT NULL,FromStatus VARCHAR(20) NULL,ToStatus VARCHAR(20) NOT NULL,Note NVARCHAR(2000) NOT NULL,ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_UserViolationHistories_Violation FOREIGN KEY(ViolationId) REFERENCES dbo.UserViolations(Id),CONSTRAINT FK_UserViolationHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_UserViolationHistories_Status CHECK((FromStatus IS NULL OR FromStatus IN('Active','Revoked')) AND ToStatus IN('Active','Revoked')));
  CREATE INDEX IX_UserViolationHistories_Violation_Created ON dbo.UserViolationHistories(ViolationId,CreatedAtUtc);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_UserViolationHistories_AppendOnly ON dbo.UserViolationHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;THROW 51000,N''Lịch sử vi phạm chỉ được phép ghi thêm.'',1;END;');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_UserViolations_ProtectFacts ON dbo.UserViolations AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;IF NOT EXISTS(SELECT 1 FROM inserted) THROW 51000,N''Không được xóa vi phạm.'',1;IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id WHERE i.UserId<>d.UserId OR i.Category<>d.Category OR i.Severity<>d.Severity OR i.PenaltyPoints<>d.PenaltyPoints OR i.Reason<>d.Reason OR i.CreatedByUserId<>d.CreatedByUserId OR i.CreatedAtUtc<>d.CreatedAtUtc) THROW 51000,N''Thông tin gốc của vi phạm là bất biến.'',1;END;');
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_VIOLATIONS') INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_ADMIN_VIOLATIONS',N'Vi phạm tài khoản','/Admin/Violations','fa-solid fa-triangle-exclamation',NULL,13,1,@now);
 DECLARE @roleId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN'),@menuId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_VIOLATIONS');
 IF @roleId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@roleId AND MenuId=@menuId) INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@roleId,@menuId,@now);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK TRANSACTION;THROW;END CATCH;

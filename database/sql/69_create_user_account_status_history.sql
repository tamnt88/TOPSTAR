SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.UserAccountStatusHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.UserAccountStatusHistories(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserAccountStatusHistories PRIMARY KEY,
   UserId UNIQUEIDENTIFIER NOT NULL,FromStatus NVARCHAR(30) NOT NULL,ToStatus NVARCHAR(30) NOT NULL,
   ProfileType VARCHAR(10) NOT NULL,FromProfileStatus NVARCHAR(30) NOT NULL,ToProfileStatus NVARCHAR(30) NOT NULL,
   Reason NVARCHAR(1000) NOT NULL,ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_UserAccountStatusHistories_User FOREIGN KEY(UserId) REFERENCES dbo.Users(Id),
   CONSTRAINT FK_UserAccountStatusHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_UserAccountStatusHistories_UserStatus CHECK(FromStatus IN('Active','Locked') AND ToStatus IN('Active','Locked') AND FromStatus<>ToStatus),
   CONSTRAINT CK_UserAccountStatusHistories_ProfileType CHECK(ProfileType IN('Brand','KOC')),
   CONSTRAINT CK_UserAccountStatusHistories_ProfileStatus CHECK(FromProfileStatus IN('Approved','Locked') AND ToProfileStatus IN('Approved','Locked') AND FromProfileStatus<>ToProfileStatus),
   CONSTRAINT CK_UserAccountStatusHistories_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 10 AND 1000));
  CREATE INDEX IX_UserAccountStatusHistories_User_Created ON dbo.UserAccountStatusHistories(UserId,CreatedAtUtc);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_UserAccountStatusHistories_AppendOnly ON dbo.UserAccountStatusHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;THROW 51000,N''Lịch sử trạng thái tài khoản chỉ được phép ghi thêm.'',1;END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH IF XACT_STATE()<>0 ROLLBACK TRANSACTION;THROW;END CATCH;

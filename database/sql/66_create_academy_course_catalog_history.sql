SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.AcademyCourses','U') IS NULL
 BEGIN
  CREATE TABLE dbo.AcademyCourses(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AcademyCourses PRIMARY KEY,
   Title NVARCHAR(160) NOT NULL,Level NVARCHAR(50) NOT NULL,DurationWeeks INT NOT NULL,
   Summary NVARCHAR(1000) NOT NULL,Syllabus NVARCHAR(4000) NULL,Status VARCHAR(20) NOT NULL,OrderNo INT NOT NULL,
   CreatedByUserId UNIQUEIDENTIFIER NOT NULL,UpdatedByUserId UNIQUEIDENTIFIER NULL,
   CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NULL,
   CONSTRAINT FK_AcademyCourses_Creator FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT FK_AcademyCourses_Updater FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_AcademyCourses_Title CHECK(LEN(LTRIM(RTRIM(Title))) BETWEEN 3 AND 160),
   CONSTRAINT CK_AcademyCourses_Level CHECK(LEN(LTRIM(RTRIM(Level))) BETWEEN 2 AND 50),
   CONSTRAINT CK_AcademyCourses_Duration CHECK(DurationWeeks BETWEEN 1 AND 104),
   CONSTRAINT CK_AcademyCourses_Summary CHECK(LEN(LTRIM(RTRIM(Summary))) BETWEEN 10 AND 1000),
   CONSTRAINT CK_AcademyCourses_Status CHECK(Status IN('Draft','Published','Archived')),
   CONSTRAINT CK_AcademyCourses_Order CHECK(OrderNo BETWEEN 0 AND 10000));
  CREATE INDEX IX_AcademyCourses_Status_Order ON dbo.AcademyCourses(Status,OrderNo,Title);
 END;
 IF OBJECT_ID('dbo.AcademyCourseHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.AcademyCourseHistories(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AcademyCourseHistories PRIMARY KEY,
   CourseId UNIQUEIDENTIFIER NOT NULL,FromStatus VARCHAR(20) NULL,ToStatus VARCHAR(20) NOT NULL,
   ActionName VARCHAR(30) NOT NULL,Note NVARCHAR(1000) NOT NULL,ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_AcademyCourseHistories_Course FOREIGN KEY(CourseId) REFERENCES dbo.AcademyCourses(Id),
   CONSTRAINT FK_AcademyCourseHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_AcademyCourseHistories_Status CHECK((FromStatus IS NULL OR FromStatus IN('Draft','Published','Archived')) AND ToStatus IN('Draft','Published','Archived')),
   CONSTRAINT CK_AcademyCourseHistories_Action CHECK(ActionName IN('Create','Update','ChangeStatus')),
   CONSTRAINT CK_AcademyCourseHistories_Note CHECK(LEN(LTRIM(RTRIM(Note))) BETWEEN 5 AND 1000));
  CREATE INDEX IX_AcademyCourseHistories_Course_Created ON dbo.AcademyCourseHistories(CourseId,CreatedAtUtc);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyCourseHistories_AppendOnly ON dbo.AcademyCourseHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;THROW 51000,N''Lịch sử khóa học chỉ được phép ghi thêm.'',1;END;');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyCourses_NoDelete ON dbo.AcademyCourses AFTER DELETE AS BEGIN SET NOCOUNT ON;THROW 51000,N''Không được xóa khóa học; hãy chuyển sang Archived.'',1;END;');
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_ACADEMY')
  INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_ADMIN_ACADEMY',N'Học viện KOC','/Admin/Academy','fa-solid fa-graduation-cap',NULL,14,1,@now);
 DECLARE @roleId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='ADMIN'),@menuId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_ACADEMY');
 IF @roleId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@roleId AND MenuId=@menuId)
  INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@roleId,@menuId,@now);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

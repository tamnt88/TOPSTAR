SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.Schedules GROUP BY ParticipationId HAVING COUNT(*)>1)
    THROW 51033, 'Schedules contains duplicate ParticipationId rows. Resolve duplicates before migration 32.', 1;
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Schedules') AND name='UX_Schedules_Participation')
    CREATE UNIQUE INDEX UX_Schedules_Participation ON dbo.Schedules(ParticipationId);

IF OBJECT_ID('dbo.ScheduleStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE dbo.ScheduleStatusHistories(
        Id UNIQUEIDENTIFIER NOT NULL,
        ScheduleId UNIQUEIDENTIFIER NOT NULL,
        FromStatus NVARCHAR(30) NULL,
        ToStatus NVARCHAR(30) NOT NULL,
        StartsAtUtc DATETIME2(0) NULL,
        EndsAtUtc DATETIME2(0) NULL,
        Note NVARCHAR(1000) NULL,
        ActorUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        CONSTRAINT PK_ScheduleStatusHistories PRIMARY KEY(Id),
        CONSTRAINT FK_ScheduleStatusHistories_Schedule FOREIGN KEY(ScheduleId) REFERENCES dbo.Schedules(Id),
        CONSTRAINT FK_ScheduleStatusHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_ScheduleStatusHistories_From CHECK(FromStatus IS NULL OR FromStatus IN('Proposed','Confirmed','Rescheduled','Cancelled')),
        CONSTRAINT CK_ScheduleStatusHistories_To CHECK(ToStatus IN('Proposed','Confirmed','Rescheduled','Cancelled')),
        CONSTRAINT CK_ScheduleStatusHistories_Time CHECK(StartsAtUtc IS NULL OR EndsAtUtc IS NULL OR EndsAtUtc>StartsAtUtc)
    );
    CREATE INDEX IX_ScheduleStatusHistories_Schedule_Created ON dbo.ScheduleStatusHistories(ScheduleId,CreatedAtUtc);
END;

INSERT INTO dbo.ScheduleStatusHistories(Id,ScheduleId,FromStatus,ToStatus,StartsAtUtc,EndsAtUtc,Note,ActorUserId,CreatedAtUtc)
SELECT NEWID(),s.Id,NULL,s.Status,s.StartsAtUtc,s.EndsAtUtc,N'Migrated snapshot',NULL,COALESCE(s.UpdatedAtUtc,s.CreatedAtUtc)
FROM dbo.Schedules s WHERE NOT EXISTS(SELECT 1 FROM dbo.ScheduleStatusHistories h WHERE h.ScheduleId=s.Id);

EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ScheduleStatusHistories_AppendOnly ON dbo.ScheduleStatusHistories INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51034, ''ScheduleStatusHistories is append-only.'', 1; END;');

IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_BRAND_BOOKINGS')
 INSERT INTO dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_BRAND_BOOKINGS',N'Lịch booking','/Brand/Bookings','fa-solid fa-calendar-check',NULL,6,1,SYSUTCDATETIME());
IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_KOC_BOOKINGS')
 INSERT INTO dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_KOC_BOOKINGS',N'Lịch booking','/Koc/Bookings','fa-solid fa-calendar-check',NULL,6,1,SYSUTCDATETIME());
DECLARE @BrandRole UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='BRAND'), @KocRole UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='KOC');
DECLARE @BrandMenu UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_BRAND_BOOKINGS'), @KocMenu UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_KOC_BOOKINGS');
IF @BrandRole IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@BrandRole AND MenuId=@BrandMenu) INSERT INTO dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@BrandRole,@BrandMenu,SYSUTCDATETIME());
IF @KocRole IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@KocRole AND MenuId=@KocMenu) INSERT INTO dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@KocRole,@KocMenu,SYSUTCDATETIME());

COMMIT TRANSACTION;

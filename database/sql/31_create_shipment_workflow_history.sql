SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM dbo.Shipments GROUP BY ParticipationId HAVING COUNT(*) > 1)
    THROW 51031, 'Shipments contains duplicate ParticipationId rows. Resolve duplicates before migration 31.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Shipments') AND name='UX_Shipments_Participation')
    CREATE UNIQUE INDEX UX_Shipments_Participation ON dbo.Shipments(ParticipationId);

IF OBJECT_ID('dbo.ShipmentStatusHistories','U') IS NULL
BEGIN
    CREATE TABLE dbo.ShipmentStatusHistories (
        Id UNIQUEIDENTIFIER NOT NULL,
        ShipmentId UNIQUEIDENTIFIER NOT NULL,
        FromStatus NVARCHAR(30) NULL,
        ToStatus NVARCHAR(30) NOT NULL,
        Note NVARCHAR(1000) NULL,
        ActorUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIME2(0) NOT NULL,
        CONSTRAINT PK_ShipmentStatusHistories PRIMARY KEY (Id),
        CONSTRAINT FK_ShipmentStatusHistories_Shipment FOREIGN KEY (ShipmentId) REFERENCES dbo.Shipments(Id),
        CONSTRAINT FK_ShipmentStatusHistories_Actor FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_ShipmentStatusHistories_From CHECK (FromStatus IS NULL OR FromStatus IN ('NotCreated','Created','InTransit','Delivered','Issue','Returned')),
        CONSTRAINT CK_ShipmentStatusHistories_To CHECK (ToStatus IN ('NotCreated','Created','InTransit','Delivered','Issue','Returned'))
    );
    CREATE INDEX IX_ShipmentStatusHistories_Shipment_Created ON dbo.ShipmentStatusHistories(ShipmentId,CreatedAtUtc);
END;

INSERT INTO dbo.ShipmentStatusHistories (Id,ShipmentId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
SELECT NEWID(),s.Id,NULL,s.Status,COALESCE(s.IssueNote,N'Migrated snapshot'),NULL,COALESCE(s.UpdatedAtUtc,s.CreatedAtUtc)
FROM dbo.Shipments s
WHERE NOT EXISTS (SELECT 1 FROM dbo.ShipmentStatusHistories h WHERE h.ShipmentId=s.Id);

EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ShipmentStatusHistories_AppendOnly ON dbo.ShipmentStatusHistories INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51032, ''ShipmentStatusHistories is append-only.'', 1; END;');

IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Code='MENU_BRAND_SHIPMENTS')
    INSERT INTO dbo.Menus (Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES (NEWID(),'MENU_BRAND_SHIPMENTS',N'Vận chuyển','/Brand/Shipments','fa-solid fa-truck-fast',NULL,5,1,SYSUTCDATETIME());
IF NOT EXISTS (SELECT 1 FROM dbo.Menus WHERE Code='MENU_KOC_SHIPMENTS')
    INSERT INTO dbo.Menus (Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES (NEWID(),'MENU_KOC_SHIPMENTS',N'Đơn hàng','/Koc/Shipments','fa-solid fa-box',NULL,5,1,SYSUTCDATETIME());

DECLARE @BrandRoleId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='BRAND');
DECLARE @KocRoleId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code)='KOC');
DECLARE @BrandMenuId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_BRAND_SHIPMENTS');
DECLARE @KocMenuId UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_KOC_SHIPMENTS');
IF @BrandRoleId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@BrandRoleId AND MenuId=@BrandMenuId)
    INSERT INTO dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@BrandRoleId,@BrandMenuId,SYSUTCDATETIME());
IF @KocRoleId IS NOT NULL AND NOT EXISTS (SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@KocRoleId AND MenuId=@KocMenuId)
    INSERT INTO dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) VALUES(@KocRoleId,@KocMenuId,SYSUTCDATETIME());

COMMIT TRANSACTION;

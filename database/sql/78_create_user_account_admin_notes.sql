SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.UserAccountAdminNotes', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.UserAccountAdminNotes
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_UserAccountAdminNotes PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        Body NVARCHAR(2000) NOT NULL,
        ActorUserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_UserAccountAdminNotes_User FOREIGN KEY (UserId) REFERENCES dbo.Users(Id),
        CONSTRAINT FK_UserAccountAdminNotes_Actor FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_UserAccountAdminNotes_Body CHECK (LEN(LTRIM(RTRIM(Body))) BETWEEN 3 AND 2000)
    );
    CREATE INDEX IX_UserAccountAdminNotes_User_CreatedAt
        ON dbo.UserAccountAdminNotes(UserId, CreatedAtUtc DESC);
END;

EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_UserAccountAdminNotes_AppendOnly
ON dbo.UserAccountAdminNotes AFTER UPDATE, DELETE AS
BEGIN
    SET NOCOUNT ON;
    THROW 51000, N''Ghi chú quản trị là dữ liệu lịch sử chỉ được phép ghi thêm.'', 1;
END');

COMMIT TRANSACTION;

SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.SystemWebsiteConfigs', 'AcademyContentJson') IS NULL
    ALTER TABLE dbo.SystemWebsiteConfigs ADD AcademyContentJson nvarchar(max) NULL;

IF OBJECT_ID('dbo.AcademyContentHistories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.AcademyContentHistories
    (
        Id uniqueidentifier NOT NULL CONSTRAINT PK_AcademyContentHistories PRIMARY KEY,
        ContentJson nvarchar(max) NOT NULL,
        Reason nvarchar(1000) NOT NULL,
        ActorUserId uniqueidentifier NULL,
        CreatedAtUtc datetime2 NOT NULL
    );
    CREATE INDEX IX_AcademyContentHistories_CreatedAtUtc ON dbo.AcademyContentHistories(CreatedAtUtc);
END;

COMMIT TRANSACTION;
PRINT N'OK - Đã bổ sung quản trị nội dung trang Học viện KOC.';

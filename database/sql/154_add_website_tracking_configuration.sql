/* TOPSTAR - Public website tracking configuration and history. Safe to rerun. */
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
BEGIN TRANSACTION;
IF OBJECT_ID(N'dbo.WebsiteTrackingConfiguration', N'U') IS NULL
CREATE TABLE dbo.WebsiteTrackingConfiguration
(
    Id int NOT NULL CONSTRAINT PK_WebsiteTrackingConfiguration PRIMARY KEY,
    IsEnabled bit NOT NULL CONSTRAINT DF_WebsiteTrackingConfiguration_Enabled DEFAULT 0,
    HeadCode nvarchar(max) NULL,
    BodyCode nvarchar(max) NULL,
    UpdatedAtUtc datetime2 NOT NULL,
    UpdatedByUserId uniqueidentifier NOT NULL,
    CONSTRAINT CK_WebsiteTrackingConfiguration_Singleton CHECK (Id = 1)
);
IF OBJECT_ID(N'dbo.WebsiteTrackingHistory', N'U') IS NULL
CREATE TABLE dbo.WebsiteTrackingHistory
(
    Id bigint IDENTITY(1,1) NOT NULL CONSTRAINT PK_WebsiteTrackingHistory PRIMARY KEY,
    IsEnabled bit NOT NULL,
    HeadCode nvarchar(max) NULL,
    BodyCode nvarchar(max) NULL,
    ActorUserId uniqueidentifier NOT NULL,
    CreatedAtUtc datetime2 NOT NULL
);
COMMIT TRANSACTION;
GO

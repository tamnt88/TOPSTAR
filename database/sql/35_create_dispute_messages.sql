SET NOCOUNT ON;SET XACT_ABORT ON;BEGIN TRANSACTION;
IF OBJECT_ID('dbo.DisputeMessages','U') IS NULL
BEGIN
 CREATE TABLE dbo.DisputeMessages(Id UNIQUEIDENTIFIER NOT NULL,DisputeCaseId UNIQUEIDENTIFIER NOT NULL,AuthorUserId UNIQUEIDENTIFIER NOT NULL,AuthorRole NVARCHAR(20) NOT NULL,Body NVARCHAR(2000) NOT NULL,EvidenceUrl NVARCHAR(1000) NULL,Visibility NVARCHAR(20) NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT PK_DisputeMessages PRIMARY KEY(Id),CONSTRAINT FK_DisputeMessages_Case FOREIGN KEY(DisputeCaseId) REFERENCES dbo.DisputeCases(Id),CONSTRAINT FK_DisputeMessages_Author FOREIGN KEY(AuthorUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_DisputeMessages_AuthorRole CHECK(AuthorRole IN('ADMIN','BRAND','KOC')),CONSTRAINT CK_DisputeMessages_Visibility CHECK(Visibility IN('Parties','AdminOnly')),CONSTRAINT CK_DisputeMessages_AdminOnly CHECK(Visibility<>'AdminOnly' OR AuthorRole='ADMIN'));
 CREATE INDEX IX_DisputeMessages_Case_Created ON dbo.DisputeMessages(DisputeCaseId,CreatedAtUtc);
END;
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_DisputeMessages_AppendOnly ON dbo.DisputeMessages INSTEAD OF UPDATE, DELETE AS BEGIN THROW 51036, ''DisputeMessages is append-only.'', 1; END;');
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_DisputeMessages_ActiveCaseOnly ON dbo.DisputeMessages AFTER INSERT AS BEGIN IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.DisputeCases d ON d.Id=i.DisputeCaseId WHERE d.IsActive=0) THROW 51037, ''Cannot append message to a closed dispute case.'', 1; END;');
COMMIT TRANSACTION;

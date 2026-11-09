SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.DisputeCases','U') IS NULL
        THROW 51038, 'Run migration 34_create_dispute_workflow.sql before migration 36.', 1;

    IF COL_LENGTH('dbo.Campaigns','CancellationDisputeCaseId') IS NULL EXEC(N'ALTER TABLE dbo.Campaigns ADD CancellationDisputeCaseId UNIQUEIDENTIFIER NULL;');
    IF COL_LENGTH('dbo.Campaigns','CancelledByUserId') IS NULL EXEC(N'ALTER TABLE dbo.Campaigns ADD CancelledByUserId UNIQUEIDENTIFIER NULL;');
    IF COL_LENGTH('dbo.Campaigns','CancelledAtUtc') IS NULL EXEC(N'ALTER TABLE dbo.Campaigns ADD CancelledAtUtc DATETIME2(7) NULL;');
    IF COL_LENGTH('dbo.Campaigns','CancellationReason') IS NULL EXEC(N'ALTER TABLE dbo.Campaigns ADD CancellationReason NVARCHAR(1000) NULL;');

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Campaigns_CancellationDisputeCase')
        EXEC(N'ALTER TABLE dbo.Campaigns ADD CONSTRAINT FK_Campaigns_CancellationDisputeCase FOREIGN KEY(CancellationDisputeCaseId) REFERENCES dbo.DisputeCases(Id);');
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_Campaigns_CancelledByUser')
        EXEC(N'ALTER TABLE dbo.Campaigns ADD CONSTRAINT FK_Campaigns_CancelledByUser FOREIGN KEY(CancelledByUserId) REFERENCES dbo.Users(Id);');
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.Campaigns') AND name='IX_Campaigns_CancellationDisputeCase')
        EXEC(N'CREATE INDEX IX_Campaigns_CancellationDisputeCase ON dbo.Campaigns(CancellationDisputeCaseId) WHERE CancellationDisputeCaseId IS NOT NULL;');
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Campaigns') AND name='CK_Campaigns_CancellationMetadata')
        EXEC(N'ALTER TABLE dbo.Campaigns ADD CONSTRAINT CK_Campaigns_CancellationMetadata CHECK((Status<>''Cancelled'' AND CancellationDisputeCaseId IS NULL AND CancelledByUserId IS NULL AND CancelledAtUtc IS NULL AND CancellationReason IS NULL) OR (Status=''Cancelled'' AND CancellationDisputeCaseId IS NOT NULL AND CancelledByUserId IS NOT NULL AND CancelledAtUtc IS NOT NULL AND LEN(CancellationReason)>=10));');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

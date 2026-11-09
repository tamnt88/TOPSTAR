SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;
    IF OBJECT_ID('dbo.OperationalScanRuns','U') IS NULL OR OBJECT_ID('dbo.DisputeCases','U') IS NULL
        THROW 51055, 'Run migrations 34, 44 and 45 first.', 1;

    IF COL_LENGTH('dbo.OperationalEscalations','SubmissionId') IS NULL EXEC(N'ALTER TABLE dbo.OperationalEscalations ADD SubmissionId UNIQUEIDENTIFIER NULL;');
    IF COL_LENGTH('dbo.OperationalEscalations','DisputeCaseId') IS NULL EXEC(N'ALTER TABLE dbo.OperationalEscalations ADD DisputeCaseId UNIQUEIDENTIFIER NULL;');
    EXEC(N'UPDATE dbo.OperationalEscalations SET SubmissionId=SourceEntityId WHERE RuleCode=''SUBMISSION_OVERDUE'' AND SubmissionId IS NULL;');

    IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.OperationalEscalations') AND name='FK_OperationalEscalations_Submission') ALTER TABLE dbo.OperationalEscalations DROP CONSTRAINT FK_OperationalEscalations_Submission;
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.OperationalEscalations') AND name='CK_OperationalEscalations_Rule') ALTER TABLE dbo.OperationalEscalations DROP CONSTRAINT CK_OperationalEscalations_Rule;
    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.OperationalScanRuns') AND name='CK_OperationalScanRuns_Rule') ALTER TABLE dbo.OperationalScanRuns DROP CONSTRAINT CK_OperationalScanRuns_Rule;

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.OperationalEscalations') AND name='FK_OperationalEscalations_SubmissionTyped') EXEC(N'ALTER TABLE dbo.OperationalEscalations ADD CONSTRAINT FK_OperationalEscalations_SubmissionTyped FOREIGN KEY(SubmissionId) REFERENCES dbo.Submissions(Id);');
    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.OperationalEscalations') AND name='FK_OperationalEscalations_DisputeCase') EXEC(N'ALTER TABLE dbo.OperationalEscalations ADD CONSTRAINT FK_OperationalEscalations_DisputeCase FOREIGN KEY(DisputeCaseId) REFERENCES dbo.DisputeCases(Id);');

    EXEC(N'ALTER TABLE dbo.OperationalEscalations WITH CHECK ADD CONSTRAINT CK_OperationalEscalations_Rule CHECK(
        (RuleCode=''SUBMISSION_OVERDUE'' AND SourceEntityType=''Submission'' AND SubmissionId=SourceEntityId AND DisputeCaseId IS NULL) OR
        (RuleCode=''DISPUTE_SLA'' AND SourceEntityType=''DisputeCase'' AND DisputeCaseId=SourceEntityId AND SubmissionId IS NULL));');
    ALTER TABLE dbo.OperationalScanRuns WITH CHECK ADD CONSTRAINT CK_OperationalScanRuns_Rule CHECK(RuleCode IN('SUBMISSION_OVERDUE','DISPUTE_SLA'));

    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.OperationalEscalations') AND name='IX_OperationalEscalations_DisputeCase') EXEC(N'CREATE INDEX IX_OperationalEscalations_DisputeCase ON dbo.OperationalEscalations(DisputeCaseId,Status,FirstDetectedAtUtc) WHERE DisputeCaseId IS NOT NULL;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_OperationalEscalations_ValidateSource ON dbo.OperationalEscalations AFTER INSERT,UPDATE AS BEGIN SET NOCOUNT ON;
      IF EXISTS(SELECT 1 FROM inserted i WHERE i.RuleCode=''SUBMISSION_OVERDUE'' AND NOT EXISTS(SELECT 1 FROM dbo.Submissions s WHERE s.Id=i.SubmissionId)) THROW 51052, ''Escalation source submission does not exist.'', 1;
      IF EXISTS(SELECT 1 FROM inserted i WHERE i.RuleCode=''DISPUTE_SLA'' AND NOT EXISTS(SELECT 1 FROM dbo.DisputeCases d WHERE d.Id=i.DisputeCaseId)) THROW 51056, ''Escalation source dispute does not exist.'', 1;
    END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

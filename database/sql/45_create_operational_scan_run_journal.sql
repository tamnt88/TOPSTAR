SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.OperationalEscalations','U') IS NULL
        THROW 51053, 'Run migration 44_create_operational_escalation_history.sql first.', 1;

    IF OBJECT_ID('dbo.OperationalScanRuns','U') IS NULL
    BEGIN
        CREATE TABLE dbo.OperationalScanRuns(
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalScanRuns PRIMARY KEY,
            RuleCode NVARCHAR(50) NOT NULL,
            ActorUserId UNIQUEIDENTIFIER NOT NULL,
            StartedAtUtc DATETIME2(7) NOT NULL,
            CompletedAtUtc DATETIME2(7) NOT NULL,
            DetectedCount INT NOT NULL,
            CreatedCount INT NOT NULL,
            ResolvedCount INT NOT NULL,
            CriticalCount INT NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_OperationalScanRuns_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_OperationalScanRuns_Rule CHECK(RuleCode='SUBMISSION_OVERDUE'),
            CONSTRAINT CK_OperationalScanRuns_Time CHECK(CompletedAtUtc>=StartedAtUtc AND CreatedAtUtc=CompletedAtUtc),
            CONSTRAINT CK_OperationalScanRuns_Counts CHECK(DetectedCount>=0 AND CreatedCount>=0 AND ResolvedCount>=0 AND CriticalCount>=0 AND CriticalCount<=DetectedCount AND CreatedCount<=DetectedCount)
        );
        CREATE INDEX IX_OperationalScanRuns_Rule_Completed ON dbo.OperationalScanRuns(RuleCode,CompletedAtUtc DESC) INCLUDE(DetectedCount,CreatedCount,ResolvedCount,CriticalCount,ActorUserId);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_OperationalScanRuns_AppendOnly ON dbo.OperationalScanRuns INSTEAD OF UPDATE,DELETE AS BEGIN THROW 51054, ''Operational scan runs are append-only.'', 1; END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

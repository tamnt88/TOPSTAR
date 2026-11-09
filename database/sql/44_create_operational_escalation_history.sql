SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.Submissions','U') IS NULL OR OBJECT_ID('dbo.Users','U') IS NULL
        THROW 51050, 'Submission workflow and Users schema are required.', 1;

    IF OBJECT_ID('dbo.OperationalEscalations','U') IS NULL
    BEGIN
        CREATE TABLE dbo.OperationalEscalations(
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalEscalations PRIMARY KEY,
            RuleCode NVARCHAR(50) NOT NULL,
            SourceEntityType NVARCHAR(50) NOT NULL,
            SourceEntityId UNIQUEIDENTIFIER NOT NULL,
            Severity NVARCHAR(20) NOT NULL,
            Status NVARCHAR(20) NOT NULL,
            FirstDetectedAtUtc DATETIME2(7) NOT NULL,
            LastDetectedAtUtc DATETIME2(7) NOT NULL,
            AcknowledgedByUserId UNIQUEIDENTIFIER NULL,
            AcknowledgedAtUtc DATETIME2(7) NULL,
            ResolvedByUserId UNIQUEIDENTIFIER NULL,
            ResolvedAtUtc DATETIME2(7) NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            UpdatedAtUtc DATETIME2(7) NULL,
            CONSTRAINT FK_OperationalEscalations_Submission FOREIGN KEY(SourceEntityId) REFERENCES dbo.Submissions(Id),
            CONSTRAINT FK_OperationalEscalations_AcknowledgedBy FOREIGN KEY(AcknowledgedByUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT FK_OperationalEscalations_ResolvedBy FOREIGN KEY(ResolvedByUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_OperationalEscalations_Rule CHECK(RuleCode='SUBMISSION_OVERDUE' AND SourceEntityType='Submission'),
            CONSTRAINT CK_OperationalEscalations_Severity CHECK(Severity IN('High','Critical')),
            CONSTRAINT CK_OperationalEscalations_Status CHECK(Status IN('Open','Acknowledged','Resolved')),
            CONSTRAINT CK_OperationalEscalations_Time CHECK(LastDetectedAtUtc>=FirstDetectedAtUtc),
            CONSTRAINT CK_OperationalEscalations_Metadata CHECK(
                (Status='Open' AND AcknowledgedByUserId IS NULL AND AcknowledgedAtUtc IS NULL AND ResolvedByUserId IS NULL AND ResolvedAtUtc IS NULL) OR
                (Status='Acknowledged' AND AcknowledgedByUserId IS NOT NULL AND AcknowledgedAtUtc IS NOT NULL AND ResolvedByUserId IS NULL AND ResolvedAtUtc IS NULL) OR
                (Status='Resolved' AND ResolvedByUserId IS NOT NULL AND ResolvedAtUtc IS NOT NULL))
        );
        CREATE UNIQUE INDEX UX_OperationalEscalations_Rule_Source ON dbo.OperationalEscalations(RuleCode,SourceEntityType,SourceEntityId);
        CREATE INDEX IX_OperationalEscalations_Status_Severity_Detected ON dbo.OperationalEscalations(Status,Severity,FirstDetectedAtUtc);
    END;

    IF OBJECT_ID('dbo.OperationalEscalationHistories','U') IS NULL
    BEGIN
        CREATE TABLE dbo.OperationalEscalationHistories(
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalEscalationHistories PRIMARY KEY,
            EscalationId UNIQUEIDENTIFIER NOT NULL,
            FromStatus NVARCHAR(20) NULL,
            ToStatus NVARCHAR(20) NOT NULL,
            Note NVARCHAR(1000) NULL,
            ActorUserId UNIQUEIDENTIFIER NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_OperationalEscalationHistories_Escalation FOREIGN KEY(EscalationId) REFERENCES dbo.OperationalEscalations(Id),
            CONSTRAINT FK_OperationalEscalationHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_OperationalEscalationHistories_From CHECK(FromStatus IS NULL OR FromStatus IN('Open','Acknowledged','Resolved')),
            CONSTRAINT CK_OperationalEscalationHistories_To CHECK(ToStatus IN('Open','Acknowledged','Resolved'))
        );
        CREATE INDEX IX_OperationalEscalationHistories_Escalation_Created ON dbo.OperationalEscalationHistories(EscalationId,CreatedAtUtc);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_OperationalEscalationHistories_AppendOnly ON dbo.OperationalEscalationHistories INSTEAD OF UPDATE,DELETE AS BEGIN THROW 51051, ''Operational escalation history is append-only.'', 1; END;');
    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_OperationalEscalations_ValidateSource ON dbo.OperationalEscalations AFTER INSERT,UPDATE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i WHERE i.RuleCode=''SUBMISSION_OVERDUE'' AND NOT EXISTS(SELECT 1 FROM dbo.Submissions s WHERE s.Id=i.SourceEntityId)) THROW 51052, ''Escalation source submission does not exist.'', 1; END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

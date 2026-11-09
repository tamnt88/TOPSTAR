SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID('dbo.OperationalSlaPolicies', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperationalSlaPolicies
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalSlaPolicies PRIMARY KEY,
        RuleCode NVARCHAR(50) NOT NULL,
        HighAfterHours INT NOT NULL,
        CriticalAfterHours INT NOT NULL,
        IsActive BIT NOT NULL,
        UpdatedByUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        UpdatedAtUtc DATETIME2(7) NULL,
        CONSTRAINT UQ_OperationalSlaPolicies_RuleCode UNIQUE (RuleCode),
        CONSTRAINT CK_OperationalSlaPolicies_Rule CHECK (RuleCode IN ('SUBMISSION_OVERDUE','DISPUTE_SLA')),
        CONSTRAINT CK_OperationalSlaPolicies_Thresholds CHECK (HighAfterHours >= 0 AND HighAfterHours <= 2160 AND CriticalAfterHours > HighAfterHours AND CriticalAfterHours <= 2160),
        CONSTRAINT FK_OperationalSlaPolicies_UpdatedBy FOREIGN KEY (UpdatedByUserId) REFERENCES dbo.Users(Id)
    );
END;

IF OBJECT_ID('dbo.OperationalSlaPolicyHistories', 'U') IS NULL
BEGIN
    CREATE TABLE dbo.OperationalSlaPolicyHistories
    (
        Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_OperationalSlaPolicyHistories PRIMARY KEY,
        PolicyId UNIQUEIDENTIFIER NOT NULL,
        FromHighAfterHours INT NULL,
        FromCriticalAfterHours INT NULL,
        FromIsActive BIT NULL,
        ToHighAfterHours INT NOT NULL,
        ToCriticalAfterHours INT NOT NULL,
        ToIsActive BIT NOT NULL,
        Note NVARCHAR(1000) NOT NULL,
        ActorUserId UNIQUEIDENTIFIER NULL,
        CreatedAtUtc DATETIME2(7) NOT NULL,
        CONSTRAINT FK_OperationalSlaPolicyHistories_Policy FOREIGN KEY (PolicyId) REFERENCES dbo.OperationalSlaPolicies(Id),
        CONSTRAINT FK_OperationalSlaPolicyHistories_Actor FOREIGN KEY (ActorUserId) REFERENCES dbo.Users(Id),
        CONSTRAINT CK_OperationalSlaPolicyHistories_FromPair CHECK ((FromHighAfterHours IS NULL AND FromCriticalAfterHours IS NULL AND FromIsActive IS NULL) OR (FromHighAfterHours IS NOT NULL AND FromCriticalAfterHours IS NOT NULL AND FromIsActive IS NOT NULL)),
        CONSTRAINT CK_OperationalSlaPolicyHistories_ToThresholds CHECK (ToHighAfterHours >= 0 AND ToHighAfterHours <= 2160 AND ToCriticalAfterHours > ToHighAfterHours AND ToCriticalAfterHours <= 2160),
        CONSTRAINT CK_OperationalSlaPolicyHistories_Note CHECK (LEN(LTRIM(RTRIM(Note))) BETWEEN 5 AND 1000)
    );
END;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.OperationalSlaPolicyHistories') AND name='IX_OperationalSlaPolicyHistories_Policy_Created')
    CREATE INDEX IX_OperationalSlaPolicyHistories_Policy_Created ON dbo.OperationalSlaPolicyHistories(PolicyId,CreatedAtUtc DESC);

DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
IF NOT EXISTS (SELECT 1 FROM dbo.OperationalSlaPolicies WHERE RuleCode='SUBMISSION_OVERDUE')
BEGIN
    DECLARE @submissionPolicyId UNIQUEIDENTIFIER=NEWID();
    INSERT dbo.OperationalSlaPolicies(Id,RuleCode,HighAfterHours,CriticalAfterHours,IsActive,CreatedAtUtc)
    VALUES(@submissionPolicyId,'SUBMISSION_OVERDUE',0,48,1,@now);
    INSERT dbo.OperationalSlaPolicyHistories(Id,PolicyId,ToHighAfterHours,ToCriticalAfterHours,ToIsActive,Note,CreatedAtUtc)
    VALUES(NEWID(),@submissionPolicyId,0,48,1,N'Khởi tạo từ rule vận hành hiện hành',@now);
END;

IF NOT EXISTS (SELECT 1 FROM dbo.OperationalSlaPolicies WHERE RuleCode='DISPUTE_SLA')
BEGIN
    DECLARE @disputePolicyId UNIQUEIDENTIFIER=NEWID();
    INSERT dbo.OperationalSlaPolicies(Id,RuleCode,HighAfterHours,CriticalAfterHours,IsActive,CreatedAtUtc)
    VALUES(@disputePolicyId,'DISPUTE_SLA',24,48,1,@now);
    INSERT dbo.OperationalSlaPolicyHistories(Id,PolicyId,ToHighAfterHours,ToCriticalAfterHours,ToIsActive,Note,CreatedAtUtc)
    VALUES(NEWID(),@disputePolicyId,24,48,1,N'Khởi tạo từ rule vận hành hiện hành',@now);
END;

EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_OperationalSlaPolicyHistories_AppendOnly ON dbo.OperationalSlaPolicyHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51000, ''Operational SLA policy history is append-only.'', 1; END;');
EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_OperationalSlaPolicies_ProtectIdentity ON dbo.OperationalSlaPolicies AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL OR i.RuleCode<>d.RuleCode) THROW 51000, ''Operational SLA policy identity cannot be changed or deleted.'', 1; END;');

COMMIT TRANSACTION;

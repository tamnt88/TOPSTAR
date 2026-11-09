SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.ReviewAppeals', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ReviewAppeals
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReviewAppeals PRIMARY KEY,
            ReviewId UNIQUEIDENTIFIER NOT NULL,
            AppellantUserId UNIQUEIDENTIFIER NOT NULL,
            Reason NVARCHAR(2000) NOT NULL,
            Status VARCHAR(20) NOT NULL,
            DecisionNote NVARCHAR(2000) NULL,
            ResolvedByUserId UNIQUEIDENTIFIER NULL,
            ResolvedAtUtc DATETIME2(7) NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            UpdatedAtUtc DATETIME2(7) NULL,
            CONSTRAINT UQ_ReviewAppeals_Review UNIQUE(ReviewId),
            CONSTRAINT FK_ReviewAppeals_Review FOREIGN KEY(ReviewId) REFERENCES dbo.Reviews(Id),
            CONSTRAINT FK_ReviewAppeals_Appellant FOREIGN KEY(AppellantUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT FK_ReviewAppeals_Resolver FOREIGN KEY(ResolvedByUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_ReviewAppeals_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 10 AND 2000),
            CONSTRAINT CK_ReviewAppeals_Status CHECK(Status IN ('Open','Accepted','Rejected')),
            CONSTRAINT CK_ReviewAppeals_Resolution CHECK(
                (Status='Open' AND DecisionNote IS NULL AND ResolvedByUserId IS NULL AND ResolvedAtUtc IS NULL)
                OR
                (Status IN ('Accepted','Rejected') AND LEN(LTRIM(RTRIM(DecisionNote))) BETWEEN 10 AND 2000
                 AND ResolvedByUserId IS NOT NULL AND ResolvedAtUtc IS NOT NULL))
        );
        CREATE INDEX IX_ReviewAppeals_Status_Created ON dbo.ReviewAppeals(Status, CreatedAtUtc DESC);
    END;

    IF OBJECT_ID('dbo.ReviewAppealHistories', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ReviewAppealHistories
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReviewAppealHistories PRIMARY KEY,
            AppealId UNIQUEIDENTIFIER NOT NULL,
            FromStatus VARCHAR(20) NULL,
            ToStatus VARCHAR(20) NOT NULL,
            Note NVARCHAR(2000) NULL,
            ActorUserId UNIQUEIDENTIFIER NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_ReviewAppealHistories_Appeal FOREIGN KEY(AppealId) REFERENCES dbo.ReviewAppeals(Id),
            CONSTRAINT FK_ReviewAppealHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_ReviewAppealHistories_Status CHECK(
                (FromStatus IS NULL OR FromStatus IN ('Open','Accepted','Rejected'))
                AND ToStatus IN ('Open','Accepted','Rejected'))
        );
        CREATE INDEX IX_ReviewAppealHistories_Appeal_Created
            ON dbo.ReviewAppealHistories(AppealId, CreatedAtUtc);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ReviewAppealHistories_AppendOnly
    ON dbo.ReviewAppealHistories AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N''Lịch sử khiếu nại review chỉ được phép ghi thêm.'', 1;
    END;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ReviewAppeals_ProtectIdentity
    ON dbo.ReviewAppeals AFTER INSERT, UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        IF NOT EXISTS(SELECT 1 FROM inserted)
            THROW 51000, N''Không được xóa hồ sơ khiếu nại review.'', 1;
        IF EXISTS(
            SELECT 1 FROM inserted i
            LEFT JOIN deleted d ON d.Id=i.Id
            JOIN dbo.Reviews r ON r.Id=i.ReviewId
            WHERE i.AppellantUserId<>r.RevieweeUserId
               OR (d.Id IS NOT NULL AND (i.ReviewId<>d.ReviewId OR i.AppellantUserId<>d.AppellantUserId OR i.Reason<>d.Reason))
        )
            THROW 51000, N''Sai người khiếu nại hoặc dữ liệu căn cứ bất biến đã bị thay đổi.'', 1;
    END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

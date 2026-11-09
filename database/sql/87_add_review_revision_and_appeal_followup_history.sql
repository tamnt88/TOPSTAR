SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.ReviewRevisionHistories','U') IS NULL
    BEGIN
        CREATE TABLE dbo.ReviewRevisionHistories(
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReviewRevisionHistories PRIMARY KEY,
            ReviewId UNIQUEIDENTIFIER NOT NULL,
            FromRating TINYINT NOT NULL,
            ToRating TINYINT NOT NULL,
            FromComment NVARCHAR(2000) NULL,
            ToComment NVARCHAR(2000) NULL,
            Reason NVARCHAR(500) NOT NULL,
            ActorUserId UNIQUEIDENTIFIER NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_ReviewRevisionHistories_Review FOREIGN KEY(ReviewId) REFERENCES dbo.Reviews(Id),
            CONSTRAINT FK_ReviewRevisionHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_ReviewRevisionHistories_Rating CHECK(FromRating BETWEEN 1 AND 5 AND ToRating BETWEEN 1 AND 5),
            CONSTRAINT CK_ReviewRevisionHistories_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 10 AND 500)
        );
        CREATE INDEX IX_ReviewRevisionHistories_Review_Created ON dbo.ReviewRevisionHistories(ReviewId,CreatedAtUtc);
    END;

    IF OBJECT_ID('dbo.ReviewAppealSupplements','U') IS NULL
    BEGIN
        CREATE TABLE dbo.ReviewAppealSupplements(
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReviewAppealSupplements PRIMARY KEY,
            AppealId UNIQUEIDENTIFIER NOT NULL,
            Body NVARCHAR(2000) NOT NULL,
            ActorUserId UNIQUEIDENTIFIER NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_ReviewAppealSupplements_Appeal FOREIGN KEY(AppealId) REFERENCES dbo.ReviewAppeals(Id),
            CONSTRAINT FK_ReviewAppealSupplements_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_ReviewAppealSupplements_Body CHECK(LEN(LTRIM(RTRIM(Body))) BETWEEN 10 AND 2000)
        );
        CREATE INDEX IX_ReviewAppealSupplements_Appeal_Created ON dbo.ReviewAppealSupplements(AppealId,CreatedAtUtc);
    END;

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.ReviewAppeals') AND name='CK_ReviewAppeals_Status')
        ALTER TABLE dbo.ReviewAppeals DROP CONSTRAINT CK_ReviewAppeals_Status;
    ALTER TABLE dbo.ReviewAppeals WITH CHECK ADD CONSTRAINT CK_ReviewAppeals_Status CHECK(Status IN('Open','Accepted','Rejected','Withdrawn'));

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.ReviewAppeals') AND name='CK_ReviewAppeals_Resolution')
        ALTER TABLE dbo.ReviewAppeals DROP CONSTRAINT CK_ReviewAppeals_Resolution;
    ALTER TABLE dbo.ReviewAppeals WITH CHECK ADD CONSTRAINT CK_ReviewAppeals_Resolution CHECK(
        (Status='Open' AND DecisionNote IS NULL AND ResolvedByUserId IS NULL AND ResolvedAtUtc IS NULL)
        OR (Status IN('Accepted','Rejected') AND LEN(LTRIM(RTRIM(DecisionNote))) BETWEEN 10 AND 2000 AND ResolvedByUserId IS NOT NULL AND ResolvedAtUtc IS NOT NULL)
        OR (Status='Withdrawn' AND LEN(LTRIM(RTRIM(DecisionNote))) BETWEEN 10 AND 2000 AND ResolvedByUserId IS NOT NULL AND ResolvedAtUtc IS NOT NULL));

    IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.ReviewAppealHistories') AND name='CK_ReviewAppealHistories_Status')
        ALTER TABLE dbo.ReviewAppealHistories DROP CONSTRAINT CK_ReviewAppealHistories_Status;
    ALTER TABLE dbo.ReviewAppealHistories WITH CHECK ADD CONSTRAINT CK_ReviewAppealHistories_Status CHECK(
        (FromStatus IS NULL OR FromStatus IN('Open','Accepted','Rejected','Withdrawn'))
        AND ToStatus IN('Open','Accepted','Rejected','Withdrawn'));

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ReviewRevisionHistories_AppendOnly ON dbo.ReviewRevisionHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51000,N''Lịch sử sửa đánh giá chỉ được phép ghi thêm.'',1; END;');
    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ReviewAppealSupplements_AppendOnly ON dbo.ReviewAppealSupplements AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51000,N''Thông tin bổ sung khiếu nại chỉ được phép ghi thêm.'',1; END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

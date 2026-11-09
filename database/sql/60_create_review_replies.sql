SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.ReviewReplies', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ReviewReplies
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ReviewReplies PRIMARY KEY,
            ReviewId UNIQUEIDENTIFIER NOT NULL,
            AuthorUserId UNIQUEIDENTIFIER NOT NULL,
            Body NVARCHAR(2000) NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT UQ_ReviewReplies_Review UNIQUE(ReviewId),
            CONSTRAINT FK_ReviewReplies_Review FOREIGN KEY(ReviewId) REFERENCES dbo.Reviews(Id),
            CONSTRAINT FK_ReviewReplies_Author FOREIGN KEY(AuthorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_ReviewReplies_Body CHECK(LEN(LTRIM(RTRIM(Body))) BETWEEN 2 AND 2000)
        );
        CREATE INDEX IX_ReviewReplies_Author_Created
            ON dbo.ReviewReplies(AuthorUserId, CreatedAtUtc DESC);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ReviewReplies_AppendOnly
    ON dbo.ReviewReplies AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N''Phản hồi đánh giá chỉ được phép ghi thêm.'', 1;
    END;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ReviewReplies_ValidateAuthor
    ON dbo.ReviewReplies AFTER INSERT AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS (
            SELECT 1 FROM inserted i
            JOIN dbo.Reviews r ON r.Id = i.ReviewId
            WHERE i.AuthorUserId <> r.RevieweeUserId
        )
            THROW 51000, N''Chỉ người được đánh giá mới được phản hồi.'', 1;
    END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

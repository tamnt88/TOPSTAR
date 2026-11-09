SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.EmailVerificationTokens', 'CodeHash') IS NULL
        ALTER TABLE dbo.EmailVerificationTokens ADD CodeHash CHAR(64) NULL;
    IF COL_LENGTH('dbo.EmailVerificationTokens', 'AttemptCount') IS NULL
        ALTER TABLE dbo.EmailVerificationTokens ADD AttemptCount INT NOT NULL
            CONSTRAINT DF_EmailVerificationTokens_AttemptCount DEFAULT(0);

    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_EmailVerificationTokens_Attempts')
        EXEC(N'ALTER TABLE dbo.EmailVerificationTokens ADD CONSTRAINT CK_EmailVerificationTokens_Attempts
               CHECK(AttemptCount BETWEEN 0 AND 5);');
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE name='CK_EmailVerificationTokens_CodeHash')
        EXEC(N'ALTER TABLE dbo.EmailVerificationTokens ADD CONSTRAINT CK_EmailVerificationTokens_CodeHash
               CHECK(CodeHash IS NULL OR (LEN(CodeHash)=64 AND CodeHash NOT LIKE ''%[^0-9A-F]%''));');
    IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.EmailVerificationTokens') AND name='IX_EmailVerificationTokens_User_Created')
        CREATE INDEX IX_EmailVerificationTokens_User_Created
            ON dbo.EmailVerificationTokens(UserId, CreatedAtUtc DESC)
            INCLUDE(CodeHash, AttemptCount, ExpiresAtUtc, UsedAtUtc);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

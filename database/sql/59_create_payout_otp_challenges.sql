SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.PayoutOtpChallenges', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PayoutOtpChallenges
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PayoutOtpChallenges PRIMARY KEY,
            KocProfileId UNIQUEIDENTIFIER NOT NULL,
            Amount DECIMAL(18,2) NOT NULL,
            CodeHash CHAR(64) NOT NULL,
            AttemptCount INT NOT NULL CONSTRAINT DF_PayoutOtpChallenges_AttemptCount DEFAULT(0),
            ExpiresAtUtc DATETIME2(7) NOT NULL,
            ConsumedAtUtc DATETIME2(7) NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_PayoutOtpChallenges_KocProfile
                FOREIGN KEY(KocProfileId) REFERENCES dbo.KocProfiles(Id),
            CONSTRAINT CK_PayoutOtpChallenges_Amount CHECK(Amount > 0),
            CONSTRAINT CK_PayoutOtpChallenges_Attempts CHECK(AttemptCount BETWEEN 0 AND 5),
            CONSTRAINT CK_PayoutOtpChallenges_Hash CHECK
                (LEN(CodeHash) = 64 AND CodeHash NOT LIKE '%[^0-9A-F]%'),
            CONSTRAINT CK_PayoutOtpChallenges_Time CHECK
                (ExpiresAtUtc > CreatedAtUtc AND (ConsumedAtUtc IS NULL OR ConsumedAtUtc >= CreatedAtUtc))
        );

        CREATE INDEX IX_PayoutOtpChallenges_Koc_Created
            ON dbo.PayoutOtpChallenges(KocProfileId, CreatedAtUtc DESC)
            INCLUDE(ExpiresAtUtc, ConsumedAtUtc, AttemptCount);
        CREATE INDEX IX_PayoutOtpChallenges_Expiry
            ON dbo.PayoutOtpChallenges(ExpiresAtUtc)
            WHERE ConsumedAtUtc IS NULL;
    END;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

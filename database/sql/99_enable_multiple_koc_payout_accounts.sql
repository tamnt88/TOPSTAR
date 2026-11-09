SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.KocPayoutAccounts', 'IsDefault') IS NULL
        ALTER TABLE dbo.KocPayoutAccounts
            ADD IsDefault BIT NOT NULL
                CONSTRAINT DF_KocPayoutAccounts_IsDefault DEFAULT (1) WITH VALUES;

    IF COL_LENGTH('dbo.KocPayoutAccounts', 'DeletedAtUtc') IS NULL
        ALTER TABLE dbo.KocPayoutAccounts ADD DeletedAtUtc DATETIME2(7) NULL;

    IF COL_LENGTH('dbo.KocPayoutAccounts', 'DeletedReason') IS NULL
        ALTER TABLE dbo.KocPayoutAccounts ADD DeletedReason NVARCHAR(500) NULL;

    IF COL_LENGTH('dbo.PayoutOtpChallenges', 'PayoutAccountId') IS NULL
        ALTER TABLE dbo.PayoutOtpChallenges ADD PayoutAccountId UNIQUEIDENTIFIER NULL;

    DECLARE @DropSql NVARCHAR(MAX) = N'';
    SELECT @DropSql +=
        CASE WHEN i.is_unique_constraint = 1
            THEN N'ALTER TABLE dbo.KocPayoutAccounts DROP CONSTRAINT ' + QUOTENAME(i.name) + N';'
            ELSE N'DROP INDEX ' + QUOTENAME(i.name) + N' ON dbo.KocPayoutAccounts;'
        END
    FROM sys.indexes i
    INNER JOIN sys.index_columns ic
        ON ic.object_id = i.object_id AND ic.index_id = i.index_id
    INNER JOIN sys.columns c
        ON c.object_id = ic.object_id AND c.column_id = ic.column_id
    WHERE i.object_id = OBJECT_ID('dbo.KocPayoutAccounts')
      AND i.is_unique = 1
      AND c.name = 'KocProfileId'
      AND NOT EXISTS (
          SELECT 1
          FROM sys.index_columns ic2
          WHERE ic2.object_id = i.object_id
            AND ic2.index_id = i.index_id
            AND ic2.key_ordinal > 1
      )
      AND i.name <> 'UX_KocPayoutAccounts_Default';
    IF @DropSql <> N'' EXEC sys.sp_executesql @DropSql;

    /* Dynamic SQL is intentional: SQL Server compiles a batch before the
       ALTER statements above have added their columns. */
    EXEC sys.sp_executesql N'
        ;WITH Ranked AS
        (
            SELECT Id,
                   ROW_NUMBER() OVER
                       (PARTITION BY KocProfileId
                        ORDER BY IsDefault DESC, CreatedAtUtc DESC, Id) AS RowNo
            FROM dbo.KocPayoutAccounts
            WHERE DeletedAtUtc IS NULL
        )
        UPDATE account
           SET IsDefault = CASE WHEN Ranked.RowNo = 1 THEN 1 ELSE 0 END
        FROM dbo.KocPayoutAccounts account
        INNER JOIN Ranked ON Ranked.Id = account.Id;

        UPDATE dbo.KocPayoutAccounts
           SET IsDefault = 0
         WHERE DeletedAtUtc IS NOT NULL AND IsDefault = 1;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(''dbo.KocPayoutAccounts'')
              AND name = ''UX_KocPayoutAccounts_Default''
        )
            CREATE UNIQUE INDEX UX_KocPayoutAccounts_Default
                ON dbo.KocPayoutAccounts(KocProfileId)
                WHERE IsDefault = 1 AND DeletedAtUtc IS NULL;

        IF NOT EXISTS (
            SELECT 1 FROM sys.indexes
            WHERE object_id = OBJECT_ID(''dbo.KocPayoutAccounts'')
              AND name = ''IX_KocPayoutAccounts_Koc_Active''
        )
            CREATE INDEX IX_KocPayoutAccounts_Koc_Active
                ON dbo.KocPayoutAccounts(KocProfileId, DeletedAtUtc)
                INCLUDE (IsDefault, BankCode, AccountHolderName, AccountNumberLast4);
    ';

    IF NOT EXISTS (
        SELECT 1 FROM sys.foreign_keys
        WHERE parent_object_id = OBJECT_ID('dbo.PayoutOtpChallenges')
          AND name = 'FK_PayoutOtpChallenges_PayoutAccount'
    )
        EXEC sys.sp_executesql N'
            ALTER TABLE dbo.PayoutOtpChallenges
                ADD CONSTRAINT FK_PayoutOtpChallenges_PayoutAccount
                    FOREIGN KEY (PayoutAccountId)
                    REFERENCES dbo.KocPayoutAccounts(Id);
        ';

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

EXEC sys.sp_executesql N'
    SELECT
        COUNT(*) AS ActivePayoutAccounts,
        COUNT(DISTINCT KocProfileId) AS KocsWithPayoutAccount,
        SUM(CASE WHEN IsDefault = 1 THEN 1 ELSE 0 END) AS DefaultAccounts
    FROM dbo.KocPayoutAccounts
    WHERE DeletedAtUtc IS NULL;
';

SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.PayoutRequests', N'U') IS NULL
        THROW 51000, N'Không tìm thấy bảng dbo.PayoutRequests.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.PayoutRequests')
          AND name = N'CK_PayoutRequests_Status'
    )
        ALTER TABLE dbo.PayoutRequests
            DROP CONSTRAINT CK_PayoutRequests_Status;

    ALTER TABLE dbo.PayoutRequests WITH CHECK
        ADD CONSTRAINT CK_PayoutRequests_Status
        CHECK ([Status] IN
        (
            N'WithdrawRequested',
            N'PaidOut',
            N'Failed',
            N'Cancelled'
        ));

    ALTER TABLE dbo.PayoutRequests
        CHECK CONSTRAINT CK_PayoutRequests_Status;

    IF OBJECT_ID(N'dbo.PayoutRequestStatusHistories', N'U') IS NULL
        THROW 51001, N'Không tìm thấy bảng dbo.PayoutRequestStatusHistories.', 1;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.PayoutRequestStatusHistories')
          AND name = N'CK_PayoutRequestStatusHistories_From'
    )
        ALTER TABLE dbo.PayoutRequestStatusHistories
            DROP CONSTRAINT CK_PayoutRequestStatusHistories_From;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.PayoutRequestStatusHistories')
          AND name = N'CK_PayoutRequestStatusHistories_To'
    )
        ALTER TABLE dbo.PayoutRequestStatusHistories
            DROP CONSTRAINT CK_PayoutRequestStatusHistories_To;

    ALTER TABLE dbo.PayoutRequestStatusHistories WITH CHECK
        ADD CONSTRAINT CK_PayoutRequestStatusHistories_From
        CHECK ([FromStatus] IS NULL OR [FromStatus] IN
        (
            N'WithdrawRequested',
            N'PaidOut',
            N'Failed',
            N'Cancelled'
        ));

    ALTER TABLE dbo.PayoutRequestStatusHistories WITH CHECK
        ADD CONSTRAINT CK_PayoutRequestStatusHistories_To
        CHECK ([ToStatus] IN
        (
            N'WithdrawRequested',
            N'PaidOut',
            N'Failed',
            N'Cancelled'
        ));

    ALTER TABLE dbo.PayoutRequestStatusHistories
        CHECK CONSTRAINT CK_PayoutRequestStatusHistories_From;

    ALTER TABLE dbo.PayoutRequestStatusHistories
        CHECK CONSTRAINT CK_PayoutRequestStatusHistories_To;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    cc.name AS ConstraintName,
    cc.definition AS ConstraintDefinition,
    cc.is_disabled AS IsDisabled,
    cc.is_not_trusted AS IsNotTrusted
FROM sys.check_constraints cc
WHERE
    (cc.parent_object_id = OBJECT_ID(N'dbo.PayoutRequests')
     AND cc.name = N'CK_PayoutRequests_Status')
    OR
    (cc.parent_object_id = OBJECT_ID(N'dbo.PayoutRequestStatusHistories')
     AND cc.name IN
     (
         N'CK_PayoutRequestStatusHistories_From',
         N'CK_PayoutRequestStatusHistories_To'
     ))
ORDER BY cc.name;

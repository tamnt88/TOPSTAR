SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.PayoutRequests', 'FeeAmountSnapshot') IS NULL
        ALTER TABLE dbo.PayoutRequests ADD FeeAmountSnapshot DECIMAL(18,2) NULL;
    IF COL_LENGTH('dbo.PayoutRequests', 'NetAmountSnapshot') IS NULL
        ALTER TABLE dbo.PayoutRequests ADD NetAmountSnapshot DECIMAL(18,2) NULL;

    -- Legacy payouts had no fee; preserve that known historical meaning.
    EXEC(N'UPDATE dbo.PayoutRequests
           SET FeeAmountSnapshot = COALESCE(FeeAmountSnapshot, 0),
               NetAmountSnapshot = COALESCE(NetAmountSnapshot, Amount)
           WHERE FeeAmountSnapshot IS NULL OR NetAmountSnapshot IS NULL;');
    EXEC(N'ALTER TABLE dbo.PayoutRequests ALTER COLUMN FeeAmountSnapshot DECIMAL(18,2) NOT NULL;
           ALTER TABLE dbo.PayoutRequests ALTER COLUMN NetAmountSnapshot DECIMAL(18,2) NOT NULL;');

    IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = 'CK_PayoutRequests_FinancialSnapshot')
        EXEC(N'ALTER TABLE dbo.PayoutRequests ADD CONSTRAINT CK_PayoutRequests_FinancialSnapshot
               CHECK (Amount > 0 AND FeeAmountSnapshot >= 0 AND NetAmountSnapshot > 0
                      AND FeeAmountSnapshot + NetAmountSnapshot = Amount);');

    IF OBJECT_ID('dbo.PayoutPolicies', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PayoutPolicies
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PayoutPolicies PRIMARY KEY,
            Code NVARCHAR(30) NOT NULL,
            MinimumAmount DECIMAL(18,2) NOT NULL,
            DailyRequestLimit INT NOT NULL,
            FixedFee DECIMAL(18,2) NOT NULL,
            FeeRate DECIMAL(5,4) NOT NULL,
            EffectiveAtUtc DATETIME2(7) NOT NULL,
            UpdatedByUserId UNIQUEIDENTIFIER NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            UpdatedAtUtc DATETIME2(7) NULL,
            CONSTRAINT UQ_PayoutPolicies_Code UNIQUE(Code),
            CONSTRAINT CK_PayoutPolicies_Values CHECK
                (MinimumAmount > 0 AND DailyRequestLimit BETWEEN 1 AND 20
                 AND FixedFee >= 0 AND FeeRate BETWEEN 0 AND 0.2000),
            CONSTRAINT FK_PayoutPolicies_UpdatedByUser FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id)
        );
    END;

    IF OBJECT_ID('dbo.PayoutPolicyHistories', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.PayoutPolicyHistories
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PayoutPolicyHistories PRIMARY KEY,
            PolicyId UNIQUEIDENTIFIER NOT NULL,
            MinimumAmount DECIMAL(18,2) NOT NULL,
            DailyRequestLimit INT NOT NULL,
            FixedFee DECIMAL(18,2) NOT NULL,
            FeeRate DECIMAL(5,4) NOT NULL,
            Reason NVARCHAR(500) NOT NULL,
            ActorUserId UNIQUEIDENTIFIER NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_PayoutPolicyHistories_Policy FOREIGN KEY(PolicyId) REFERENCES dbo.PayoutPolicies(Id),
            CONSTRAINT FK_PayoutPolicyHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_PayoutPolicyHistories_Values CHECK
                (MinimumAmount > 0 AND DailyRequestLimit BETWEEN 1 AND 20
                 AND FixedFee >= 0 AND FeeRate BETWEEN 0 AND 0.2000 AND LEN(Reason) >= 5)
        );
        CREATE INDEX IX_PayoutPolicyHistories_Policy_Created
            ON dbo.PayoutPolicyHistories(PolicyId, CreatedAtUtc DESC);
    END;

    DECLARE @now DATETIME2(7) = SYSUTCDATETIME();
    DECLARE @policyId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.PayoutPolicies WHERE Code = 'DEFAULT');
    IF @policyId IS NULL
    BEGIN
        SET @policyId = NEWID();
        INSERT dbo.PayoutPolicies
            (Id, Code, MinimumAmount, DailyRequestLimit, FixedFee, FeeRate, EffectiveAtUtc, CreatedAtUtc)
        VALUES (@policyId, 'DEFAULT', 100000, 3, 0, 0, @now, @now);
        INSERT dbo.PayoutPolicyHistories
            (Id, PolicyId, MinimumAmount, DailyRequestLimit, FixedFee, FeeRate, Reason, ActorUserId, CreatedAtUtc)
        VALUES (NEWID(), @policyId, 100000, 3, 0, 0, N'Khởi tạo chính sách mặc định', NULL, @now);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_PayoutPolicyHistories_AppendOnly
    ON dbo.PayoutPolicyHistories AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N''Lịch sử chính sách rút tiền chỉ được phép ghi thêm.'', 1;
    END;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_PayoutRequests_FinancialSnapshotImmutable
    ON dbo.PayoutRequests AFTER UPDATE AS
    BEGIN
        SET NOCOUNT ON;
        IF UPDATE(Amount) OR UPDATE(FeeAmountSnapshot) OR UPDATE(NetAmountSnapshot)
           IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
                      WHERE i.Amount<>d.Amount OR i.FeeAmountSnapshot<>d.FeeAmountSnapshot
                         OR i.NetAmountSnapshot<>d.NetAmountSnapshot)
              THROW 51000, N''Snapshot tài chính của payout không được thay đổi.'', 1;
    END;');

    IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code = 'MENU_ADMIN_PAYOUT_POLICY')
        INSERT dbo.Menus(Id, Code, Title, Path, Icon, ParentId, OrderNo, IsVisible, CreatedAtUtc)
        VALUES(NEWID(), 'MENU_ADMIN_PAYOUT_POLICY', N'Chính sách rút tiền',
               '/Admin/Finance/PayoutPolicy', 'fa-solid fa-sliders', NULL, 12, 1, @now);
    DECLARE @roleId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Roles WHERE UPPER(Code) = 'ADMIN');
    DECLARE @menuId UNIQUEIDENTIFIER = (SELECT TOP 1 Id FROM dbo.Menus WHERE Code = 'MENU_ADMIN_PAYOUT_POLICY');
    IF @roleId IS NOT NULL AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus WHERE RoleId=@roleId AND MenuId=@menuId)
        INSERT dbo.RoleMenus(RoleId, MenuId, CreatedAtUtc) VALUES(@roleId, @menuId, @now);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

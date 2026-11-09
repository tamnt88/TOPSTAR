SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;

UPDATE dbo.Users
SET PasswordHash = N'PBKDF2$100000$vFIOXCKcqzFlK18/BPIXcQ==$ydvsRa6XngROmq1HcpCR0th1IuftQ6tt4w4VawPR9sA=',
    Status = N'Active',
    EmailConfirmed = 1,
    PhoneConfirmed = 1,
    FailedLoginCount = 0,
    LockoutEndAtUtc = NULL,
    PasswordChangedAtUtc = SYSUTCDATETIME(),
    UpdatedAtUtc = SYSUTCDATETIME()
WHERE Email = N'brand@topstar.local';

IF @@ROWCOUNT <> 1
    THROW 51000, N'Không tìm thấy duy nhất tài khoản brand@topstar.local.', 1;

DECLARE @ProvinceId UNIQUEIDENTIFIER = 'BBE41CF1-CB00-4E2F-AE71-6E9C473711BD';
DECLARE @WardId UNIQUEIDENTIFIER = '87B481A1-DFB8-4AF2-A26F-027939E69372';

UPDATE b
SET StreetAddress = N'77 Tạ Hiện',
    ProvinceId = @ProvinceId,
    WardId = @WardId,
    BusinessLicenseFileUrl = N'/uploads/brand-license/9afa923773f7441e9042739bdd3a8063.pdf',
    UpdatedAtUtc = SYSUTCDATETIME()
FROM dbo.BrandProfiles b
JOIN dbo.Users u ON u.Id = b.UserId
WHERE u.Email = N'brand@topstar.local';

COMMIT TRANSACTION;
GO

SELECT Email, Status, EmailConfirmed, PhoneConfirmed, FailedLoginCount,
       LEN(PasswordHash) AS PasswordHashLength
FROM dbo.Users
WHERE Email = N'brand@topstar.local';
GO

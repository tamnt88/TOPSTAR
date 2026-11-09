SET NOCOUNT ON; SET XACT_ABORT ON; SET ANSI_NULLS ON; SET QUOTED_IDENTIFIER ON;
BEGIN TRY
BEGIN TRANSACTION;
IF OBJECT_ID('dbo.PaymentMethodConfigurations','U') IS NULL
CREATE TABLE dbo.PaymentMethodConfigurations(
 Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_PaymentMethodConfigurations PRIMARY KEY,
 BankTransferEnabled BIT NOT NULL CONSTRAINT DF_PaymentConfig_Bank DEFAULT(1),
 BankName NVARCHAR(150) NULL, BankAccountNumber NVARCHAR(50) NULL, BankAccountHolder NVARCHAR(150) NULL,
 OnePayEnabled BIT NOT NULL CONSTRAINT DF_PaymentConfig_OnePay DEFAULT(0),
 OnePayDomesticEnabled BIT NOT NULL CONSTRAINT DF_PaymentConfig_Domestic DEFAULT(1),
 OnePayInternationalEnabled BIT NOT NULL CONSTRAINT DF_PaymentConfig_International DEFAULT(1),
 OnePayEWalletEnabled BIT NOT NULL CONSTRAINT DF_PaymentConfig_EWallet DEFAULT(1),
 OnePayEnvironment VARCHAR(20) NOT NULL CONSTRAINT DF_PaymentConfig_Env DEFAULT('Test'),
 OnePayMerchantId NVARCHAR(100) NULL, OnePayAccessCode NVARCHAR(200) NULL, OnePaySecureHashCiphertext NVARCHAR(2000) NULL,
 OnePayBaseUrl NVARCHAR(500) NULL,
 OnePayReturnUrl NVARCHAR(500) NOT NULL CONSTRAINT DF_PaymentConfig_Return DEFAULT('https://topstar.vn/Brand/OnePayReturn'),
 OnePayCallbackUrl NVARCHAR(500) NOT NULL CONSTRAINT DF_PaymentConfig_Callback DEFAULT('https://topstar.vn/api/payments/onepay/ipn'),
 UpdatedByUserId UNIQUEIDENTIFIER NULL, CreatedAtUtc DATETIME2 NOT NULL, UpdatedAtUtc DATETIME2 NULL,
 CONSTRAINT CK_PaymentConfig_Environment CHECK(OnePayEnvironment IN('Test','Production')),
 CONSTRAINT FK_PaymentConfig_UpdatedBy FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id)
);
IF NOT EXISTS(SELECT 1 FROM dbo.PaymentMethodConfigurations)
INSERT dbo.PaymentMethodConfigurations(Id,CreatedAtUtc) VALUES(NEWID(),SYSUTCDATETIME());
DECLARE @parent UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS');
DECLARE @menu UNIQUEIDENTIFIER=(SELECT TOP 1 Id FROM dbo.Menus WHERE Code='MENU_ADMIN_SETTINGS_PAYMENTS');
IF @menu IS NULL BEGIN SET @menu=NEWID(); INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc)
VALUES(@menu,'MENU_ADMIN_SETTINGS_PAYMENTS',N'Phương thức thanh toán','/Admin/Settings/PaymentMethods','fa-solid fa-credit-card',@parent,90,1,SYSUTCDATETIME()); END;
INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc)
SELECT r.Id,@menu,SYSUTCDATETIME() FROM dbo.Roles r WHERE r.Code='Admin'
AND NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=r.Id AND rm.MenuId=@menu);
COMMIT;
END TRY BEGIN CATCH IF @@TRANCOUNT>0 ROLLBACK; THROW; END CATCH;
GO

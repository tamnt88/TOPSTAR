SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.Campaigns','U') IS NULL THROW 51120,'Campaigns does not exist.',1;
 IF OBJECT_ID('dbo.CampaignBillingInvoices','U') IS NULL
 BEGIN
  CREATE TABLE dbo.CampaignBillingInvoices(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignBillingInvoices PRIMARY KEY,CampaignId UNIQUEIDENTIFIER NOT NULL,InvoiceNumber NVARCHAR(40) NOT NULL,CampaignBudgetSnapshot DECIMAL(18,2) NOT NULL,FeeRate DECIMAL(5,4) NOT NULL,FeeAmount DECIMAL(18,2) NOT NULL,Status NVARCHAR(10) NOT NULL,IssuedAtUtc DATETIME2(7) NOT NULL,DueAtUtc DATETIME2(7) NOT NULL,PaidAtUtc DATETIME2(7) NULL,PaymentReference NVARCHAR(500) NULL,VoidReason NVARCHAR(500) NULL,CreatedByUserId UNIQUEIDENTIFIER NOT NULL,UpdatedByUserId UNIQUEIDENTIFIER NULL,CreatedAtUtc DATETIME2(7) NOT NULL,UpdatedAtUtc DATETIME2(7) NULL,CONSTRAINT UQ_CampaignBillingInvoices_Campaign UNIQUE(CampaignId),CONSTRAINT UQ_CampaignBillingInvoices_Number UNIQUE(InvoiceNumber),CONSTRAINT FK_CampaignBillingInvoices_Campaigns FOREIGN KEY(CampaignId) REFERENCES dbo.Campaigns(Id),CONSTRAINT FK_CampaignBillingInvoices_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),CONSTRAINT FK_CampaignBillingInvoices_UpdatedBy FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_CampaignBillingInvoices_Amounts CHECK(CampaignBudgetSnapshot>0 AND FeeRate=0.1500 AND FeeAmount=ROUND(CampaignBudgetSnapshot*FeeRate,0)),CONSTRAINT CK_CampaignBillingInvoices_Status CHECK(Status IN('Pending','Paid','Overdue','Void')),CONSTRAINT CK_CampaignBillingInvoices_Dates CHECK(DueAtUtc>IssuedAtUtc AND (PaidAtUtc IS NULL OR PaidAtUtc>=IssuedAtUtc)),CONSTRAINT CK_CampaignBillingInvoices_Result CHECK((Status='Paid' AND PaidAtUtc IS NOT NULL AND LEN(LTRIM(RTRIM(PaymentReference)))>0 AND VoidReason IS NULL) OR (Status='Void' AND PaidAtUtc IS NULL AND PaymentReference IS NULL AND LEN(LTRIM(RTRIM(VoidReason)))>0) OR (Status IN('Pending','Overdue') AND PaidAtUtc IS NULL AND PaymentReference IS NULL AND VoidReason IS NULL)));
  CREATE INDEX IX_CampaignBillingInvoices_Status_Due ON dbo.CampaignBillingInvoices(Status,DueAtUtc) INCLUDE(CampaignId,FeeAmount,InvoiceNumber);
 END;
 IF OBJECT_ID('dbo.CampaignBillingInvoiceHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.CampaignBillingInvoiceHistories(Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_CampaignBillingInvoiceHistories PRIMARY KEY,InvoiceId UNIQUEIDENTIFIER NOT NULL,FromStatus NVARCHAR(10) NULL,ToStatus NVARCHAR(10) NOT NULL,Note NVARCHAR(500) NULL,ActorUserId UNIQUEIDENTIFIER NOT NULL,CreatedAtUtc DATETIME2(7) NOT NULL,CONSTRAINT FK_CampaignBillingInvoiceHistories_Invoices FOREIGN KEY(InvoiceId) REFERENCES dbo.CampaignBillingInvoices(Id),CONSTRAINT FK_CampaignBillingInvoiceHistories_Users FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),CONSTRAINT CK_CampaignBillingInvoiceHistories_Status CHECK(ToStatus IN('Pending','Paid','Overdue','Void') AND (FromStatus IS NULL OR FromStatus IN('Pending','Paid','Overdue','Void'))));
  CREATE INDEX IX_CampaignBillingInvoiceHistories_Invoice_Created ON dbo.CampaignBillingInvoiceHistories(InvoiceId,CreatedAtUtc);
 END;
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignBillingInvoiceHistories_AppendOnly ON dbo.CampaignBillingInvoiceHistories AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM deleted) THROW 51121,''Billing invoice history is append-only.'',1; END;');
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_CampaignBillingInvoices_ImmutableTerms ON dbo.CampaignBillingInvoices AFTER UPDATE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON i.Id=d.Id WHERE i.CampaignId<>d.CampaignId OR i.InvoiceNumber<>d.InvoiceNumber OR i.CampaignBudgetSnapshot<>d.CampaignBudgetSnapshot OR i.FeeRate<>d.FeeRate OR i.FeeAmount<>d.FeeAmount OR i.IssuedAtUtc<>d.IssuedAtUtc OR i.DueAtUtc<>d.DueAtUtc OR i.CreatedByUserId<>d.CreatedByUserId OR i.CreatedAtUtc<>d.CreatedAtUtc) THROW 51122,''Billing invoice terms are immutable.'',1; END;');
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_ADMIN_BILLING') INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_ADMIN_BILLING',N'Phí dịch vụ','/Admin/Finance/Billing','fa-solid fa-file-invoice-dollar',NULL,10,1,@now);
 IF NOT EXISTS(SELECT 1 FROM dbo.Menus WHERE Code='MENU_BRAND_BILLING') INSERT dbo.Menus(Id,Code,Title,Path,Icon,ParentId,OrderNo,IsVisible,CreatedAtUtc) VALUES(NEWID(),'MENU_BRAND_BILLING',N'Phí dịch vụ','/Brand/Billing','fa-solid fa-file-invoice',NULL,10,1,@now);
 INSERT dbo.RoleMenus(RoleId,MenuId,CreatedAtUtc) SELECT r.Id,m.Id,@now FROM dbo.Roles r JOIN dbo.Menus m ON (UPPER(r.Code)='ADMIN' AND m.Code='MENU_ADMIN_BILLING') OR (UPPER(r.Code)='BRAND' AND m.Code='MENU_BRAND_BILLING') WHERE NOT EXISTS(SELECT 1 FROM dbo.RoleMenus rm WHERE rm.RoleId=r.Id AND rm.MenuId=m.Id);
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

SET NOCOUNT ON;SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.DisputeCases','U') IS NULL THROW 51039,'Run migration 34 before migration 37.',1;
 IF OBJECT_ID('dbo.WalletTransactionStatusHistories','U') IS NULL THROW 51040,'Run migration 27 before migration 37.',1;
 IF COL_LENGTH('dbo.WalletTransactions','HoldDisputeCaseId') IS NULL EXEC(N'ALTER TABLE dbo.WalletTransactions ADD HoldDisputeCaseId UNIQUEIDENTIFIER NULL;');
 IF COL_LENGTH('dbo.WalletTransactionStatusHistories','DisputeCaseId') IS NULL EXEC(N'ALTER TABLE dbo.WalletTransactionStatusHistories ADD DisputeCaseId UNIQUEIDENTIFIER NULL;');
 IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_WalletTransactions_HoldDisputeCase') EXEC(N'ALTER TABLE dbo.WalletTransactions ADD CONSTRAINT FK_WalletTransactions_HoldDisputeCase FOREIGN KEY(HoldDisputeCaseId) REFERENCES dbo.DisputeCases(Id);');
 IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE name='FK_WalletTransactionStatusHistories_DisputeCase') EXEC(N'ALTER TABLE dbo.WalletTransactionStatusHistories ADD CONSTRAINT FK_WalletTransactionStatusHistories_DisputeCase FOREIGN KEY(DisputeCaseId) REFERENCES dbo.DisputeCases(Id);');
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.WalletTransactions') AND name='IX_WalletTransactions_HoldDisputeCase') EXEC(N'CREATE INDEX IX_WalletTransactions_HoldDisputeCase ON dbo.WalletTransactions(HoldDisputeCaseId) WHERE HoldDisputeCaseId IS NOT NULL;');
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.WalletTransactionStatusHistories') AND name='IX_WalletTransactionStatusHistories_DisputeCase') EXEC(N'CREATE INDEX IX_WalletTransactionStatusHistories_DisputeCase ON dbo.WalletTransactionStatusHistories(DisputeCaseId,CreatedAtUtc) WHERE DisputeCaseId IS NOT NULL;');
 IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.WalletTransactions') AND name='CK_WalletTransactions_HoldDisputeCase') EXEC(N'ALTER TABLE dbo.WalletTransactions ADD CONSTRAINT CK_WalletTransactions_HoldDisputeCase CHECK(HoldDisputeCaseId IS NULL OR Status=''OnHold'');');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

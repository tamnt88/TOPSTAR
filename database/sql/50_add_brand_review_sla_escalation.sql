SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;
 IF OBJECT_ID('dbo.OperationalSlaPolicies','U') IS NULL THROW 51070,'Run migrations 46 and 47 first.',1;
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.OperationalEscalations') AND name='CK_OperationalEscalations_Rule') ALTER TABLE dbo.OperationalEscalations DROP CONSTRAINT CK_OperationalEscalations_Rule;
 ALTER TABLE dbo.OperationalEscalations WITH CHECK ADD CONSTRAINT CK_OperationalEscalations_Rule CHECK(((RuleCode='SUBMISSION_OVERDUE' OR RuleCode='BRAND_REVIEW_SLA') AND SourceEntityType='Submission' AND SubmissionId=SourceEntityId AND DisputeCaseId IS NULL) OR (RuleCode='DISPUTE_SLA' AND SourceEntityType='DisputeCase' AND DisputeCaseId=SourceEntityId AND SubmissionId IS NULL));
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.OperationalScanRuns') AND name='CK_OperationalScanRuns_Rule') ALTER TABLE dbo.OperationalScanRuns DROP CONSTRAINT CK_OperationalScanRuns_Rule;
 ALTER TABLE dbo.OperationalScanRuns WITH CHECK ADD CONSTRAINT CK_OperationalScanRuns_Rule CHECK(RuleCode IN('SUBMISSION_OVERDUE','BRAND_REVIEW_SLA','DISPUTE_SLA'));
 IF EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.OperationalSlaPolicies') AND name='CK_OperationalSlaPolicies_Rule') ALTER TABLE dbo.OperationalSlaPolicies DROP CONSTRAINT CK_OperationalSlaPolicies_Rule;
 ALTER TABLE dbo.OperationalSlaPolicies WITH CHECK ADD CONSTRAINT CK_OperationalSlaPolicies_Rule CHECK(RuleCode IN('SUBMISSION_OVERDUE','BRAND_REVIEW_SLA','DISPUTE_SLA'));
 DECLARE @now DATETIME2(7)=SYSUTCDATETIME();
 IF NOT EXISTS(SELECT 1 FROM dbo.OperationalSlaPolicies WHERE RuleCode='BRAND_REVIEW_SLA')
 BEGIN
  DECLARE @id UNIQUEIDENTIFIER=NEWID();INSERT dbo.OperationalSlaPolicies(Id,RuleCode,HighAfterHours,CriticalAfterHours,IsActive,CreatedAtUtc) VALUES(@id,'BRAND_REVIEW_SLA',48,72,1,@now);INSERT dbo.OperationalSlaPolicyHistories(Id,PolicyId,ToHighAfterHours,ToCriticalAfterHours,ToIsActive,Note,CreatedAtUtc) VALUES(NEWID(),@id,48,72,1,N'Khởi tạo SLA phản hồi bài nộp của Brand',@now);
 END;
 IF NOT EXISTS(SELECT 1 FROM sys.indexes WHERE object_id=OBJECT_ID('dbo.OperationalEscalations') AND name='IX_OperationalEscalations_BrandReviewQueue') CREATE INDEX IX_OperationalEscalations_BrandReviewQueue ON dbo.OperationalEscalations(Status,Severity,FirstDetectedAtUtc) INCLUDE(SubmissionId,LastDetectedAtUtc) WHERE RuleCode='BRAND_REVIEW_SLA';
 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_OperationalEscalations_ValidateSource ON dbo.OperationalEscalations AFTER INSERT,UPDATE AS BEGIN SET NOCOUNT ON; IF EXISTS(SELECT 1 FROM inserted i WHERE i.RuleCode IN(''SUBMISSION_OVERDUE'',''BRAND_REVIEW_SLA'') AND NOT EXISTS(SELECT 1 FROM dbo.Submissions s WHERE s.Id=i.SubmissionId)) THROW 51052,''Escalation source submission does not exist.'',1; IF EXISTS(SELECT 1 FROM inserted i WHERE i.RuleCode=''DISPUTE_SLA'' AND NOT EXISTS(SELECT 1 FROM dbo.DisputeCases d WHERE d.Id=i.DisputeCaseId)) THROW 51056,''Escalation source dispute does not exist.'',1; END;');
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

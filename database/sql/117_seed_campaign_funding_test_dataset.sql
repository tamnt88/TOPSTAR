/*
 TOPSTAR - Campaign funding lifecycle QA dataset
 Account: brand@topstar.local / Topstar@123
 Safety: deterministic IDs; inserts only missing SEED-CF rows; never deletes or
 overwrites a funding/order that the tester has already manipulated.
 Prerequisite: 114, 116.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @now datetime2(7)=SYSUTCDATETIME();
 DECLARE @brand uniqueidentifier='A3C37ED9-F881-4FC8-AB2D-C18FFAA5E7F2';
 DECLARE @brandUser uniqueidentifier='465397EA-DDBD-47DC-AF44-BF9575B4D08E';
 IF NOT EXISTS(SELECT 1 FROM dbo.BrandProfiles WHERE Id=@brand)
   THROW 51000,N'Chưa có Brand demo. Hãy chạy 104_seed_brand_portal_test_dataset.sql trước.',1;
 IF OBJECT_ID('dbo.CampaignFundings','U') IS NULL OR COL_LENGTH('dbo.CampaignFundings','VersionNo') IS NULL
   THROW 51000,N'Chưa có schema quỹ chiến dịch. Hãy chạy 114 và 116 trước.',1;

 DECLARE @k1 uniqueidentifier,@k2 uniqueidentifier;
 SELECT @k1=MIN(Id),@k2=MAX(Id) FROM (SELECT TOP(2) k.Id FROM dbo.KocProfiles k JOIN dbo.Users u ON u.Id=k.UserId WHERE k.Status=N'Approved' AND u.Status=N'Active' ORDER BY k.Id) q;
 IF @k1 IS NULL OR @k2 IS NULL OR @k1=@k2 THROW 51000,N'Cần ít nhất 2 KOC Active/Approved để tạo dữ liệu kiểm thử.',1;

 DECLARE @seed TABLE(No int PRIMARY KEY,CampaignId uniqueidentifier,FundingId uniqueidentifier,Title nvarchar(200),Budget decimal(18,2),Mode nvarchar(10),FundingStatus nvarchar(30),PaymentMethod nvarchar(20),CampaignStatus nvarchar(30));
 INSERT @seed VALUES
 (1,'CF000001-2026-4000-8000-000000000001','CF100001-2026-4000-8000-000000000001',N'[QA Quỹ] Phân bổ thù lao nháp',10000000,N'Equal',N'Draft',N'BankTransfer',N'Published'),
 (2,'CF000002-2026-4000-8000-000000000002','CF100002-2026-4000-8000-000000000002',N'[QA Quỹ] Chờ chuyển khoản',12000000,N'Equal',N'Pending',N'BankTransfer',N'Published'),
 (3,'CF000003-2026-4000-8000-000000000003','CF100003-2026-4000-8000-000000000003',N'[QA Quỹ] Chờ OnePay',16000000,N'Custom',N'Pending',N'OnePay',N'Published'),
 (4,'CF000004-2026-4000-8000-000000000004','CF100004-2026-4000-8000-000000000004',N'[QA Quỹ] Đã thanh toán chia đều',20000000,N'Equal',N'Paid',N'BankTransfer',N'InProgress'),
 (5,'CF000005-2026-4000-8000-000000000005','CF100005-2026-4000-8000-000000000005',N'[QA Quỹ] Đã thanh toán thù lao riêng',30000000,N'Custom',N'Paid',N'OnePay',N'InProgress'),
 (6,'CF000006-2026-4000-8000-000000000006','CF100006-2026-4000-8000-000000000006',N'[QA Quỹ] Đã nghiệm thu và giải ngân',24000000,N'Equal',N'Released',N'BankTransfer',N'Completed');

 INSERT dbo.Campaigns(Id,BrandProfileId,Title,Objective,BudgetAmount,PackageCode,RequiresBooking,RequiresShipment,ExecutionMode,Status,StartDate,EndDate,ViewCount,IsVisible,CreatedAtUtc,UpdatedAtUtc)
 SELECT s.CampaignId,@brand,s.Title,N'<p>Dữ liệu QA cho vòng đời thanh toán và thù lao KOC.</p>',s.Budget,N'SELECT_20M',0,0,N'LivestreamOnline',s.CampaignStatus,CAST(@now AS date),DATEADD(day,30,CAST(@now AS date)),0,1,@now,@now
 FROM @seed s WHERE NOT EXISTS(SELECT 1 FROM dbo.Campaigns c WHERE c.Id=s.CampaignId);

 DECLARE @participants TABLE(No int,KocNo int,Id uniqueidentifier,CampaignId uniqueidentifier,KocId uniqueidentifier,PRIMARY KEY(No,KocNo));
 INSERT @participants
 SELECT s.No,k.KocNo,CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT('SEED-CF-P-',s.No,'-',k.KocNo))),s.CampaignId,k.KocId
 FROM @seed s CROSS APPLY(VALUES(1,@k1),(2,@k2)) k(KocNo,KocId);
 INSERT dbo.Participations(Id,CampaignId,KocProfileId,Status,ConfirmedAtUtc,CreatedAtUtc)
 SELECT p.Id,p.CampaignId,p.KocId,CASE WHEN p.No=6 THEN N'Completed' ELSE N'Confirmed' END,DATEADD(day,-10,@now),DATEADD(day,-10,@now)
 FROM @participants p WHERE NOT EXISTS(SELECT 1 FROM dbo.Participations x WHERE x.Id=p.Id);

 INSERT dbo.CampaignFundings(Id,CampaignId,VersionNo,AllocationMode,Status,OrderNumber,PaymentMethod,CampaignBudgetSnapshot,FeeRateSnapshot,FixedFeeSnapshot,PlatformFeeSnapshot,GatewayFeeRateSnapshot,GatewayFeeSnapshot,GatewayFeeBearerSnapshot,TotalPayableSnapshot,DisputeHoldDaysSnapshot,PaymentDueDaysSnapshot,RefundPolicySnapshot,CreatedAtUtc)
 SELECT s.FundingId,s.CampaignId,1,s.Mode,N'Draft',CASE WHEN s.FundingStatus=N'Draft' THEN NULL ELSE CONCAT(N'SEED-CF-',s.No,N'-V1') END,s.PaymentMethod,s.Budget,.15,0,s.Budget*.15,0,0,N'Brand',s.Budget*1.15,7,7,N'Hoàn phần ngân sách chưa giải ngân sau khi khấu trừ nghĩa vụ đã nghiệm thu.',@now
 FROM @seed s WHERE NOT EXISTS(SELECT 1 FROM dbo.CampaignFundings f WHERE f.Id=s.FundingId);

 INSERT dbo.CampaignFundingAllocations(Id,FundingId,ParticipationId,Amount,AcceptedAmount,RefundedAmount,Status,SettlementReason,ReleasedAtUtc,WalletTransactionId,CreatedAtUtc)
 SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT('SEED-CF-A-',s.No,'-',p.KocNo))),s.FundingId,p.Id,
   CASE WHEN s.Mode=N'Custom' AND p.KocNo=1 THEN s.Budget*.6 WHEN s.Mode=N'Custom' THEN s.Budget*.4 ELSE s.Budget/2 END,
   CASE WHEN s.FundingStatus=N'Released' THEN s.Budget/2 ELSE 0 END,0,
   CASE WHEN s.FundingStatus=N'Released' THEN N'Accepted' ELSE N'Active' END,
   CASE WHEN s.FundingStatus=N'Released' THEN N'Nghiệm thu hoàn tất bằng dữ liệu QA.' ELSE NULL END,
   CASE WHEN s.FundingStatus=N'Released' THEN DATEADD(day,-8,@now) ELSE NULL END,NULL,@now
 FROM @seed s JOIN @participants p ON p.No=s.No
 WHERE NOT EXISTS(SELECT 1 FROM dbo.CampaignFundingAllocations a WHERE a.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT('SEED-CF-A-',s.No,'-',p.KocNo))));

 UPDATE f SET Status=s.FundingStatus,SubmittedAtUtc=CASE WHEN s.FundingStatus=N'Draft' THEN NULL ELSE @now END,DueAtUtc=CASE WHEN s.FundingStatus=N'Draft' THEN NULL ELSE DATEADD(day,7,@now) END,
   PaidAtUtc=CASE WHEN s.FundingStatus IN(N'Paid',N'Released') THEN DATEADD(day,-10,@now) END,PaymentReference=CASE WHEN s.FundingStatus IN(N'Paid',N'Released') THEN CONCAT(N'SEED-PAID-',s.No) END,UpdatedByUserId=@brandUser,UpdatedAtUtc=@now
 FROM dbo.CampaignFundings f JOIN @seed s ON s.FundingId=f.Id WHERE f.Status=N'Draft' AND s.FundingStatus<>N'Draft';

 INSERT dbo.CampaignFundLedgers(Id,FundingId,AllocationId,EntryType,Amount,Note,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT('SEED-CF-L-',s.No))),s.FundingId,NULL,
   CASE WHEN s.FundingStatus=N'Draft' THEN N'DraftCreated' WHEN s.FundingStatus=N'Pending' THEN N'PaymentRequested' WHEN s.FundingStatus=N'Paid' THEN N'PaymentConfirmed' ELSE N'FundingReleased' END,
   CASE WHEN s.FundingStatus=N'Draft' THEN 0 ELSE s.Budget END,N'Dữ liệu QA vòng đời quỹ chiến dịch.',@brandUser,@now
 FROM @seed s WHERE NOT EXISTS(SELECT 1 FROM dbo.CampaignFundLedgers l WHERE l.Id=CONVERT(uniqueidentifier,HASHBYTES('MD5',CONCAT('SEED-CF-L-',s.No))));

 COMMIT;
 SELECT s.No,s.Title,f.AllocationMode,f.Status,f.PaymentMethod,f.CampaignBudgetSnapshot,COUNT(a.Id) KocCount,SUM(a.Amount) Allocated
 FROM @seed s JOIN dbo.CampaignFundings f ON f.Id=s.FundingId LEFT JOIN dbo.CampaignFundingAllocations a ON a.FundingId=f.Id
 GROUP BY s.No,s.Title,f.AllocationMode,f.Status,f.PaymentMethod,f.CampaignBudgetSnapshot ORDER BY s.No;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT>0 ROLLBACK;
 THROW;
END CATCH;

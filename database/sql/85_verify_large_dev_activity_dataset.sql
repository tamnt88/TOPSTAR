/* Read-only verification for seed 84. Safe in every environment. */
SET NOCOUNT ON;

SELECT
 (SELECT COUNT(*) FROM dbo.Users WHERE Email LIKE 'seed.koc.%@seed.topstar.local') SeedKocUsers,
 (SELECT COUNT(*) FROM dbo.Users WHERE Email LIKE 'seed.brand.%@seed.topstar.local') SeedBrandUsers,
 (SELECT COUNT(*) FROM dbo.Campaigns WHERE Title LIKE N'Chiến dịch Seed %') SeedCampaigns,
 (SELECT COUNT(*) FROM dbo.Campaigns WHERE Title LIKE N'Chiến dịch Seed %' AND Status='Published' AND IsVisible=1 AND StartDate>CONVERT(DATE,SYSUTCDATETIME()) AND (EndDate IS NULL OR EndDate>=StartDate)) SeedFuturePublishedCampaigns,
 (SELECT COUNT(*) FROM dbo.Applications WHERE PitchNote=N'KOC quan tâm chiến dịch và có kinh nghiệm phù hợp.') SeedApplications,
 (SELECT COUNT(*) FROM dbo.ParticipationStatusHistories WHERE Note=N'Bản ghi seed lịch sử hợp tác.') SeedCollaborations,
 (SELECT COUNT(*) FROM dbo.WalletTransactions WHERE Description=N'Thu nhập từ chiến dịch seed.') SeedWalletTransactions,
 (SELECT COUNT(*) FROM dbo.UserViolations WHERE Reason=N'Vi phạm seed có căn cứ để kiểm thử màn hình và lịch sử.') SeedViolations,
 (SELECT COUNT(*) FROM dbo.AuthEvents WHERE UserAgent IN (N'Mozilla/5.0 Topstar Seed Browser',N'Mozilla/5.0 Topstar Seed Brand Browser')) SeedAuthEvents,
 (SELECT COUNT(*) FROM dbo.CampaignStatusHistories WHERE Note=N'Khởi tạo lịch sử chiến dịch từ bộ dữ liệu seed.') SeedCampaignHistories,
 (SELECT COUNT(*) FROM dbo.ReviewStatusHistories WHERE Reason=N'Khởi tạo lịch sử đánh giá từ bộ dữ liệu seed.') SeedReviewHistories,
 (SELECT COUNT(*) FROM dbo.WalletTransactionStatusHistories WHERE Reason=N'Khởi tạo lịch sử giao dịch từ bộ dữ liệu seed.') SeedWalletHistories,
 (SELECT COUNT(*) FROM dbo.AuditLogs WHERE ActionName='SeedCreated' AND PayloadJson LIKE N'%"source":"seed84"%') SeedAuditLogs;

SELECT TOP (20) p.DisplayName,u.Email,u.Status AccountStatus,p.Status ProfileStatus,p.Followers,p.Score,
 (SELECT COUNT(*) FROM dbo.KocSocialAccounts s WHERE s.KocProfileId=p.Id) SocialCount,
 (SELECT COUNT(*) FROM dbo.KocPortfolios f WHERE f.KocProfileId=p.Id) PortfolioCount,
 (SELECT COUNT(*) FROM dbo.KocMediaAssets m WHERE m.KocProfileId=p.Id) MediaCount,
 (SELECT COUNT(*) FROM dbo.Applications a WHERE a.KocProfileId=p.Id) ApplicationCount,
 (SELECT COUNT(*) FROM dbo.Participations x WHERE x.KocProfileId=p.Id) CollaborationCount,
 (SELECT COUNT(*) FROM dbo.WalletTransactions w WHERE w.KocProfileId=p.Id) WalletCount
FROM dbo.KocProfiles p JOIN dbo.Users u ON u.Id=p.UserId
WHERE u.Email LIKE 'seed.koc.%@seed.topstar.local'
ORDER BY p.DisplayName;

SELECT 'KOC profile status' Dimension,p.Status,COUNT(*) Total FROM dbo.KocProfiles p
JOIN dbo.Users u ON u.Id=p.UserId WHERE u.Email LIKE 'seed.koc.%@seed.topstar.local' GROUP BY p.Status
UNION ALL
SELECT 'Brand profile status',p.Status,COUNT(*) FROM dbo.BrandProfiles p
JOIN dbo.Users u ON u.Id=p.UserId WHERE u.Email LIKE 'seed.brand.%@seed.topstar.local' GROUP BY p.Status
UNION ALL
SELECT 'Campaign status',c.Status,COUNT(*) FROM dbo.Campaigns c WHERE c.Title LIKE N'Chiến dịch Seed %' GROUP BY c.Status;

SELECT TOP (20) p.CompanyName,u.Email,u.Status AccountStatus,p.Status ProfileStatus,p.Industry,
 (SELECT COUNT(*) FROM dbo.Campaigns c WHERE c.BrandProfileId=p.Id) CampaignCount,
 (SELECT COUNT(*) FROM dbo.AuthEvents e WHERE e.UserId=u.Id) AuthEventCount,
 (SELECT COUNT(*) FROM dbo.AuditLogs a WHERE a.EntityName='BrandProfile' AND a.EntityId=CONVERT(NVARCHAR(36),p.Id)) AuditCount
FROM dbo.BrandProfiles p JOIN dbo.Users u ON u.Id=p.UserId
WHERE u.Email LIKE 'seed.brand.%@seed.topstar.local'
ORDER BY p.CompanyName;

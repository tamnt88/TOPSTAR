/*
 Topstar - Large DEV/QA dataset
 Creates: 300 KOC, 250 Brand, 300 campaigns and representative activity/history.
 Safe properties:
 - INSERT only; never updates/deletes existing business data.
 - Deterministic IDs + seed.topstar.local email domain make the script idempotent.
 - Intended for DEV/QA only. Do not run in production.
 Login password for seeded users: Topstar@123
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;

IF DB_NAME() NOT IN (N'TopstarDb', N'TopstarDb_Dev', N'TopstarDb_Test')
    THROW 51000, N'Seed 84 chỉ được chạy trên database DEV/TEST Topstar.', 1;

DECLARE @RoleKoc UNIQUEIDENTIFIER = (SELECT TOP (1) Id FROM dbo.Roles WHERE UPPER(Code) = 'KOC');
DECLARE @RoleBrand UNIQUEIDENTIFIER = (SELECT TOP (1) Id FROM dbo.Roles WHERE UPPER(Code) = 'BRAND');
DECLARE @AdminUser UNIQUEIDENTIFIER = (
    SELECT TOP (1) u.Id FROM dbo.Users u
    JOIN dbo.UserRoles ur ON ur.UserId=u.Id JOIN dbo.Roles r ON r.Id=ur.RoleId
    WHERE UPPER(r.Code)='ADMIN' ORDER BY u.CreatedAtUtc
);
IF @RoleKoc IS NULL OR @RoleBrand IS NULL OR @AdminUser IS NULL
    THROW 51001, N'Thiếu role KOC/BRAND hoặc tài khoản Admin. Hãy chạy các migration/seed nền trước.', 1;

DECLARE @PasswordHash NVARCHAR(512)=N'PBKDF2$100000$vFIOXCKcqzFlK18/BPIXcQ==$ydvsRa6XngROmq1HcpCR0th1IuftQ6tt4w4VawPR9sA=';
DECLARE @Now DATETIME2(7)=SYSUTCDATETIME();

DROP TABLE IF EXISTS #N;
;WITH n AS (
    SELECT 1 AS No UNION ALL SELECT No+1 FROM n WHERE No < 300
)
SELECT No INTO #N FROM n OPTION (MAXRECURSION 0);

DROP TABLE IF EXISTS #KocSeed;
SELECT n.No,
       CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-KOC-USER-',n.No))) UserId,
       CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-KOC-PROFILE-',n.No))) ProfileId,
       CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-KOC-BODY-',n.No))) BodyId
INTO #KocSeed FROM #N n;

DROP TABLE IF EXISTS #BrandSeed;
SELECT n.No,
       CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-BRAND-USER-',n.No))) UserId,
       CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-BRAND-PROFILE-',n.No))) ProfileId
INTO #BrandSeed FROM #N n WHERE n.No<=250;

DROP TABLE IF EXISTS #CampaignSeed;
SELECT n.No,
       CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-CAMPAIGN-',n.No))) CampaignId,
       b.ProfileId BrandProfileId
INTO #CampaignSeed FROM #N n JOIN #BrandSeed b ON b.No=((n.No-1)%250)+1;

BEGIN TRY
 BEGIN TRANSACTION;

 /* Users and roles */
 INSERT dbo.Users(Id,Email,Phone,Gender,DateOfBirth,PasswordHash,Status,EmailConfirmed,PhoneConfirmed,LastLoginAtUtc,PasswordChangedAtUtc,FailedLoginCount,CreatedAtUtc)
 SELECT s.UserId,CONCAT('seed.koc.',FORMAT(s.No,'0000'),'@seed.topstar.local'),
        CONCAT('091',FORMAT(s.No,'0000000')),CASE WHEN s.No%3=0 THEN 'Male' ELSE 'Female' END,
        DATEFROMPARTS(1988+(s.No%15),1+(s.No%12),1+(s.No%27)),@PasswordHash,
        CASE WHEN s.No%29=0 THEN 'Locked' ELSE 'Active' END,1,CASE WHEN s.No%4=0 THEN 0 ELSE 1 END,
        DATEADD(HOUR,-(s.No%240),@Now),DATEADD(DAY,-200,@Now),0,DATEADD(DAY,-(30+s.No%700),@Now)
 FROM #KocSeed s WHERE NOT EXISTS(SELECT 1 FROM dbo.Users x WHERE x.Id=s.UserId OR x.Email=CONCAT('seed.koc.',FORMAT(s.No,'0000'),'@seed.topstar.local'));

 INSERT dbo.Users(Id,Email,Phone,Gender,DateOfBirth,PasswordHash,Status,EmailConfirmed,PhoneConfirmed,LastLoginAtUtc,PasswordChangedAtUtc,FailedLoginCount,CreatedAtUtc)
 SELECT s.UserId,CONCAT('seed.brand.',FORMAT(s.No,'0000'),'@seed.topstar.local'),
        CONCAT('092',FORMAT(s.No,'0000000')),NULL,NULL,@PasswordHash,
        CASE WHEN s.No%31=0 THEN 'Locked' ELSE 'Active' END,1,1,
        DATEADD(HOUR,-(s.No%200),@Now),DATEADD(DAY,-200,@Now),0,DATEADD(DAY,-(60+s.No%600),@Now)
 FROM #BrandSeed s WHERE NOT EXISTS(SELECT 1 FROM dbo.Users x WHERE x.Id=s.UserId OR x.Email=CONCAT('seed.brand.',FORMAT(s.No,'0000'),'@seed.topstar.local'));

 INSERT dbo.UserRoles(UserId,RoleId,AssignedAtUtc,CreatedAtUtc)
 SELECT s.UserId,@RoleKoc,@Now,@Now FROM #KocSeed s
 WHERE EXISTS(SELECT 1 FROM dbo.Users u WHERE u.Id=s.UserId)
 AND NOT EXISTS(SELECT 1 FROM dbo.UserRoles ur WHERE ur.UserId=s.UserId AND ur.RoleId=@RoleKoc);
 INSERT dbo.UserRoles(UserId,RoleId,AssignedAtUtc,CreatedAtUtc)
 SELECT s.UserId,@RoleBrand,@Now,@Now FROM #BrandSeed s
 WHERE EXISTS(SELECT 1 FROM dbo.Users u WHERE u.Id=s.UserId)
 AND NOT EXISTS(SELECT 1 FROM dbo.UserRoles ur WHERE ur.UserId=s.UserId AND ur.RoleId=@RoleBrand);

 /* Profiles: distributed statuses support list filters and review queue */
 INSERT dbo.KocProfiles(Id,UserId,DisplayName,AvatarUrl,Gender,Region,StreetAddress,Followers,Score,Status,ReviewNote,ViewCount,IsVisible,CreatedAtUtc)
 SELECT s.ProfileId,s.UserId,
        CONCAT(CASE s.No%8 WHEN 0 THEN N'Nguyễn Minh ' WHEN 1 THEN N'Trần Thảo ' WHEN 2 THEN N'Lê Gia ' WHEN 3 THEN N'Phạm Khánh ' WHEN 4 THEN N'Võ Ngọc ' WHEN 5 THEN N'Đặng Thanh ' WHEN 6 THEN N'Bùi Hoàng ' ELSE N'Đỗ Anh ' END,FORMAT(s.No,'000')),
        CASE s.No%10
          WHEN 0 THEN '/images/koc/trang-nhungdocx-1635528230350.webp'
          WHEN 1 THEN '/images/koc/Phat-hien-baaca0eb0e33dc4f9d45910b8c86623f0144cea0fe0c2093c5-1623311033-423-width1000height1499.webp'
          WHEN 2 THEN '/images/koc/nguoi-mau-thoi-trang5.webp'
          WHEN 3 THEN '/images/koc/nguoi-mau-thoi-trang.webp'
          WHEN 4 THEN '/images/koc/nguoi-mau-nam-viet-nam_461.avif'
          WHEN 5 THEN '/images/koc/Nguoi-Mau-1-1-02.jpg'
          WHEN 6 THEN '/images/koc/HUY05820.jpg'
          WHEN 7 THEN '/images/koc/anp-0693-min-7988.jpg'
          WHEN 8 THEN '/images/koc/1-lee-sung-kyung-339.webp' ELSE '/images/koc/0-min-3843.jpg' END,
        CASE WHEN s.No%3=0 THEN 'Male' ELSE 'Female' END,
        CASE s.No%6 WHEN 0 THEN N'Hồ Chí Minh' WHEN 1 THEN N'Hà Nội' WHEN 2 THEN N'Đà Nẵng' WHEN 3 THEN N'Cần Thơ' WHEN 4 THEN N'Hải Phòng' ELSE N'Bình Dương' END,
        CONCAT(10+s.No,N' Đường Seed, Phường ',1+s.No%20),
        5000+(s.No*791)%850000,CAST(55+(s.No%450)/10.0 AS DECIMAL(5,2)),
        CASE WHEN s.No%20=0 THEN 'PendingReview' WHEN s.No%23=0 THEN 'NeedUpdate' WHEN s.No%37=0 THEN 'Rejected' ELSE 'Approved' END,
        N'Dữ liệu seed phục vụ kiểm thử hiển thị và thao tác Admin.',(s.No*137)%9000,1,DATEADD(DAY,-(30+s.No%700),@Now)
 FROM #KocSeed s WHERE EXISTS(SELECT 1 FROM dbo.Users u WHERE u.Id=s.UserId)
 AND NOT EXISTS(SELECT 1 FROM dbo.KocProfiles p WHERE p.Id=s.ProfileId OR p.UserId=s.UserId);

 INSERT dbo.BrandProfiles(Id,UserId,CompanyName,ContactName,ContactPhone,Industry,Website,Status,ReviewNote,ViewCount,IsVisible,CreatedAtUtc)
 SELECT s.ProfileId,s.UserId,CONCAT(N'Topstar Seed Brand ',FORMAT(s.No,'000')),
        CONCAT(N'Người phụ trách ',FORMAT(s.No,'000')),CONCAT('092',FORMAT(s.No,'0000000')),
        CASE s.No%8 WHEN 0 THEN N'Beauty' WHEN 1 THEN N'FMCG' WHEN 2 THEN N'Thời trang' WHEN 3 THEN N'Công nghệ' WHEN 4 THEN N'Giáo dục' WHEN 5 THEN N'Gia dụng' WHEN 6 THEN N'Du lịch' ELSE N'Tài chính' END,
        CONCAT('https://seed-brand-',s.No,'.example.test'),
        CASE WHEN s.No%18=0 THEN 'PendingReview' WHEN s.No%27=0 THEN 'NeedUpdate' WHEN s.No%41=0 THEN 'Rejected' ELSE 'Approved' END,
        N'Dữ liệu Brand seed phục vụ kiểm thử.',(s.No*89)%7000,1,DATEADD(DAY,-(60+s.No%600),@Now)
 FROM #BrandSeed s WHERE EXISTS(SELECT 1 FROM dbo.Users u WHERE u.Id=s.UserId)
 AND NOT EXISTS(SELECT 1 FROM dbo.BrandProfiles p WHERE p.Id=s.ProfileId OR p.UserId=s.UserId);

 /* KOC 360 detail */
 INSERT dbo.KocBodyProfiles(Id,KocProfileId,HeightCm,WeightKg,BustCm,WaistCm,HipCm,MaritalStatus,HasChildren,ChildrenCount,CreatedAtUtc)
 SELECT s.BodyId,s.ProfileId,155+s.No%31,45+s.No%35,78+s.No%20,58+s.No%22,82+s.No%22,
        CASE WHEN s.No%3=0 THEN 'Married' WHEN s.No%5=0 THEN 'Other' ELSE 'Single' END,
        CASE WHEN s.No%4=0 THEN 1 ELSE 0 END,CASE WHEN s.No%4=0 THEN 1+s.No%3 ELSE 0 END,DATEADD(DAY,-300,@Now)
 FROM #KocSeed s WHERE EXISTS(SELECT 1 FROM dbo.KocProfiles p WHERE p.Id=s.ProfileId)
 AND NOT EXISTS(SELECT 1 FROM dbo.KocBodyProfiles b WHERE b.KocProfileId=s.ProfileId);

 INSERT dbo.KocChildren(Id,KocBodyProfileId,AgeYears,Gender,BirthYear,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-CHILD-',s.No,'-',c.No))),s.BodyId,
        2+((s.No+c.No)%14),CASE WHEN (s.No+c.No)%2=0 THEN 'Female' ELSE 'Male' END,
        YEAR(@Now)-(2+((s.No+c.No)%14)),DATEADD(DAY,-200,@Now)
 FROM #KocSeed s JOIN #N c ON c.No<=CASE WHEN s.No%4=0 THEN 1+s.No%3 ELSE 0 END
 WHERE EXISTS(SELECT 1 FROM dbo.KocBodyProfiles b WHERE b.Id=s.BodyId)
 AND NOT EXISTS(SELECT 1 FROM dbo.KocChildren x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-CHILD-',s.No,'-',c.No))));

 INSERT dbo.KocExperiences(Id,KocProfileId,YearsOfExperience,WorkedBrands,CaseStudyUrl,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-EXP-',s.No))),s.ProfileId,
        CAST((s.No%80)/10.0 AS DECIMAL(4,1)),N'Shopee; Samsung; Grab; Local Brand',CONCAT('https://portfolio.example.test/koc/',s.No),DATEADD(DAY,-200,@Now)
 FROM #KocSeed s WHERE NOT EXISTS(SELECT 1 FROM dbo.KocExperiences x WHERE x.KocProfileId=s.ProfileId);
 INSERT dbo.KocProductionCapabilities(Id,KocProfileId,CanScript,CanShoot,CanEdit,CanLivestream,Devices,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PROD-',s.No))),s.ProfileId,1,1,CASE WHEN s.No%3=0 THEN 0 ELSE 1 END,CASE WHEN s.No%4=0 THEN 1 ELSE 0 END,N'iPhone 15; Sony ZV-E10; Softbox',DATEADD(DAY,-180,@Now)
 FROM #KocSeed s WHERE NOT EXISTS(SELECT 1 FROM dbo.KocProductionCapabilities x WHERE x.KocProfileId=s.ProfileId);
 INSERT dbo.KocJobPreferences(Id,KocProfileId,MinBudgetAmount,PreferredRegions,AvailabilityNote,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PREF-',s.No))),s.ProfileId,
        500000+(s.No%20)*250000,N'Toàn quốc; ưu tiên Hồ Chí Minh và Hà Nội',N'Có thể nhận job trong tuần, cần báo trước 3 ngày.',DATEADD(DAY,-170,@Now)
 FROM #KocSeed s WHERE NOT EXISTS(SELECT 1 FROM dbo.KocJobPreferences x WHERE x.KocProfileId=s.ProfileId);

 INSERT dbo.KocSocialAccounts(Id,KocProfileId,Platform,ChannelUrl,Handle,Followers,AvgViews,AvgEngagementRate,IsPrimary,IsVerified,ViewCount,IsVisible,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-SOCIAL-',s.No,'-',p.No))),s.ProfileId,
        CASE p.No WHEN 1 THEN 'TikTok' WHEN 2 THEN 'Instagram' ELSE 'YouTube' END,
        CONCAT('https://social.example.test/',s.No,'/',p.No),CONCAT('@seedkoc',FORMAT(s.No,'000'),CASE p.No WHEN 1 THEN '_tt' WHEN 2 THEN '_ig' ELSE '_yt' END),
        2000+(s.No*997+p.No*1703)%500000,1000+(s.No*337+p.No*503)%150000,CAST(0.02+((s.No+p.No)%100)/1000.0 AS DECIMAL(7,4)),
        CASE WHEN p.No=1 THEN 1 ELSE 0 END,CASE WHEN (s.No+p.No)%4=0 THEN 1 ELSE 0 END,100+s.No,1,DATEADD(DAY,-120,@Now)
 FROM #KocSeed s JOIN #N p ON p.No<=CASE WHEN s.No%5=0 THEN 3 ELSE 2 END
 WHERE NOT EXISTS(SELECT 1 FROM dbo.KocSocialAccounts x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-SOCIAL-',s.No,'-',p.No))));

 DECLARE @CategoryCount INT=(SELECT COUNT(*) FROM dbo.KocCategories);
 IF @CategoryCount>0
 INSERT dbo.KocProfileCategories(KocProfileId,CategoryId,CreatedAtUtc)
 SELECT s.ProfileId,c.Id,@Now FROM #KocSeed s
 CROSS APPLY(SELECT Id FROM(SELECT Id,ROW_NUMBER() OVER(ORDER BY Code) rn FROM dbo.KocCategories) q WHERE q.rn=1+(s.No%@CategoryCount))c
 WHERE NOT EXISTS(SELECT 1 FROM dbo.KocProfileCategories x WHERE x.KocProfileId=s.ProfileId AND x.CategoryId=c.Id);

 INSERT dbo.KocPortfolios(Id,KocProfileId,Title,BrandName,CampaignName,PostUrl,Platform,Views,Likes,Comments,Shares,ViewCount,IsVisible,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PORTFOLIO-',s.No,'-',p.No))),s.ProfileId,
        CONCAT(N'Video review sản phẩm #',p.No),CASE p.No WHEN 1 THEN N'Shopee' WHEN 2 THEN N'Samsung' ELSE N'Grab' END,
        CONCAT(N'Chiến dịch mẫu ',p.No),CONCAT('https://content.example.test/',s.No,'/',p.No),
        CASE p.No WHEN 1 THEN 'TikTok' WHEN 2 THEN 'Instagram' ELSE 'YouTube' END,
        10000+(s.No*991*p.No)%2000000,1000+(s.No*113*p.No)%150000,50+(s.No*17)%5000,20+(s.No*11)%3000,100+s.No,1,DATEADD(DAY,-p.No*30,@Now)
 FROM #KocSeed s JOIN #N p ON p.No<=3
 WHERE NOT EXISTS(SELECT 1 FROM dbo.KocPortfolios x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PORTFOLIO-',s.No,'-',p.No))));

 INSERT dbo.KocMediaAssets(Id,KocProfileId,MediaType,FileUrl,SortOrder,IsVisible,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-MEDIA-',s.No,'-',p.No))),s.ProfileId,'GalleryImage',
        CASE (s.No+p.No)%10 WHEN 0 THEN '/images/koc/trang-nhungdocx-1635528230350.webp' WHEN 1 THEN '/images/koc/Phat-hien-baaca0eb0e33dc4f9d45910b8c86623f0144cea0fe0c2093c5-1623311033-423-width1000height1499.webp' WHEN 2 THEN '/images/koc/nguoi-mau-thoi-trang5.webp' WHEN 3 THEN '/images/koc/nguoi-mau-thoi-trang.webp' WHEN 4 THEN '/images/koc/nguoi-mau-nam-viet-nam_461.avif' WHEN 5 THEN '/images/koc/Nguoi-Mau-1-1-02.jpg' WHEN 6 THEN '/images/koc/HUY05820.jpg' WHEN 7 THEN '/images/koc/anp-0693-min-7988.jpg' WHEN 8 THEN '/images/koc/1-lee-sung-kyung-339.webp' ELSE '/images/koc/0-min-3843.jpg' END,
        p.No,1,DATEADD(DAY,-p.No,@Now)
 FROM #KocSeed s JOIN #N p ON p.No<=6
 WHERE NOT EXISTS(SELECT 1 FROM dbo.KocMediaAssets x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-MEDIA-',s.No,'-',p.No))));

 /* Campaigns, applications and collaborations */
 INSERT dbo.Campaigns(Id,BrandProfileId,Title,Objective,BudgetAmount,PackageCode,RequiresBooking,RequiresShipment,Status,StartDate,EndDate,ReviewNote,ViewCount,IsVisible,CreatedAtUtc)
 SELECT c.CampaignId,c.BrandProfileId,CONCAT(N'Chiến dịch Seed ',FORMAT(c.No,'000')),
        CASE c.No%4 WHEN 0 THEN N'Tăng nhận diện' WHEN 1 THEN N'Ra mắt sản phẩm' WHEN 2 THEN N'Tăng chuyển đổi' ELSE N'Tạo nội dung UGC' END,
        5000000+(c.No%40)*2500000,
        CASE
          WHEN 5000000+(c.No%40)*2500000 < 20000000 THEN 'ASSIGNED_5_TO_20M'
          WHEN 5000000+(c.No%40)*2500000 < 50000000 THEN 'SELECT_20M'
          WHEN 5000000+(c.No%40)*2500000 < 100000000 THEN 'SELECT_50M'
          ELSE 'SELECT_100M'
        END,c.No%2,c.No%3,
        CASE c.No%6 WHEN 0 THEN 'Completed' WHEN 1 THEN 'InProgress' WHEN 2 THEN 'Published' WHEN 3 THEN 'Approved' WHEN 4 THEN 'Submitted' ELSE 'Draft' END,
        CASE WHEN c.No%6=2 THEN DATEADD(DAY,1+(c.No%45),CAST(@Now AS DATE)) ELSE DATEADD(DAY,-(c.No%180),CAST(@Now AS DATE)) END,
        CASE WHEN c.No%6=2 THEN DATEADD(DAY,31+(c.No%45),CAST(@Now AS DATE)) ELSE DATEADD(DAY,30-(c.No%180),CAST(@Now AS DATE)) END,
        N'Chiến dịch dữ liệu seed.',c.No*31,1,DATEADD(DAY,-(40+c.No%300),@Now)
 FROM #CampaignSeed c WHERE NOT EXISTS(SELECT 1 FROM dbo.Campaigns x WHERE x.Id=c.CampaignId);

 INSERT dbo.CampaignStatusHistories(Id,CampaignId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-CAMPAIGN-HISTORY-',c.No))),
        c.CampaignId,NULL,cp.Status,N'Khởi tạo lịch sử chiến dịch từ bộ dữ liệu seed.',@AdminUser,cp.CreatedAtUtc
 FROM #CampaignSeed c JOIN dbo.Campaigns cp ON cp.Id=c.CampaignId
 WHERE NOT EXISTS(SELECT 1 FROM dbo.CampaignStatusHistories h
                  WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-CAMPAIGN-HISTORY-',c.No))));

 INSERT dbo.Applications(Id,CampaignId,KocProfileId,Status,PitchNote,AppliedAtUtc,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-APP-',k.No,'-',a.No))),
        c.CampaignId,k.ProfileId,
        CASE (k.No+a.No)%5 WHEN 0 THEN 'Applied' WHEN 1 THEN 'Shortlisted' WHEN 2 THEN 'NotSelected' WHEN 3 THEN 'Confirmed' ELSE 'Withdrawn' END,
        N'KOC quan tâm chiến dịch và có kinh nghiệm phù hợp.',DATEADD(DAY,-(a.No*7+k.No%60),@Now),DATEADD(DAY,-(a.No*7+k.No%60),@Now)
 FROM #KocSeed k JOIN #N a ON a.No<=3
 JOIN #CampaignSeed c ON c.No=1+((k.No*7+a.No*31-1)%300)
 WHERE NOT EXISTS(SELECT 1 FROM dbo.Applications x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-APP-',k.No,'-',a.No))));

 INSERT dbo.Participations(Id,CampaignId,KocProfileId,Status,ConfirmedAtUtc,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PART-',k.No,'-',a.No))),
        c.CampaignId,k.ProfileId,CASE (k.No+a.No)%4 WHEN 0 THEN 'Cancelled' WHEN 1 THEN 'Confirmed' ELSE 'Completed' END,
        DATEADD(DAY,-(a.No*10+k.No%90),@Now),DATEADD(DAY,-(a.No*10+k.No%90),@Now)
 FROM #KocSeed k JOIN #N a ON a.No<=2
 JOIN #CampaignSeed c ON c.No=1+((k.No*11+a.No*37-1)%300)
 WHERE NOT EXISTS(SELECT 1 FROM dbo.Participations x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PART-',k.No,'-',a.No))))
 AND NOT EXISTS(SELECT 1 FROM dbo.Participations x WHERE x.CampaignId=c.CampaignId AND x.KocProfileId=k.ProfileId);

 /* Histories shown on KOC 360 */
 INSERT dbo.ApplicationStatusHistories(Id,ApplicationId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-APP-HISTORY-',k.No,'-',a.No))),app.Id,NULL,app.Status,N'Bản ghi seed lịch sử ứng tuyển.',@AdminUser,app.CreatedAtUtc
 FROM #KocSeed k JOIN #N a ON a.No<=3
 JOIN dbo.Applications app ON app.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-APP-',k.No,'-',a.No)))
 WHERE NOT EXISTS(SELECT 1 FROM dbo.ApplicationStatusHistories h WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-APP-HISTORY-',k.No,'-',a.No))));

 INSERT dbo.ParticipationStatusHistories(Id,ParticipationId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PART-HISTORY-',k.No,'-',a.No))),p.Id,NULL,p.Status,N'Bản ghi seed lịch sử hợp tác.',@AdminUser,p.CreatedAtUtc
 FROM #KocSeed k JOIN #N a ON a.No<=2
 JOIN dbo.Participations p ON p.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PART-',k.No,'-',a.No)))
 WHERE NOT EXISTS(SELECT 1 FROM dbo.ParticipationStatusHistories h WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PART-HISTORY-',k.No,'-',a.No))));

 INSERT dbo.Reviews(Id,CampaignId,ReviewerUserId,RevieweeUserId,Rating,Comment,Status,ViewCount,IsVisible,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-REVIEW-',k.No))),c.CampaignId,b.UserId,k.UserId,
        3+k.No%3,CASE k.No%3 WHEN 0 THEN N'KOC phối hợp chuyên nghiệp, nội dung đúng yêu cầu.' WHEN 1 THEN N'Phản hồi nhanh và hoàn thành đúng hạn.' ELSE N'Chất lượng tốt, cần cải thiện báo cáo số liệu.' END,
        CASE WHEN k.No%17=0 THEN 'Hidden' ELSE 'Completed' END,k.No%50,1,DATEADD(DAY,-(k.No%120),@Now)
 FROM #KocSeed k JOIN #CampaignSeed c ON c.No=1+((k.No*11)%300) JOIN #BrandSeed b ON b.ProfileId=c.BrandProfileId
 WHERE NOT EXISTS(SELECT 1 FROM dbo.Reviews x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-REVIEW-',k.No))));

 INSERT dbo.ReviewStatusHistories(Id,ReviewId,FromStatus,ToStatus,Reason,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-REVIEW-HISTORY-',k.No))),
        r.Id,NULL,r.Status,N'Khởi tạo lịch sử đánh giá từ bộ dữ liệu seed.',@AdminUser,r.CreatedAtUtc
 FROM #KocSeed k JOIN dbo.Reviews r
   ON r.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-REVIEW-',k.No)))
 WHERE NOT EXISTS(SELECT 1 FROM dbo.ReviewStatusHistories h
                  WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-REVIEW-HISTORY-',k.No))));

 INSERT dbo.WalletTransactions(Id,KocProfileId,CampaignId,ParticipationId,TransactionType,Amount,Status,Description,HoldReason,CreatedByUserId,AvailableAtUtc,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-WALLET-',k.No,'-',a.No))),k.ProfileId,p.CampaignId,p.Id,'Earning',
        750000+(k.No%20)*250000,CASE (k.No+a.No)%5 WHEN 0 THEN 'Pending' WHEN 1 THEN 'Available' WHEN 2 THEN 'OnHold' WHEN 3 THEN 'PaidOut' ELSE 'Failed' END,
        N'Thu nhập từ chiến dịch seed.',CASE WHEN (k.No+a.No)%5=2 THEN N'Tạm giữ để kiểm tra đối soát.' ELSE NULL END,@AdminUser,DATEADD(DAY,7,p.CreatedAtUtc),p.CreatedAtUtc
 FROM #KocSeed k JOIN #N a ON a.No<=2
 JOIN dbo.Participations p ON p.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-PART-',k.No,'-',a.No)))
 WHERE NOT EXISTS(SELECT 1 FROM dbo.WalletTransactions x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-WALLET-',k.No,'-',a.No))))
 AND NOT EXISTS(SELECT 1 FROM dbo.WalletTransactions x WHERE x.ParticipationId=p.Id);

 INSERT dbo.WalletTransactionStatusHistories(Id,WalletTransactionId,FromStatus,ToStatus,Reason,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-WALLET-HISTORY-',k.No,'-',a.No))),
        w.Id,NULL,w.Status,N'Khởi tạo lịch sử giao dịch từ bộ dữ liệu seed.',@AdminUser,w.CreatedAtUtc
 FROM #KocSeed k JOIN #N a ON a.No<=2
 JOIN dbo.WalletTransactions w
   ON w.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-WALLET-',k.No,'-',a.No)))
 WHERE NOT EXISTS(SELECT 1 FROM dbo.WalletTransactionStatusHistories h
                  WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-WALLET-HISTORY-',k.No,'-',a.No))));

 INSERT dbo.UserViolations(Id,UserId,Category,Severity,PenaltyPoints,Reason,Status,ResolutionNote,CreatedByUserId,RevokedByUserId,RevokedAtUtc,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-VIOLATION-',k.No))),k.UserId,
        CASE k.No%3 WHEN 0 THEN N'Trễ deadline' WHEN 1 THEN N'Nội dung không đúng brief' ELSE N'Không phản hồi' END,
        CASE k.No%3 WHEN 0 THEN 'Low' WHEN 1 THEN 'Medium' ELSE 'High' END,5+(k.No%4)*5,
        N'Vi phạm seed có căn cứ để kiểm thử màn hình và lịch sử.',
        CASE WHEN k.No%2=0 THEN 'Revoked' ELSE 'Active' END,
        CASE WHEN k.No%2=0 THEN N'Đã xác minh và thu hồi vi phạm sau khi KOC bổ sung bằng chứng.' ELSE NULL END,
        @AdminUser,CASE WHEN k.No%2=0 THEN @AdminUser ELSE NULL END,CASE WHEN k.No%2=0 THEN DATEADD(DAY,-2,@Now) ELSE NULL END,DATEADD(DAY,-(10+k.No%90),@Now)
 FROM #KocSeed k WHERE k.No%7=0
 AND NOT EXISTS(SELECT 1 FROM dbo.UserViolations x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-VIOLATION-',k.No))));

 INSERT dbo.AuthEvents(UserId,EmailOrPhone,EventType,EventStatus,FailureReason,UserDeviceId,IpAddress,UserAgent,OccurredAtUtc,CreatedAtUtc)
 SELECT k.UserId,CONCAT('seed.koc.',FORMAT(k.No,'0000'),'@seed.topstar.local'),
        CASE e.No WHEN 1 THEN 'Login' WHEN 2 THEN 'Login' ELSE 'Logout' END,
        CASE WHEN e.No=2 AND k.No%5=0 THEN 'Failed' ELSE 'Success' END,
        CASE WHEN e.No=2 AND k.No%5=0 THEN N'Sai mật khẩu thử nghiệm' ELSE NULL END,NULL,
        CONCAT('10.20.',k.No%250,'.',10+e.No),N'Mozilla/5.0 Topstar Seed Browser',
        DATEADD(HOUR,-(k.No%120+e.No*12),@Now),DATEADD(HOUR,-(k.No%120+e.No*12),@Now)
 FROM #KocSeed k JOIN #N e ON e.No<=3
 WHERE NOT EXISTS(SELECT 1 FROM dbo.AuthEvents x WHERE x.UserId=k.UserId
   AND x.UserAgent=N'Mozilla/5.0 Topstar Seed Browser'
   AND x.IpAddress=CONCAT('10.20.',k.No%250,'.',10+e.No));

 INSERT dbo.AuthEvents(UserId,EmailOrPhone,EventType,EventStatus,FailureReason,UserDeviceId,IpAddress,UserAgent,OccurredAtUtc,CreatedAtUtc)
 SELECT b.UserId,CONCAT('seed.brand.',FORMAT(b.No,'0000'),'@seed.topstar.local'),
        CASE e.No WHEN 1 THEN 'Login' ELSE 'Logout' END,'Success',NULL,NULL,
        CONCAT('10.30.',b.No%250,'.',20+e.No),N'Mozilla/5.0 Topstar Seed Brand Browser',
        DATEADD(HOUR,-(b.No%100+e.No*10),@Now),DATEADD(HOUR,-(b.No%100+e.No*10),@Now)
 FROM #BrandSeed b JOIN #N e ON e.No<=2
 WHERE NOT EXISTS(SELECT 1 FROM dbo.AuthEvents x WHERE x.UserId=b.UserId
   AND x.UserAgent=N'Mozilla/5.0 Topstar Seed Brand Browser'
   AND x.IpAddress=CONCAT('10.30.',b.No%250,'.',20+e.No));

 INSERT dbo.SocialSyncJobs(Id,RequestedByUserId,RequestedByRole,Scope,KocProfileId,CampaignId,Status,ErrorMessage,RequestedAtUtc,StartedAtUtc,FinishedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-SYNC-',k.No,'-',j.No))),@AdminUser,'ADMIN','KocSelf',k.ProfileId,NULL,
        CASE WHEN (k.No+j.No)%8=0 THEN 'Failed' ELSE 'Succeeded' END,
        CASE WHEN (k.No+j.No)%8=0 THEN N'Nền tảng social tạm thời giới hạn request (dữ liệu seed).' ELSE NULL END,
        DATEADD(DAY,-j.No*7,DATEADD(MINUTE,-k.No,@Now)),DATEADD(DAY,-j.No*7,DATEADD(MINUTE,-k.No+1,@Now)),DATEADD(DAY,-j.No*7,DATEADD(MINUTE,-k.No+3,@Now))
 FROM #KocSeed k JOIN #N j ON j.No<=3
 WHERE NOT EXISTS(SELECT 1 FROM dbo.SocialSyncJobs x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-SYNC-',k.No,'-',j.No))));

 INSERT dbo.SocialProfileMetricSnapshots(Id,KocProfileId,Platform,SnapshotAtUtc,Followers,AvgViews,AvgEngagementRate,Reach,Impressions,Likes,Comments,Shares,PostsCount,DataSource,ConfidenceScore,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-METRIC-',k.No,'-',m.No))),k.ProfileId,'TikTok',
        DATEADD(DAY,-m.No*7,@Now),2000+(k.No*997)%500000+m.No*500,1000+(k.No*337)%150000,
        CAST(.03+(k.No%70)/1000.0 AS DECIMAL(7,4)),5000+k.No*30,8000+k.No*40,1000+k.No*3,50+k.No,20+k.No%100,20+k.No%30,'Manual',90,DATEADD(DAY,-m.No*7,@Now)
 FROM #KocSeed k JOIN #N m ON m.No<=4
 WHERE NOT EXISTS(SELECT 1 FROM dbo.SocialProfileMetricSnapshots x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-METRIC-',k.No,'-',m.No))));

 /* Explicit status/audit journals; profile insert triggers also create initial snapshots. */
 INSERT dbo.UserAccountStatusHistories(Id,UserId,FromStatus,ToStatus,ProfileType,FromProfileStatus,ToProfileStatus,Reason,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-ACCOUNT-HISTORY-',k.No))),k.UserId,'Active',
        'Locked','KOC','Approved','Approved',
        N'Sự kiện tài khoản seed để kiểm tra timeline.',@AdminUser,DATEADD(DAY,-(k.No%60),@Now)
 FROM #KocSeed k WHERE k.No%29=0
 AND NOT EXISTS(SELECT 1 FROM dbo.UserAccountStatusHistories x WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-ACCOUNT-HISTORY-',k.No))));

 INSERT dbo.UserAccountStatusHistories(Id,UserId,FromStatus,ToStatus,ProfileType,FromProfileStatus,ToProfileStatus,Reason,ActorUserId,CreatedAtUtc)
 SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-BRAND-ACCOUNT-HISTORY-',b.No))),
        b.UserId,'Active','Locked','Brand','Approved','Approved',
        N'Sự kiện tài khoản Brand seed để kiểm tra timeline.',@AdminUser,DATEADD(DAY,-(b.No%60),@Now)
 FROM #BrandSeed b WHERE b.No%31=0
 AND NOT EXISTS(SELECT 1 FROM dbo.UserAccountStatusHistories x
                WHERE x.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SEED-BRAND-ACCOUNT-HISTORY-',b.No))));

 INSERT dbo.AuditLogs(ActorUserId,EntityName,EntityId,ActionName,PayloadJson,CreatedAtUtc)
 SELECT @AdminUser,'KOCProfile',CONVERT(NVARCHAR(36),k.ProfileId),'SeedCreated',
        CONCAT(N'{"source":"seed84","number":',k.No,N'}'),DATEADD(DAY,-(k.No%90),@Now)
 FROM #KocSeed k WHERE NOT EXISTS(SELECT 1 FROM dbo.AuditLogs x WHERE x.EntityName='KOCProfile' AND x.EntityId=CONVERT(NVARCHAR(36),k.ProfileId) AND x.ActionName='SeedCreated');

 INSERT dbo.AuditLogs(ActorUserId,EntityName,EntityId,ActionName,PayloadJson,CreatedAtUtc)
 SELECT @AdminUser,'BrandProfile',CONVERT(NVARCHAR(36),b.ProfileId),'SeedCreated',
        CONCAT(N'{"source":"seed84","number":',b.No,N'}'),DATEADD(DAY,-(b.No%90),@Now)
 FROM #BrandSeed b WHERE NOT EXISTS(SELECT 1 FROM dbo.AuditLogs x
   WHERE x.EntityName='BrandProfile' AND x.EntityId=CONVERT(NVARCHAR(36),b.ProfileId) AND x.ActionName='SeedCreated');

 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

/* Verification summary */
SELECT N'KOC seed' AS Dataset,COUNT(*) AS [RowCount] FROM dbo.Users WHERE Email LIKE 'seed.koc.%@seed.topstar.local'
UNION ALL SELECT N'Brand seed',COUNT(*) FROM dbo.Users WHERE Email LIKE 'seed.brand.%@seed.topstar.local'
UNION ALL SELECT N'Campaign seed',COUNT(*) FROM dbo.Campaigns WHERE Title LIKE N'Chiến dịch Seed %'
UNION ALL SELECT N'Applications seed',COUNT(*) FROM dbo.Applications WHERE PitchNote=N'KOC quan tâm chiến dịch và có kinh nghiệm phù hợp.'
UNION ALL SELECT N'Participations seed',COUNT(*) FROM dbo.ParticipationStatusHistories WHERE Note=N'Bản ghi seed lịch sử hợp tác.'
UNION ALL SELECT N'Wallet seed',COUNT(*) FROM dbo.WalletTransactions WHERE Description=N'Thu nhập từ chiến dịch seed.'
UNION ALL SELECT N'Violations seed',COUNT(*) FROM dbo.UserViolations WHERE Reason=N'Vi phạm seed có căn cứ để kiểm thử màn hình và lịch sử.'
UNION ALL SELECT N'Social sync seed',COUNT(*) FROM dbo.SocialSyncJobs WHERE ErrorMessage LIKE N'%dữ liệu seed%';

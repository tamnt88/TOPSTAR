/*
Topstar v1 seed data for SQL Server 2022
Run after: 00_create_topstar_v1.sql
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

DECLARE @RoleAdmin UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleBrand UNIQUEIDENTIFIER = NEWID();
DECLARE @RoleKoc UNIQUEIDENTIFIER = NEWID();

INSERT INTO [dbo].[Roles] ([Id],[Code],[Name])
VALUES
(@RoleAdmin, 'ADMIN', 'Administrator'),
(@RoleBrand, 'BRAND', 'Brand User'),
(@RoleKoc, 'KOC', 'Key Opinion Consumer');

DECLARE @AdminUser UNIQUEIDENTIFIER = NEWID();
DECLARE @BrandUser UNIQUEIDENTIFIER = NEWID();
DECLARE @KocUser UNIQUEIDENTIFIER = NEWID();

INSERT INTO [dbo].[Users] ([Id],[Email],[Phone],[PasswordHash],[Status])
VALUES
(@AdminUser, 'admin@topstar.local', '0900000001', 'TEMP_HASH_CHANGE_ME', 'Active'),
(@BrandUser, 'brand@topstar.local', '0900000002', 'TEMP_HASH_CHANGE_ME', 'Active'),
(@KocUser, 'koc@topstar.local', '0900000003', 'TEMP_HASH_CHANGE_ME', 'Active');

INSERT INTO [dbo].[UserRoles] ([UserId],[RoleId])
VALUES
(@AdminUser,@RoleAdmin),
(@BrandUser,@RoleBrand),
(@KocUser,@RoleKoc);

DECLARE @BrandProfile UNIQUEIDENTIFIER = NEWID();
DECLARE @KocProfile UNIQUEIDENTIFIER = NEWID();
DECLARE @KocBodyProfile UNIQUEIDENTIFIER = NEWID();
DECLARE @CategoryBeauty UNIQUEIDENTIFIER = NEWID();
DECLARE @CategoryLifestyle UNIQUEIDENTIFIER = NEWID();
DECLARE @Campaign UNIQUEIDENTIFIER = NEWID();
DECLARE @Deliverable UNIQUEIDENTIFIER = NEWID();
DECLARE @Application UNIQUEIDENTIFIER = NEWID();
DECLARE @Shortlist UNIQUEIDENTIFIER = NEWID();
DECLARE @Participation UNIQUEIDENTIFIER = NEWID();
DECLARE @Submission UNIQUEIDENTIFIER = NEWID();
DECLARE @SubmissionPost UNIQUEIDENTIFIER = NEWID();

INSERT INTO [dbo].[BrandProfiles]
([Id],[UserId],[CompanyName],[ContactName],[ContactPhone],[Industry],[Website],[Status])
VALUES
(@BrandProfile,@BrandUser,N'Topstar Brand Demo',N'Ms. Brand Owner','0900000002',N'Beauty','https://example.com','Approved');

INSERT INTO [dbo].[KocProfiles]
([Id],[UserId],[DisplayName],[Gender],[Region],[Followers],[Score],[Status])
VALUES
(@KocProfile,@KocUser,N'KOC Demo 01',N'Female',N'HCM',25000,88.50,'Approved');

INSERT INTO [dbo].[KocBodyProfiles]
([Id],[KocProfileId],[HeightCm],[WeightKg],[BustCm],[WaistCm],[HipCm],[MaritalStatus],[HasChildren],[ChildrenCount])
VALUES
(@KocBodyProfile,@KocProfile,165,50,84,62,89,'Single',0,0);

INSERT INTO [dbo].[KocSocialAccounts]
([KocProfileId],[Platform],[ChannelUrl],[Handle],[Followers],[AvgViews],[AvgEngagementRate],[IsPrimary],[IsVerified],[ViewCount],[IsVisible])
VALUES
(@KocProfile,'TikTok','https://www.tiktok.com/@kocdemo','@kocdemo',25000,12000,0.0850,1,0,1200,1),
(@KocProfile,'Instagram','https://www.instagram.com/kocdemo','@kocdemo',18000,7000,0.0620,0,0,700,1);

INSERT INTO [dbo].[KocCategories]
([Id],[Code],[Name])
VALUES
(@CategoryBeauty,'beauty',N'Beauty'),
(@CategoryLifestyle,'lifestyle',N'Lifestyle');

INSERT INTO [dbo].[KocProfileCategories]
([KocProfileId],[CategoryId])
VALUES
(@KocProfile,@CategoryBeauty),
(@KocProfile,@CategoryLifestyle);

INSERT INTO [dbo].[KocExperiences]
([KocProfileId],[YearsOfExperience],[WorkedBrands],[CaseStudyUrl])
VALUES
(@KocProfile,2.5,N'The Face Shop; Maybelline; Innisfree','https://portfolio.example.com/kocdemo');

INSERT INTO [dbo].[KocProductionCapabilities]
([KocProfileId],[CanScript],[CanShoot],[CanEdit],[CanLivestream],[Devices])
VALUES
(@KocProfile,1,1,1,1,N'iPhone 15 Pro; Sony ZV-E10; Softbox');

INSERT INTO [dbo].[KocJobPreferences]
([KocProfileId],[MinBudgetAmount],[PreferredRegions],[AvailabilityNote])
VALUES
(@KocProfile,1500000,N'HCM,Binh Duong',N'Available on weekdays after 18:00 and weekends');

INSERT INTO [dbo].[KocPortfolios]
([KocProfileId],[Title],[BrandName],[CampaignName],[PostUrl],[Platform],[Views],[Likes],[Comments],[Shares],[ViewCount],[IsVisible])
VALUES
(@KocProfile,N'Review serum launch',N'Innisfree',N'Green Tea Skincare','https://www.tiktok.com/@kocdemo/video/111','TikTok',55000,4200,390,210,55000,1),
(@KocProfile,N'Flash sale livestream',N'Shopee',N'Flash Sale 11.11','https://www.facebook.com/reel/222','Facebook',22000,1800,140,90,22000,1);

INSERT INTO [dbo].[Campaigns]
([Id],[BrandProfileId],[Title],[Objective],[BudgetAmount],[PackageCode],[RequiresBooking],[RequiresShipment],[Status],[StartDate],[EndDate])
VALUES
(@Campaign,@BrandProfile,N'Launch Serum Q2',N'Awareness',20000000,'PKG_20M',1,1,'Published','2026-04-10','2026-05-10');

INSERT INTO [dbo].[CampaignDeliverables]
([Id],[CampaignId],[DeliverableType],[Quantity],[DueDateUtc])
VALUES
(@Deliverable,@Campaign,'Video',1,DATEADD(DAY,14,SYSUTCDATETIME()));

INSERT INTO [dbo].[Applications]
([Id],[CampaignId],[KocProfileId],[Status],[PitchNote])
VALUES
(@Application,@Campaign,@KocProfile,'Applied',N'Sample pitch note');

INSERT INTO [dbo].[Shortlists]
([Id],[CampaignId],[SentToBrandAtUtc],[CreatedByUserId])
VALUES
(@Shortlist,@Campaign,SYSUTCDATETIME(),@AdminUser);

INSERT INTO [dbo].[ShortlistItems]
([ShortlistId],[ApplicationId],[SelectionSource],[Note])
VALUES
(@Shortlist,@Application,'Top3',N'Auto top score');

UPDATE [dbo].[Applications]
SET [Status] = 'Confirmed', [UpdatedAtUtc] = SYSUTCDATETIME()
WHERE [Id] = @Application;

INSERT INTO [dbo].[Participations]
([Id],[CampaignId],[KocProfileId],[Status])
VALUES
(@Participation,@Campaign,@KocProfile,'Confirmed');

INSERT INTO [dbo].[Schedules]
([ParticipationId],[Status],[StartsAtUtc],[EndsAtUtc])
VALUES
(@Participation,'Proposed',DATEADD(DAY,3,SYSUTCDATETIME()),DATEADD(DAY,3,DATEADD(HOUR,1,SYSUTCDATETIME())));

INSERT INTO [dbo].[Shipments]
([ParticipationId],[TrackingCode],[Carrier],[Status])
VALUES
(@Participation,'TS-DEMO-0001','GHN','Created');

INSERT INTO [dbo].[Submissions]
([Id],[ParticipationId],[DeliverableId],[Status],[SubmissionUrl])
VALUES
(@Submission,@Participation,@Deliverable,'Submitted','https://tiktok.com/@demo/video/123456789');

INSERT INTO [dbo].[SubmissionPosts]
([Id],[SubmissionId],[Platform],[PostUrl],[PostExternalId],[PostedAtUtc],[Status],[ViewCount],[IsVisible])
VALUES
(@SubmissionPost,@Submission,'TikTok','https://tiktok.com/@demo/video/123456789','123456789',DATEADD(DAY,-1,SYSUTCDATETIME()),'Active',12000,1);

INSERT INTO [dbo].[PostMetricSnapshots]
([SubmissionPostId],[CapturedAtUtc],[Views],[Likes],[Comments],[Shares],[Saves],[Impressions],[Reach],[Clicks],[EngagementRate],[DataSource])
VALUES
(@SubmissionPost,DATEADD(HOUR,-24,SYSUTCDATETIME()),8200,710,85,42,26,9100,7800,190,0.1021,'Manual'),
(@SubmissionPost,SYSUTCDATETIME(),12000,980,130,66,39,13800,11200,260,0.1012,'Manual');

INSERT INTO [dbo].[CampaignKocDailyMetrics]
([CampaignId],[KocProfileId],[MetricDate],[Impressions],[Views],[Likes],[Comments],[Shares],[Saves],[Clicks],[Conversions],[SpendAmount],[RevenueAmount])
VALUES
(@Campaign,@KocProfile,CAST(SYSUTCDATETIME() AS DATE),13800,12000,980,130,66,39,260,14,4000000,9800000);

INSERT INTO [dbo].[Notifications]
([UserId],[Type],[Title],[Body])
VALUES
(@BrandUser,'Campaign',N'Shortlist sent',N'Admin has sent shortlist for your campaign.'),
(@KocUser,'Application',N'You are confirmed',N'You are confirmed for campaign Launch Serum Q2.');

COMMIT TRANSACTION;
GO

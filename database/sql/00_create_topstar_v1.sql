/*
Topstar Database Schema v1 (SQL Server 2022)
Date: 2026-04-03
Notes:
- Use default schema dbo (no custom schema prefixes).
- All tables include CreatedAtUtc + UpdatedAtUtc.
- ViewCount/IsVisible are only added to content-facing tables.
*/

SET XACT_ABORT ON;
GO

IF DB_ID(N'TopstarDb') IS NULL
BEGIN
    CREATE DATABASE [TopstarDb];
END
GO

USE [TopstarDb];
GO

BEGIN TRANSACTION;
GO

-- Users: account table for Admin/Brand/KOC
CREATE TABLE [dbo].[Users] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [Email] NVARCHAR(320) NOT NULL,
  [Phone] NVARCHAR(20) NULL,
  [Gender] NVARCHAR(20) NULL CHECK ([Gender] IN ('Male','Female','Other')),
  [PasswordHash] NVARCHAR(512) NOT NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Active' CHECK ([Status] IN ('Active','Locked','Disabled')),
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Users] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_Users_Email] UNIQUE ([Email])
);
GO

-- Roles
CREATE TABLE [dbo].[Roles] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [Code] NVARCHAR(30) NOT NULL,
  [Name] NVARCHAR(100) NOT NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Roles] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_Roles_Code] UNIQUE ([Code])
);
GO

-- UserRoles
CREATE TABLE [dbo].[UserRoles] (
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [RoleId] UNIQUEIDENTIFIER NOT NULL,
  [AssignedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId],[RoleId]),
  CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [dbo].[Roles]([Id])
);
GO

-- BrandProfiles (content-facing: IsVisible + ViewCount)
CREATE TABLE [dbo].[BrandProfiles] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [CompanyName] NVARCHAR(200) NOT NULL,
  [ContactName] NVARCHAR(150) NOT NULL,
  [ContactPhone] NVARCHAR(20) NOT NULL,
  [Industry] NVARCHAR(100) NULL,
  [Website] NVARCHAR(500) NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Draft' CHECK ([Status] IN ('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')),
  [ReviewNote] NVARCHAR(1000) NULL,
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_BrandProfiles] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_BrandProfiles_UserId] UNIQUE ([UserId]),
  CONSTRAINT [FK_BrandProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- KocProfiles (content-facing: IsVisible + ViewCount)
CREATE TABLE [dbo].[KocProfiles] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [DisplayName] NVARCHAR(150) NOT NULL,
  [Gender] NVARCHAR(20) NULL,
  [Region] NVARCHAR(100) NULL,
  [Followers] INT NOT NULL DEFAULT 0,
  [Score] DECIMAL(5,2) NOT NULL DEFAULT 0,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Draft' CHECK ([Status] IN ('Draft','PendingReview','NeedUpdate','Approved','Rejected','Locked')),
  [ReviewNote] NVARCHAR(1000) NULL,
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocProfiles] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_KocProfiles_UserId] UNIQUE ([UserId]),
  CONSTRAINT [FK_KocProfiles_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- KocBodyProfiles: physical/family info from KOC onboarding step
CREATE TABLE [dbo].[KocBodyProfiles] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [HeightCm] DECIMAL(5,2) NULL,
  [WeightKg] DECIMAL(5,2) NULL,
  [BustCm] DECIMAL(5,2) NULL,
  [WaistCm] DECIMAL(5,2) NULL,
  [HipCm] DECIMAL(5,2) NULL,
  [MaritalStatus] NVARCHAR(30) NULL CHECK ([MaritalStatus] IN ('Single','Married','Other')),
  [HasChildren] BIT NOT NULL DEFAULT 0,
  [ChildrenCount] INT NOT NULL DEFAULT 0,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocBodyProfiles] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_KocBodyProfiles_KocProfileId] UNIQUE ([KocProfileId]),
  CONSTRAINT [FK_KocBodyProfiles_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- KocChildren: child details when HasChildren = 1
CREATE TABLE [dbo].[KocChildren] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [KocBodyProfileId] UNIQUEIDENTIFIER NOT NULL,
  [AgeYears] INT NULL,
  [Gender] NVARCHAR(20) NULL CHECK ([Gender] IN ('Male','Female','Other')),
  [BirthYear] INT NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocChildren] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_KocChildren_KocBodyProfiles] FOREIGN KEY ([KocBodyProfileId]) REFERENCES [dbo].[KocBodyProfiles]([Id])
);
GO

-- KocSocialAccounts: linked social channels and baseline metrics
CREATE TABLE [dbo].[KocSocialAccounts] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [Platform] NVARCHAR(30) NOT NULL CHECK ([Platform] IN ('TikTok','Instagram','Facebook','YouTube','Other')),
  [ChannelUrl] NVARCHAR(1200) NOT NULL,
  [Handle] NVARCHAR(200) NULL,
  [Followers] INT NOT NULL DEFAULT 0,
  [AvgViews] INT NULL,
  [AvgEngagementRate] DECIMAL(7,4) NULL,
  [IsPrimary] BIT NOT NULL DEFAULT 0,
  [IsVerified] BIT NOT NULL DEFAULT 0,
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocSocialAccounts] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_KocSocialAccounts_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- KocCategories: creator niches (beauty, food, lifestyle...)
CREATE TABLE [dbo].[KocCategories] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [Code] NVARCHAR(50) NOT NULL,
  [Name] NVARCHAR(100) NOT NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocCategories] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_KocCategories_Code] UNIQUE ([Code])
);
GO

-- KocProfileCategories: many-to-many KOC <-> category
CREATE TABLE [dbo].[KocProfileCategories] (
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [CategoryId] UNIQUEIDENTIFIER NOT NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocProfileCategories] PRIMARY KEY ([KocProfileId],[CategoryId]),
  CONSTRAINT [FK_KocProfileCategories_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id]),
  CONSTRAINT [FK_KocProfileCategories_KocCategories] FOREIGN KEY ([CategoryId]) REFERENCES [dbo].[KocCategories]([Id])
);
GO

-- KocExperiences: prior collaboration summary
CREATE TABLE [dbo].[KocExperiences] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [YearsOfExperience] DECIMAL(4,1) NULL,
  [WorkedBrands] NVARCHAR(2000) NULL,
  [CaseStudyUrl] NVARCHAR(1200) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocExperiences] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_KocExperiences_KocProfileId] UNIQUE ([KocProfileId]),
  CONSTRAINT [FK_KocExperiences_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- KocProductionCapabilities: shooting/editing/livestream capabilities
CREATE TABLE [dbo].[KocProductionCapabilities] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [CanScript] BIT NOT NULL DEFAULT 0,
  [CanShoot] BIT NOT NULL DEFAULT 0,
  [CanEdit] BIT NOT NULL DEFAULT 0,
  [CanLivestream] BIT NOT NULL DEFAULT 0,
  [Devices] NVARCHAR(1000) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocProductionCapabilities] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_KocProductionCapabilities_KocProfileId] UNIQUE ([KocProfileId]),
  CONSTRAINT [FK_KocProductionCapabilities_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- KocJobPreferences: budget floor, preferred regions and availability notes
CREATE TABLE [dbo].[KocJobPreferences] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [MinBudgetAmount] DECIMAL(18,2) NULL,
  [PreferredRegions] NVARCHAR(500) NULL,
  [AvailabilityNote] NVARCHAR(1000) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocJobPreferences] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_KocJobPreferences_KocProfileId] UNIQUE ([KocProfileId]),
  CONSTRAINT [FK_KocJobPreferences_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- KocPortfolios: portfolio/campaign highlights shown on KOC profile
CREATE TABLE [dbo].[KocPortfolios] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [Title] NVARCHAR(200) NOT NULL,
  [BrandName] NVARCHAR(200) NULL,
  [CampaignName] NVARCHAR(200) NULL,
  [PostUrl] NVARCHAR(1200) NULL,
  [Platform] NVARCHAR(30) NULL CHECK ([Platform] IN ('TikTok','Instagram','Facebook','YouTube','Other')),
  [Views] BIGINT NOT NULL DEFAULT 0,
  [Likes] BIGINT NOT NULL DEFAULT 0,
  [Comments] BIGINT NOT NULL DEFAULT 0,
  [Shares] BIGINT NOT NULL DEFAULT 0,
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_KocPortfolios] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_KocPortfolios_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- Campaigns (content-facing: IsVisible + ViewCount)
CREATE TABLE [dbo].[Campaigns] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [BrandProfileId] UNIQUEIDENTIFIER NOT NULL,
  [Title] NVARCHAR(200) NOT NULL,
  [Objective] NVARCHAR(100) NULL,
  [BudgetAmount] DECIMAL(18,2) NOT NULL,
  [PackageCode] NVARCHAR(30) NOT NULL,
  [RequiresBooking] BIT NOT NULL DEFAULT 0,
  [RequiresShipment] BIT NOT NULL DEFAULT 0,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Draft' CHECK ([Status] IN ('Draft','Submitted','NeedEdit','Approved','Published','InProgress','Completed','Cancelled')),
  [StartDate] DATE NULL,
  [EndDate] DATE NULL,
  [ReviewNote] NVARCHAR(1000) NULL,
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Campaigns] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_Campaigns_BrandProfiles] FOREIGN KEY ([BrandProfileId]) REFERENCES [dbo].[BrandProfiles]([Id])
);
GO

-- CampaignDeliverables
CREATE TABLE [dbo].[CampaignDeliverables] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [CampaignId] UNIQUEIDENTIFIER NOT NULL,
  [DeliverableType] NVARCHAR(50) NOT NULL,
  [Quantity] INT NOT NULL DEFAULT 1,
  [DueDateUtc] DATETIME2(0) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_CampaignDeliverables] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_CampaignDeliverables_Campaigns] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id])
);
GO

-- Applications
CREATE TABLE [dbo].[Applications] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [CampaignId] UNIQUEIDENTIFIER NOT NULL,
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Applied' CHECK ([Status] IN ('Applied','Shortlisted','NotSelected','Confirmed','Withdrawn','CancelledByAdmin')),
  [PitchNote] NVARCHAR(1000) NULL,
  [AppliedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Applications] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_Applications_Campaign_Koc] UNIQUE ([CampaignId],[KocProfileId]),
  CONSTRAINT [FK_Applications_Campaigns] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id]),
  CONSTRAINT [FK_Applications_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- Shortlists
CREATE TABLE [dbo].[Shortlists] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [CampaignId] UNIQUEIDENTIFIER NOT NULL,
  [SentToBrandAtUtc] DATETIME2(0) NULL,
  [CreatedByUserId] UNIQUEIDENTIFIER NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Shortlists] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_Shortlists_CampaignId] UNIQUE ([CampaignId]),
  CONSTRAINT [FK_Shortlists_Campaigns] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id]),
  CONSTRAINT [FK_Shortlists_Users] FOREIGN KEY ([CreatedByUserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- ShortlistItems
CREATE TABLE [dbo].[ShortlistItems] (
  [ShortlistId] UNIQUEIDENTIFIER NOT NULL,
  [ApplicationId] UNIQUEIDENTIFIER NOT NULL,
  [SelectionSource] NVARCHAR(20) NOT NULL CHECK ([SelectionSource] IN ('Top3','Random','Manual')),
  [Note] NVARCHAR(500) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_ShortlistItems] PRIMARY KEY ([ShortlistId],[ApplicationId]),
  CONSTRAINT [FK_ShortlistItems_Shortlists] FOREIGN KEY ([ShortlistId]) REFERENCES [dbo].[Shortlists]([Id]),
  CONSTRAINT [FK_ShortlistItems_Applications] FOREIGN KEY ([ApplicationId]) REFERENCES [dbo].[Applications]([Id])
);
GO

-- Participations
CREATE TABLE [dbo].[Participations] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [CampaignId] UNIQUEIDENTIFIER NOT NULL,
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Confirmed' CHECK ([Status] IN ('Confirmed','Cancelled')),
  [ConfirmedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Participations] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_Participations_Campaign_Koc] UNIQUE ([CampaignId],[KocProfileId]),
  CONSTRAINT [FK_Participations_Campaigns] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id]),
  CONSTRAINT [FK_Participations_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- Schedules
CREATE TABLE [dbo].[Schedules] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [ParticipationId] UNIQUEIDENTIFIER NOT NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Proposed' CHECK ([Status] IN ('Proposed','Confirmed','Rescheduled','Cancelled')),
  [StartsAtUtc] DATETIME2(0) NULL,
  [EndsAtUtc] DATETIME2(0) NULL,
  [RescheduleCount] INT NOT NULL DEFAULT 0,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Schedules] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_Schedules_Participations] FOREIGN KEY ([ParticipationId]) REFERENCES [dbo].[Participations]([Id])
);
GO

-- Shipments
CREATE TABLE [dbo].[Shipments] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [ParticipationId] UNIQUEIDENTIFIER NOT NULL,
  [TrackingCode] NVARCHAR(100) NULL,
  [Carrier] NVARCHAR(100) NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'NotCreated' CHECK ([Status] IN ('NotCreated','Created','InTransit','Delivered','Issue','Returned')),
  [IssueNote] NVARCHAR(1000) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Shipments] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_Shipments_Participations] FOREIGN KEY ([ParticipationId]) REFERENCES [dbo].[Participations]([Id])
);
GO

-- Submissions (content-facing: IsVisible + ViewCount)
CREATE TABLE [dbo].[Submissions] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [ParticipationId] UNIQUEIDENTIFIER NOT NULL,
  [DeliverableId] UNIQUEIDENTIFIER NOT NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'NotSubmitted' CHECK ([Status] IN ('NotSubmitted','Submitted','NeedRevision','Approved','Rejected')),
  [SubmissionUrl] NVARCHAR(1000) NULL,
  [ReviewComment] NVARCHAR(1000) NULL,
  [RevisionCount] INT NOT NULL DEFAULT 0,
  [SubmittedAtUtc] DATETIME2(0) NULL,
  [ReviewedAtUtc] DATETIME2(0) NULL,
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Submissions] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_Submissions_Participations] FOREIGN KEY ([ParticipationId]) REFERENCES [dbo].[Participations]([Id]),
  CONSTRAINT [FK_Submissions_Deliverables] FOREIGN KEY ([DeliverableId]) REFERENCES [dbo].[CampaignDeliverables]([Id])
);
GO

-- SubmissionPosts: each submitted social link from KOC
CREATE TABLE [dbo].[SubmissionPosts] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [SubmissionId] UNIQUEIDENTIFIER NOT NULL,
  [Platform] NVARCHAR(30) NOT NULL CHECK ([Platform] IN ('TikTok','Instagram','Facebook','YouTube','Other')),
  [PostUrl] NVARCHAR(1200) NOT NULL,
  [PostExternalId] NVARCHAR(200) NULL,
  [PostedAtUtc] DATETIME2(0) NULL,
  [Status] NVARCHAR(30) NOT NULL DEFAULT 'Active' CHECK ([Status] IN ('Active','Removed','Hidden','InvalidLink')),
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_SubmissionPosts] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_SubmissionPosts_Submissions] FOREIGN KEY ([SubmissionId]) REFERENCES [dbo].[Submissions]([Id])
);
GO

-- PostMetricSnapshots: time-series metrics captured for each post link
CREATE TABLE [dbo].[PostMetricSnapshots] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [SubmissionPostId] UNIQUEIDENTIFIER NOT NULL,
  [CapturedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [Views] BIGINT NOT NULL DEFAULT 0,
  [Likes] BIGINT NOT NULL DEFAULT 0,
  [Comments] BIGINT NOT NULL DEFAULT 0,
  [Shares] BIGINT NOT NULL DEFAULT 0,
  [Saves] BIGINT NOT NULL DEFAULT 0,
  [Impressions] BIGINT NOT NULL DEFAULT 0,
  [Reach] BIGINT NOT NULL DEFAULT 0,
  [Clicks] BIGINT NOT NULL DEFAULT 0,
  [EngagementRate] DECIMAL(7,4) NULL,
  [DataSource] NVARCHAR(30) NOT NULL DEFAULT 'Manual' CHECK ([DataSource] IN ('Manual','Api','Crawler')),
  [RawJson] NVARCHAR(MAX) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_PostMetricSnapshots] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_PostMetricSnapshots_SubmissionPosts] FOREIGN KEY ([SubmissionPostId]) REFERENCES [dbo].[SubmissionPosts]([Id])
);
GO

-- CampaignKocDailyMetrics: aggregated daily KPI by campaign and KOC for dashboard/report
CREATE TABLE [dbo].[CampaignKocDailyMetrics] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [CampaignId] UNIQUEIDENTIFIER NOT NULL,
  [KocProfileId] UNIQUEIDENTIFIER NOT NULL,
  [MetricDate] DATE NOT NULL,
  [Impressions] BIGINT NOT NULL DEFAULT 0,
  [Views] BIGINT NOT NULL DEFAULT 0,
  [Likes] BIGINT NOT NULL DEFAULT 0,
  [Comments] BIGINT NOT NULL DEFAULT 0,
  [Shares] BIGINT NOT NULL DEFAULT 0,
  [Saves] BIGINT NOT NULL DEFAULT 0,
  [Clicks] BIGINT NOT NULL DEFAULT 0,
  [Conversions] BIGINT NOT NULL DEFAULT 0,
  [SpendAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
  [RevenueAmount] DECIMAL(18,2) NOT NULL DEFAULT 0,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_CampaignKocDailyMetrics] PRIMARY KEY ([Id]),
  CONSTRAINT [UQ_CampaignKocDailyMetrics_Campaign_Koc_Date] UNIQUE ([CampaignId],[KocProfileId],[MetricDate]),
  CONSTRAINT [FK_CampaignKocDailyMetrics_Campaigns] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id]),
  CONSTRAINT [FK_CampaignKocDailyMetrics_KocProfiles] FOREIGN KEY ([KocProfileId]) REFERENCES [dbo].[KocProfiles]([Id])
);
GO

-- Reviews (content-facing: IsVisible + ViewCount)
CREATE TABLE [dbo].[Reviews] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [CampaignId] UNIQUEIDENTIFIER NOT NULL,
  [ReviewerUserId] UNIQUEIDENTIFIER NOT NULL,
  [RevieweeUserId] UNIQUEIDENTIFIER NOT NULL,
  [Rating] TINYINT NOT NULL CHECK ([Rating] BETWEEN 1 AND 5),
  [Comment] NVARCHAR(1000) NULL,
  [Status] NVARCHAR(20) NOT NULL DEFAULT 'Completed' CHECK ([Status] IN ('Pending','Completed','Hidden')),
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Reviews] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_Reviews_Campaigns] FOREIGN KEY ([CampaignId]) REFERENCES [dbo].[Campaigns]([Id]),
  CONSTRAINT [FK_Reviews_Reviewer] FOREIGN KEY ([ReviewerUserId]) REFERENCES [dbo].[Users]([Id]),
  CONSTRAINT [FK_Reviews_Reviewee] FOREIGN KEY ([RevieweeUserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- AuditLogs
CREATE TABLE [dbo].[AuditLogs] (
  [Id] BIGINT IDENTITY(1,1) NOT NULL,
  [ActorUserId] UNIQUEIDENTIFIER NULL,
  [EntityName] NVARCHAR(100) NOT NULL,
  [EntityId] NVARCHAR(100) NOT NULL,
  [ActionName] NVARCHAR(100) NOT NULL,
  [PayloadJson] NVARCHAR(MAX) NULL,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_AuditLogs_Users] FOREIGN KEY ([ActorUserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Notifications (content-facing: IsVisible + ViewCount)
CREATE TABLE [dbo].[Notifications] (
  [Id] UNIQUEIDENTIFIER NOT NULL DEFAULT NEWID(),
  [UserId] UNIQUEIDENTIFIER NOT NULL,
  [Type] NVARCHAR(50) NOT NULL,
  [Title] NVARCHAR(200) NOT NULL,
  [Body] NVARCHAR(1000) NOT NULL,
  [IsRead] BIT NOT NULL DEFAULT 0,
  [ReadAtUtc] DATETIME2(0) NULL,
  [ViewCount] BIGINT NOT NULL DEFAULT 0,
  [IsVisible] BIT NOT NULL DEFAULT 1,
  [CreatedAtUtc] DATETIME2(0) NOT NULL DEFAULT SYSUTCDATETIME(),
  [UpdatedAtUtc] DATETIME2(0) NULL,
  CONSTRAINT [PK_Notifications] PRIMARY KEY ([Id]),
  CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users]([Id])
);
GO

-- Indexes
CREATE INDEX [IX_BrandProfiles_Status] ON [dbo].[BrandProfiles]([Status]);
CREATE INDEX [IX_KocProfiles_Status] ON [dbo].[KocProfiles]([Status]);
CREATE INDEX [IX_KocSocialAccounts_KocProfileId] ON [dbo].[KocSocialAccounts]([KocProfileId]);
CREATE INDEX [IX_KocPortfolios_KocProfileId] ON [dbo].[KocPortfolios]([KocProfileId]);
CREATE INDEX [IX_Campaigns_Status] ON [dbo].[Campaigns]([Status]);
CREATE INDEX [IX_Applications_Status] ON [dbo].[Applications]([Status]);
CREATE INDEX [IX_Schedules_Status] ON [dbo].[Schedules]([Status]);
CREATE INDEX [IX_Shipments_Status] ON [dbo].[Shipments]([Status]);
CREATE INDEX [IX_Submissions_Status] ON [dbo].[Submissions]([Status]);
CREATE INDEX [IX_SubmissionPosts_SubmissionId] ON [dbo].[SubmissionPosts]([SubmissionId]);
CREATE INDEX [IX_PostMetricSnapshots_Post_CapturedAt] ON [dbo].[PostMetricSnapshots]([SubmissionPostId],[CapturedAtUtc]);
CREATE INDEX [IX_CampaignKocDailyMetrics_Campaign_Date] ON [dbo].[CampaignKocDailyMetrics]([CampaignId],[MetricDate]);
CREATE INDEX [IX_Notifications_User_Read] ON [dbo].[Notifications]([UserId],[IsRead]);
GO

COMMIT TRANSACTION;
GO

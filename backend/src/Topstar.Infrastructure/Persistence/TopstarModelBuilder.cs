using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;

namespace Topstar.Infrastructure.Persistence;

internal static class TopstarModelBuilder
{
    public static void Configure(ModelBuilder modelBuilder)
    {
        ConfigureTables(modelBuilder);
        ConfigureRelationships(modelBuilder);
    }

    private static void ConfigureTables(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserEntity>(e =>
        {
            e.ToTable("Users");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.Email).IsUnique();
            e.HasIndex(x => x.Phone).IsUnique().HasFilter("[Phone] IS NOT NULL AND [Phone] <> ''");
            e.Property(x => x.Gender).HasMaxLength(20);
        });
        modelBuilder.Entity<RoleEntity>(e => { e.ToTable("Roles"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<UserRoleEntity>(e => { e.ToTable("UserRoles"); e.HasKey(x => new { x.UserId, x.RoleId }); });
        modelBuilder.Entity<BrandProfileEntity>(e => { e.ToTable("BrandProfiles",tb=>tb.HasTrigger("TR_BrandProfiles_CaptureStatusHistory")); e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); e.Property(x=>x.RefundBankName).HasMaxLength(200); e.Property(x=>x.RefundAccountNumber).HasMaxLength(100); e.Property(x=>x.RefundAccountHolder).HasMaxLength(200); });
        modelBuilder.Entity<BrandProfileStatusHistoryEntity>(e => { e.ToTable("BrandProfileStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.BrandProfileId, x.CreatedAtUtc }); });
        modelBuilder.Entity<KocProfileEntity>(e => { e.ToTable("KocProfiles",tb=>tb.HasTrigger("TR_KocProfiles_CaptureStatusHistory")); e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); e.Property(x => x.Score).HasPrecision(5, 2); });
        modelBuilder.Entity<KocProfileStatusHistoryEntity>(e => { e.ToTable("KocProfileStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.KocProfileId, x.CreatedAtUtc }); });
        modelBuilder.Entity<KocBodyProfileEntity>(e => { e.ToTable("KocBodyProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); e.Property(x => x.HeightCm).HasPrecision(5, 2); e.Property(x => x.WeightKg).HasPrecision(5, 2); e.Property(x => x.BustCm).HasPrecision(5, 2); e.Property(x => x.WaistCm).HasPrecision(5, 2); e.Property(x => x.HipCm).HasPrecision(5, 2); });
        modelBuilder.Entity<KocChildEntity>(e => { e.ToTable("KocChildren"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<KocSocialAccountEntity>(e => { e.ToTable("KocSocialAccounts"); e.HasKey(x => x.Id); e.Property(x => x.AvgEngagementRate).HasPrecision(7, 4); });
        modelBuilder.Entity<KocCategoryEntity>(e => { e.ToTable("KocCategories"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); e.HasIndex(x => new { x.IsActive, x.SortOrder }); });
        modelBuilder.Entity<KocCategoryHistoryEntity>(e => { e.ToTable("KocCategoryHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.KocCategoryId, x.CreatedAtUtc }); });
        modelBuilder.Entity<KocProfileCategoryEntity>(e => { e.ToTable("KocProfileCategories"); e.HasKey(x => new { x.KocProfileId, x.CategoryId }); });
        modelBuilder.Entity<KocSocialAccountCategoryEntity>(e => { e.ToTable("KocSocialAccountCategories"); e.HasKey(x => new { x.SocialAccountId, x.CategoryId }); });
        modelBuilder.Entity<KocExperienceEntity>(e => { e.ToTable("KocExperiences"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); e.Property(x => x.YearsOfExperience).HasPrecision(4, 1); });
        modelBuilder.Entity<KocProductionCapabilityEntity>(e => { e.ToTable("KocProductionCapabilities"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); });
        modelBuilder.Entity<KocJobPreferenceEntity>(e => { e.ToTable("KocJobPreferences"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); e.Property(x => x.MinBudgetAmount).HasPrecision(18, 2); });
        modelBuilder.Entity<KocPortfolioEntity>(e => { e.ToTable("KocPortfolios"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<KocMediaAssetEntity>(e => { e.ToTable("KocMediaAssets"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.KocProfileId, x.MediaType }); });
        modelBuilder.Entity<SocialPlatformConnectionEntity>(e =>
        {
            e.ToTable("SocialPlatformConnections");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.UserId, x.Platform }).IsUnique();
            e.HasIndex(x => x.PlatformUserId);
        });
        modelBuilder.Entity<SocialSyncJobEntity>(e =>
        {
            e.ToTable("SocialSyncJobs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.Status, x.RequestedAtUtc });
            e.HasIndex(x => x.KocProfileId);
            e.HasIndex(x => x.CampaignId);
        });
        modelBuilder.Entity<SocialProfileMetricSnapshotEntity>(e =>
        {
            e.ToTable("SocialProfileMetricSnapshots");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.KocProfileId, x.Platform, x.SnapshotAtUtc });
            e.Property(x => x.AvgEngagementRate).HasPrecision(10, 4);
        });
        modelBuilder.Entity<CampaignEntity>(e => { e.ToTable("Campaigns"); e.HasKey(x => x.Id); e.Property(x => x.BudgetAmount).HasPrecision(18, 2); e.Property(x => x.ExecutionMode).HasMaxLength(40); e.Property(x => x.VenueName).HasMaxLength(200); e.Property(x => x.VenueAddress).HasMaxLength(500); e.Property(x => x.ContactName).HasMaxLength(100); e.Property(x => x.ContactPhone).HasMaxLength(20); e.Property(x => x.ExecutionSchedule).HasMaxLength(500); e.Property(x => x.BookingInstructions).HasMaxLength(1000); e.Property(x => x.ShippingInstructions).HasMaxLength(1000); e.Property(x => x.SupportDetails).HasMaxLength(1000); e.Property(x => x.ExecutionRequirements).HasMaxLength(2000); });
        modelBuilder.Entity<CampaignStatusHistoryEntity>(e => { e.ToTable("CampaignStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.CreatedAtUtc }); });
        modelBuilder.Entity<CampaignDeliverableEntity>(e => { e.ToTable("CampaignDeliverables"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.CreatedAtUtc }); });
        modelBuilder.Entity<CampaignDeliverableHistoryEntity>(e => { e.ToTable("CampaignDeliverableHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.CreatedAtUtc }); });
        modelBuilder.Entity<CampaignReviewFeedbackItemEntity>(e => { e.ToTable("CampaignReviewFeedbackItems"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.CreatedAtUtc }); });
        modelBuilder.Entity<CampaignTargetingEntity>(e => { e.ToTable("CampaignTargetings"); e.HasKey(x => x.CampaignId); e.Property(x=>x.MinimumEngagementRate).HasPrecision(5,2); e.Property(x=>x.MaximumEngagementRate).HasPrecision(5,2); });
        modelBuilder.Entity<CampaignTargetProvinceEntity>(e => { e.ToTable("CampaignTargetProvinces"); e.HasKey(x => new { x.CampaignId, x.ProvinceId }); });
        modelBuilder.Entity<CampaignTargetCategoryEntity>(e => { e.ToTable("CampaignTargetCategories"); e.HasKey(x => new { x.CampaignId, x.CategoryId }); });
        modelBuilder.Entity<CampaignTargetingHistoryEntity>(e => { e.ToTable("CampaignTargetingHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.CreatedAtUtc }); });
        modelBuilder.Entity<ApplicationEntity>(e => { e.ToTable("Applications"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.KocProfileId }).IsUnique(); e.Property(x => x.ProposedAmount).HasPrecision(18, 2); });
        modelBuilder.Entity<KocCampaignFavoriteEntity>(e => { e.ToTable("KocCampaignFavorites"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.KocProfileId, x.CampaignId }).IsUnique(); });
        modelBuilder.Entity<ApplicationStatusHistoryEntity>(e => { e.ToTable("ApplicationStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ApplicationId, x.CreatedAtUtc }); });
        modelBuilder.Entity<ShortlistEntity>(e => { e.ToTable("Shortlists"); e.HasKey(x => x.Id); e.HasIndex(x => x.CampaignId).IsUnique(); });
        modelBuilder.Entity<ShortlistItemEntity>(e => { e.ToTable("ShortlistItems"); e.HasKey(x => new { x.ShortlistId, x.ApplicationId }); e.Property(x=>x.TargetingMatchScoreSnapshot).HasPrecision(5,2); });
        modelBuilder.Entity<ParticipationEntity>(e => { e.ToTable("Participations"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.KocProfileId }).IsUnique(); });
        modelBuilder.Entity<ParticipationStatusHistoryEntity>(e => { e.ToTable("ParticipationStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ParticipationId, x.CreatedAtUtc }); });
        modelBuilder.Entity<ScheduleEntity>(e => { e.ToTable("Schedules"); e.HasKey(x => x.Id); e.HasIndex(x => x.ParticipationId).IsUnique(); e.Property(x => x.Status).IsConcurrencyToken(); });
        modelBuilder.Entity<ScheduleStatusHistoryEntity>(e => { e.ToTable("ScheduleStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ScheduleId, x.CreatedAtUtc }); });
        modelBuilder.Entity<ScheduleReminderEventEntity>(e => { e.ToTable("ScheduleReminderEvents"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ScheduleId, x.ReminderCode, x.StartsAtUtcSnapshot }).IsUnique(); });
        modelBuilder.Entity<ShipmentEntity>(e => { e.ToTable("Shipments"); e.HasKey(x => x.Id); e.HasIndex(x => x.ParticipationId).IsUnique(); e.Property(x => x.Status).IsConcurrencyToken(); });
        modelBuilder.Entity<ShipmentEvidenceEntity>(e => { e.ToTable("ShipmentEvidence"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ShipmentId, x.CreatedAtUtc }); e.Property(x => x.EvidenceType).HasMaxLength(30); e.Property(x => x.FileUrl).HasMaxLength(1000); e.Property(x => x.Caption).HasMaxLength(500); e.Property(x => x.ActorRole).HasMaxLength(10); });
        modelBuilder.Entity<ShipmentTrackingEventEntity>(e => { e.ToTable("ShipmentTrackingEvents"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ShipmentId, x.OccurredAtUtc }); e.Property(x => x.EventCode).HasMaxLength(30); e.Property(x => x.Description).HasMaxLength(1000); e.Property(x => x.Location).HasMaxLength(300); e.Property(x => x.Source).HasMaxLength(30); });
        modelBuilder.Entity<ShipmentStatusHistoryEntity>(e => { e.ToTable("ShipmentStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ShipmentId, x.CreatedAtUtc }); });
        modelBuilder.Entity<SubmissionEntity>(e => { e.ToTable("Submissions"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ParticipationId, x.DeliverableId }).IsUnique(); e.Property(x => x.SubmissionNote).HasMaxLength(2000); e.Property(x => x.AttachmentUrl).HasMaxLength(1000); e.Property(x => x.AttachmentName).HasMaxLength(260); });
        modelBuilder.Entity<SubmissionHistoryEntity>(e => { e.ToTable("SubmissionHistories", tb => tb.UseSqlOutputClause(false)); e.HasKey(x => x.Id); e.HasIndex(x => new { x.SubmissionId, x.CreatedAtUtc }); e.Property(x => x.SubmissionNote).HasMaxLength(2000); e.Property(x => x.AttachmentUrl).HasMaxLength(1000); e.Property(x => x.AttachmentName).HasMaxLength(260); });
        modelBuilder.Entity<SubmissionPostEntity>(e => { e.ToTable("SubmissionPosts"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<PostMetricSnapshotEntity>(e => { e.ToTable("PostMetricSnapshots"); e.HasKey(x => x.Id); e.Property(x => x.EngagementRate).HasPrecision(7, 4); });
        modelBuilder.Entity<CampaignKocDailyMetricEntity>(e => { e.ToTable("CampaignKocDailyMetrics"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.KocProfileId, x.MetricDate }).IsUnique(); e.Property(x => x.SpendAmount).HasPrecision(18, 2); e.Property(x => x.RevenueAmount).HasPrecision(18, 2); });
        modelBuilder.Entity<ReviewEntity>(e => { e.ToTable("Reviews"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.ReviewerUserId, x.RevieweeUserId }).IsUnique(); });
        modelBuilder.Entity<ReviewReplyEntity>(e => { e.ToTable("ReviewReplies"); e.HasKey(x => x.Id); e.HasIndex(x => x.ReviewId).IsUnique(); e.Property(x => x.Body).HasMaxLength(2000); });
        modelBuilder.Entity<ReviewAppealEntity>(e => { e.ToTable("ReviewAppeals", tb => tb.HasTrigger("TR_ReviewAppeals_ProtectIdentity")); e.HasKey(x => x.Id); e.HasIndex(x => x.ReviewId).IsUnique(); e.HasIndex(x => new { x.Status, x.CreatedAtUtc }); e.Property(x => x.Reason).HasMaxLength(2000); e.Property(x => x.DecisionNote).HasMaxLength(2000); });
        modelBuilder.Entity<ReviewAppealHistoryEntity>(e => { e.ToTable("ReviewAppealHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.AppealId, x.CreatedAtUtc }); e.Property(x => x.Note).HasMaxLength(2000); });
        modelBuilder.Entity<ReviewStatusHistoryEntity>(e => { e.ToTable("ReviewStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ReviewId, x.CreatedAtUtc }); });
        modelBuilder.Entity<ReviewRevisionHistoryEntity>(e => { e.ToTable("ReviewRevisionHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ReviewId, x.CreatedAtUtc }); });
        modelBuilder.Entity<ReviewAppealSupplementEntity>(e => { e.ToTable("ReviewAppealSupplements"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.AppealId, x.CreatedAtUtc }); });
        modelBuilder.Entity<WalletTransactionEntity>(e => { e.ToTable("WalletTransactions"); e.HasKey(x => x.Id); e.HasIndex(x => x.ParticipationId).IsUnique(); e.HasIndex(x => x.HoldDisputeCaseId).HasFilter("[HoldDisputeCaseId] IS NOT NULL"); e.Property(x => x.Amount).HasPrecision(18, 2); });
        modelBuilder.Entity<KocPayoutAccountEntity>(e =>
        {
            e.ToTable("KocPayoutAccounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => new { x.KocProfileId, x.DeletedAtUtc });
            e.HasIndex(x => x.KocProfileId).IsUnique().HasFilter("[IsDefault] = 1 AND [DeletedAtUtc] IS NULL");
        });
        modelBuilder.Entity<PayoutRequestEntity>(e => { e.ToTable("PayoutRequests"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.KocProfileId, x.Status, x.RequestedAtUtc }); e.Property(x => x.Amount).HasPrecision(18, 2); });
        modelBuilder.Entity<WalletTransactionStatusHistoryEntity>(e => { e.ToTable("WalletTransactionStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.WalletTransactionId, x.CreatedAtUtc }); });
        modelBuilder.Entity<PayoutRequestStatusHistoryEntity>(e => { e.ToTable("PayoutRequestStatusHistories"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.PayoutRequestId, x.CreatedAtUtc }); });
        modelBuilder.Entity<PayoutPolicyEntity>(e=>{e.ToTable("PayoutPolicies");e.HasKey(x=>x.Id);e.HasIndex(x=>x.Code).IsUnique();e.Property(x=>x.MinimumAmount).HasPrecision(18,2);e.Property(x=>x.FixedFee).HasPrecision(18,2);e.Property(x=>x.FeeRate).HasPrecision(5,4);});
        modelBuilder.Entity<PayoutPolicyHistoryEntity>(e=>{e.ToTable("PayoutPolicyHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.PolicyId,x.CreatedAtUtc});e.Property(x=>x.MinimumAmount).HasPrecision(18,2);e.Property(x=>x.FixedFee).HasPrecision(18,2);e.Property(x=>x.FeeRate).HasPrecision(5,4);});
        modelBuilder.Entity<PayoutOtpChallengeEntity>(e=>{e.ToTable("PayoutOtpChallenges");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.KocProfileId,x.CreatedAtUtc});e.Property(x=>x.Amount).HasPrecision(18,2);e.Property(x=>x.CodeHash).HasMaxLength(500);});
        modelBuilder.Entity<PayoutRequestEntity>().Property(x=>x.FeeAmountSnapshot).HasPrecision(18,2);modelBuilder.Entity<PayoutRequestEntity>().Property(x=>x.NetAmountSnapshot).HasPrecision(18,2);
        modelBuilder.Entity<CampaignBillingInvoiceEntity>(e=>{e.ToTable("CampaignBillingInvoices");e.HasKey(x=>x.Id);e.HasIndex(x=>x.CampaignId).IsUnique();e.HasIndex(x=>x.InvoiceNumber).IsUnique();e.Property(x=>x.CampaignBudgetSnapshot).HasPrecision(18,2);e.Property(x=>x.FeeRate).HasPrecision(5,4);e.Property(x=>x.FeeAmount).HasPrecision(18,2);e.Property(x=>x.Status).IsConcurrencyToken();});
        modelBuilder.Entity<CampaignBillingInvoiceHistoryEntity>(e=>{e.ToTable("CampaignBillingInvoiceHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.InvoiceId,x.CreatedAtUtc});});
        modelBuilder.Entity<DisputeCaseEntity>(e => { e.ToTable("DisputeCases"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.ParticipationId, x.Category, x.IsActive }).IsUnique().HasFilter("[IsActive] = 1"); e.Property(x=>x.Status).IsConcurrencyToken(); });
        modelBuilder.Entity<DisputeCaseStatusHistoryEntity>(e => { e.ToTable("DisputeCaseStatusHistories"); e.HasKey(x=>x.Id); e.HasIndex(x=>new{x.DisputeCaseId,x.CreatedAtUtc}); });
        modelBuilder.Entity<DisputeMessageEntity>(e => { e.ToTable("DisputeMessages"); e.HasKey(x=>x.Id); e.HasIndex(x=>new{x.DisputeCaseId,x.CreatedAtUtc}); });
        modelBuilder.Entity<AuditLogEntity>(e => { e.ToTable("AuditLogs"); e.HasKey(x => x.Id);e.Property(x=>x.ActorEmailSnapshot).HasMaxLength(256);e.Property(x=>x.ActorRoleSnapshot).HasMaxLength(200); });
        modelBuilder.Entity<NotificationEntity>(e => { e.ToTable("Notifications"); e.HasKey(x => x.Id); e.Property(x=>x.TargetUrl).HasMaxLength(500); e.Property(x=>x.SourceEntityType).HasMaxLength(50); e.Property(x=>x.AudienceRole).HasMaxLength(10); });
        modelBuilder.Entity<NotificationTemplateEntity>(e=>{e.ToTable("NotificationTemplates");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.Type,x.AudienceRole}).IsUnique();e.Property(x=>x.Type).HasMaxLength(50);e.Property(x=>x.AudienceRole).HasMaxLength(10);e.Property(x=>x.TitleTemplate).HasMaxLength(200);e.Property(x=>x.BodyTemplate).HasMaxLength(1000);});
        modelBuilder.Entity<NotificationTemplateHistoryEntity>(e=>{e.ToTable("NotificationTemplateHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.TemplateId,x.CreatedAtUtc});e.Property(x=>x.TitleTemplate).HasMaxLength(200);e.Property(x=>x.BodyTemplate).HasMaxLength(1000);e.Property(x=>x.Reason).HasMaxLength(1000);});
        modelBuilder.Entity<UserViolationEntity>(e=>{e.ToTable("UserViolations");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.UserId,x.Status,x.CreatedAtUtc});e.Property(x=>x.Category).HasMaxLength(100);e.Property(x=>x.Severity).HasMaxLength(20);e.Property(x=>x.Reason).HasMaxLength(2000);e.Property(x=>x.ResolutionNote).HasMaxLength(1000);});
        modelBuilder.Entity<UserViolationHistoryEntity>(e=>{e.ToTable("UserViolationHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.ViolationId,x.CreatedAtUtc});e.Property(x=>x.Note).HasMaxLength(2000);});
        modelBuilder.Entity<UserAccountStatusHistoryEntity>(e=>{e.ToTable("UserAccountStatusHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.UserId,x.CreatedAtUtc});e.Property(x=>x.Reason).HasMaxLength(1000);});
        modelBuilder.Entity<UserAccountAdminNoteEntity>(e=>{e.ToTable("UserAccountAdminNotes");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.UserId,x.CreatedAtUtc});e.Property(x=>x.Body).HasMaxLength(2000);});
        modelBuilder.Entity<AcademyCourseEntity>(e=>{e.ToTable("AcademyCourses");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.Status,x.OrderNo});e.Property(x=>x.Title).HasMaxLength(160);e.Property(x=>x.Level).HasMaxLength(50);e.Property(x=>x.Summary).HasMaxLength(1000);e.Property(x=>x.Syllabus).HasMaxLength(4000);});
        modelBuilder.Entity<AcademyCourseHistoryEntity>(e=>{e.ToTable("AcademyCourseHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.CourseId,x.CreatedAtUtc});e.Property(x=>x.ActionName).HasMaxLength(30);e.Property(x=>x.Note).HasMaxLength(1000);});
        modelBuilder.Entity<AcademyLessonEntity>(e=>{e.ToTable("AcademyLessons");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.CourseId,x.OrderNo});e.Property(x=>x.Title).HasMaxLength(200);e.Property(x=>x.ContentUrl).HasMaxLength(1000);});
        modelBuilder.Entity<AcademyLessonHistoryEntity>(e=>{e.ToTable("AcademyLessonHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.LessonId,x.CreatedAtUtc});e.Property(x=>x.TitleSnapshot).HasMaxLength(200);e.Property(x=>x.ContentUrlSnapshot).HasMaxLength(1000);e.Property(x=>x.Reason).HasMaxLength(1000);});
        modelBuilder.Entity<AcademyEnrollmentEntity>(e=>{e.ToTable("AcademyEnrollments");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.CourseId,x.KocUserId}).IsUnique();e.HasIndex(x=>x.CertificateCode).IsUnique().HasFilter("[CertificateCode] IS NOT NULL");e.Property(x=>x.CertificateCode).HasMaxLength(40);e.Property(x=>x.CourseTitleSnapshot).HasMaxLength(160);e.Property(x=>x.KocDisplayNameSnapshot).HasMaxLength(200);});
        modelBuilder.Entity<AcademyLessonCompletionEntity>(e=>{e.ToTable("AcademyLessonCompletions");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.EnrollmentId,x.LessonId}).IsUnique();});
        modelBuilder.Entity<AcademyEnrollmentLessonEntity>(e=>{e.ToTable("AcademyEnrollmentLessons");e.HasKey(x=>new{x.EnrollmentId,x.LessonId});e.HasIndex(x=>new{x.EnrollmentId,x.OrderNoSnapshot});e.Property(x=>x.TitleSnapshot).HasMaxLength(200);});
        modelBuilder.Entity<NotificationEventEntity>(e => { e.ToTable("NotificationEvents"); e.HasKey(x=>x.Id); e.HasIndex(x=>new{x.NotificationId,x.CreatedAtUtc}); e.Property(x=>x.TargetUrlSnapshot).HasMaxLength(500); e.Property(x=>x.ActorRoleSnapshot).HasMaxLength(10); });
        modelBuilder.Entity<OperationalEscalationEntity>(e => { e.ToTable("OperationalEscalations"); e.HasKey(x=>x.Id); e.HasIndex(x=>new{x.RuleCode,x.SourceEntityType,x.SourceEntityId}).IsUnique(); e.Property(x=>x.Status).IsConcurrencyToken(); });
        modelBuilder.Entity<OperationalEscalationHistoryEntity>(e => { e.ToTable("OperationalEscalationHistories"); e.HasKey(x=>x.Id); e.HasIndex(x=>new{x.EscalationId,x.CreatedAtUtc}); });
        modelBuilder.Entity<OperationalScanRunEntity>(e => { e.ToTable("OperationalScanRuns"); e.HasKey(x=>x.Id); e.HasIndex(x=>new{x.RuleCode,x.CompletedAtUtc}); });
        modelBuilder.Entity<OperationalSlaPolicyEntity>(e => { e.ToTable("OperationalSlaPolicies"); e.HasKey(x=>x.Id); e.HasIndex(x=>x.RuleCode).IsUnique(); e.Property(x=>x.UpdatedAtUtc).IsConcurrencyToken(); });
        modelBuilder.Entity<OperationalSlaPolicyHistoryEntity>(e => { e.ToTable("OperationalSlaPolicyHistories"); e.HasKey(x=>x.Id); e.HasIndex(x=>new{x.PolicyId,x.CreatedAtUtc}); });
        modelBuilder.Entity<AdminProfileEntity>(e => { e.ToTable("AdminProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); });
        modelBuilder.Entity<PermissionEntity>(e => { e.ToTable("Permissions"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<RolePermissionEntity>(e => { e.ToTable("RolePermissions"); e.HasKey(x => new { x.RoleId, x.PermissionId }); });
        modelBuilder.Entity<UserPermissionEntity>(e => { e.ToTable("UserPermissions"); e.HasKey(x => new { x.UserId, x.PermissionId }); });
        modelBuilder.Entity<MenuEntity>(e => { e.ToTable("Menus"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<RoleMenuEntity>(e => { e.ToTable("RoleMenus"); e.HasKey(x => new { x.RoleId, x.MenuId }); });
        modelBuilder.Entity<UserMenuEntity>(e => { e.ToTable("UserMenus"); e.HasKey(x => new { x.UserId, x.MenuId }); });
        modelBuilder.Entity<DashboardWidgetEntity>(e => { e.ToTable("DashboardWidgets"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<RoleDashboardConfigEntity>(e => { e.ToTable("RoleDashboardConfigs"); e.HasKey(x => new { x.RoleId, x.WidgetId }); });
        modelBuilder.Entity<UserDashboardConfigEntity>(e => { e.ToTable("UserDashboardConfigs"); e.HasKey(x => new { x.UserId, x.WidgetId }); });
        modelBuilder.Entity<UserDeviceEntity>(e => { e.ToTable("UserDevices"); e.HasKey(x => x.Id); e.HasIndex(x => x.DeviceFingerprint).IsUnique(); });
        modelBuilder.Entity<AuthEventEntity>(e => { e.ToTable("AuthEvents"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<AuthSessionEntity>(e => { e.ToTable("AuthSessions"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<PasswordResetTokenEntity>(e => { e.ToTable("PasswordResetTokens"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<EmailVerificationTokenEntity>(e => { e.ToTable("EmailVerificationTokens"); e.HasKey(x => x.Id); e.Property(x => x.CodeHash).HasMaxLength(64); e.HasIndex(x => new { x.UserId, x.CreatedAtUtc }); });
        modelBuilder.Entity<ProvinceEntity>(e => { e.ToTable("Provinces"); e.HasKey(x => x.Id); e.HasIndex(x => x.ProvinceCode).IsUnique(); });
        modelBuilder.Entity<WardEntity>(e => { e.ToTable("Wards"); e.HasKey(x => x.Id); e.HasIndex(x => x.WardCode).IsUnique(); });
        modelBuilder.Entity<SystemEmailAccountEntity>(e =>
        {
            e.ToTable("SystemEmailAccounts");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.EmailAddress);
        });
        modelBuilder.Entity<SystemShippingConfigEntity>(e =>
        {
            e.ToTable("SystemShippingConfigs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.ProviderCode).IsUnique();
        });
        modelBuilder.Entity<SystemWebsiteConfigEntity>(e =>
        {
            e.ToTable("SystemWebsiteConfigs");
            e.HasKey(x => x.Id);
        });
        modelBuilder.Entity<PublicPageSeoConfigEntity>(e =>
        {
            e.ToTable("PublicPageSeoConfigs");
            e.HasKey(x => x.Id);
            e.HasIndex(x => x.PageKey).IsUnique();
            e.HasIndex(x => x.PagePath).IsUnique();
        });
        modelBuilder.Entity<SocialLoginConfigurationEntity>(e =>
        {
            e.ToTable("SocialLoginConfigurations");
            e.HasKey(x => x.Id);
            e.Property(x => x.Provider).HasMaxLength(30);
            e.Property(x => x.RedirectUri).HasMaxLength(1000);
            e.Property(x => x.ClientId).HasMaxLength(500);
            e.Property(x => x.ClientSecret).HasMaxLength(1000);
            e.HasIndex(x => x.Provider).IsUnique();
        });
        modelBuilder.Entity<HomepageContentHistoryEntity>(e =>
        {
            e.ToTable("HomepageContentHistories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => x.CreatedAtUtc);
        });
        modelBuilder.Entity<AcademyContentHistoryEntity>(e =>
        {
            e.ToTable("AcademyContentHistories");
            e.HasKey(x => x.Id);
            e.Property(x => x.Reason).HasMaxLength(1000);
            e.HasIndex(x => x.CreatedAtUtc);
        });
        modelBuilder.Entity<CampaignPackagePolicyEntity>(e =>
        {
            e.ToTable("CampaignPackagePolicies", table =>
                table.HasCheckConstraint("CK_CampaignPackagePolicies_Quota", "[SelectionQuota] BETWEEN 1 AND 100"));
            e.HasKey(x => x.Id);
            e.Property(x => x.Code).HasMaxLength(80);
            e.Property(x => x.Name).HasMaxLength(150);
            e.Property(x => x.ImageUrl).HasMaxLength(500);
            e.Property(x => x.Description).HasMaxLength(1000);
            e.Property(x => x.Benefits).HasMaxLength(8000);
            e.Property(x => x.SalePrice).HasColumnType("decimal(18,2)");
            e.Property(x => x.MinBudgetAmount).HasPrecision(18, 2);
            e.Property(x => x.MaxBudgetAmount).HasPrecision(18, 2);
            e.HasIndex(x => x.Code).IsUnique();
        });
        modelBuilder.Entity<CampaignPackagePolicyHistoryEntity>(e=>{e.ToTable("CampaignPackagePolicyHistories",table=>table.HasCheckConstraint("CK_CampaignPackagePolicyHistories_Quota","[SelectionQuota] BETWEEN 1 AND 100"));e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.PolicyId,x.CreatedAtUtc});e.Property(x=>x.Name).HasMaxLength(150);e.Property(x=>x.ImageUrl).HasMaxLength(500);e.Property(x=>x.Description).HasMaxLength(1000);e.Property(x=>x.Benefits).HasMaxLength(8000);e.Property(x=>x.SalePrice).HasColumnType("decimal(18,2)");e.Property(x=>x.MinBudgetAmount).HasPrecision(18,2);e.Property(x=>x.MaxBudgetAmount).HasPrecision(18,2);e.Property(x=>x.Reason).HasMaxLength(1000);});
        modelBuilder.Entity<OperationalRuntimePolicyEntity>(e=>{e.ToTable("OperationalRuntimePolicies");e.HasKey(x=>x.Id);});
        modelBuilder.Entity<OperationalRuntimePolicyHistoryEntity>(e=>{e.ToTable("OperationalRuntimePolicyHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.PolicyId,x.CreatedAtUtc});e.Property(x=>x.SnapshotJson).HasMaxLength(4000);e.Property(x=>x.Reason).HasMaxLength(500);});
        modelBuilder.Entity<OperationalCatalogItemEntity>(e=>{e.ToTable("OperationalCatalogItems");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.CatalogType,x.Code}).IsUnique();e.Property(x=>x.CatalogType).HasMaxLength(40);e.Property(x=>x.Code).HasMaxLength(80);e.Property(x=>x.Name).HasMaxLength(150);e.Property(x=>x.Description).HasMaxLength(500);e.Property(x=>x.DefaultSeverity).HasMaxLength(20);});
        modelBuilder.Entity<OperationalCatalogHistoryEntity>(e=>{e.ToTable("OperationalCatalogHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.CatalogItemId,x.CreatedAtUtc});e.Property(x=>x.Action).HasMaxLength(20);e.Property(x=>x.SnapshotJson).HasMaxLength(4000);e.Property(x=>x.Reason).HasMaxLength(500);});
        modelBuilder.Entity<BrandPackagePurchaseEntity>(e=>{e.ToTable("BrandPackagePurchases");e.HasKey(x=>x.Id);e.HasIndex(x=>x.OrderNumber).IsUnique();e.HasIndex(x=>new{x.BrandProfileId,x.Status});e.HasIndex(x=>x.CampaignId).IsUnique().HasFilter("[CampaignId] IS NOT NULL");e.Property(x=>x.AmountSnapshot).HasColumnType("decimal(18,2)");e.Property(x=>x.MinBudgetAmountSnapshot).HasColumnType("decimal(18,2)");e.Property(x=>x.MaxBudgetAmountSnapshot).HasColumnType("decimal(18,2)");e.Property(x=>x.PaymentReference).HasMaxLength(100).IsUnicode();e.Property(x=>x.PaymentMethod).HasMaxLength(50);e.Property(x=>x.PaymentProofUrl).HasMaxLength(1000);e.Property(x=>x.PaymentProofNote).HasMaxLength(500);e.Property(x=>x.Status).IsConcurrencyToken();});
        modelBuilder.Entity<BrandPackagePurchaseHistoryEntity>(e=>{e.ToTable("BrandPackagePurchaseHistories");e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.PurchaseId,x.CreatedAtUtc});});
        modelBuilder.Entity<PaymentMethodConfigurationEntity>(e=>{e.ToTable("PaymentMethodConfigurations");e.HasKey(x=>x.Id);});
        modelBuilder.Entity<VnPayCallbackLogEntity>(e=>{e.ToTable("VnPayCallbackLogs",tb=>tb.UseSqlOutputClause(false));e.HasKey(x=>x.Id);e.HasIndex(x=>new{x.TargetReference,x.ReceivedAtUtc});e.Property(x=>x.Source).HasMaxLength(10);e.Property(x=>x.TargetReference).HasMaxLength(100);e.Property(x=>x.TransactionNo).HasMaxLength(100);e.Property(x=>x.RemoteIp).HasMaxLength(100);e.Property(x=>x.UserAgent).HasMaxLength(2000);e.Property(x=>x.ResponseCode).HasMaxLength(10);e.Property(x=>x.ResultMessage).HasMaxLength(2000);e.Property(x=>x.ErrorDetail).HasMaxLength(4000);});
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRoleEntity>().HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
        modelBuilder.Entity<UserRoleEntity>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        modelBuilder.Entity<BrandProfileEntity>().HasOne(x => x.User).WithOne(x => x.BrandProfile).HasForeignKey<BrandProfileEntity>(x => x.UserId);
        modelBuilder.Entity<BrandProfileStatusHistoryEntity>().HasOne<BrandProfileEntity>().WithMany().HasForeignKey(x => x.BrandProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<KocProfileEntity>().HasOne(x => x.User).WithOne(x => x.KocProfile).HasForeignKey<KocProfileEntity>(x => x.UserId);
        modelBuilder.Entity<KocProfileStatusHistoryEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AdminProfileEntity>().HasOne(x => x.User).WithOne(x => x.AdminProfile).HasForeignKey<AdminProfileEntity>(x => x.UserId);

        modelBuilder.Entity<KocBodyProfileEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocChildEntity>().HasOne<KocBodyProfileEntity>().WithMany().HasForeignKey(x => x.KocBodyProfileId);
        modelBuilder.Entity<KocSocialAccountEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocProfileCategoryEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocProfileCategoryEntity>().HasOne<KocCategoryEntity>().WithMany().HasForeignKey(x => x.CategoryId);
        modelBuilder.Entity<KocSocialAccountCategoryEntity>().HasOne<KocSocialAccountEntity>().WithMany().HasForeignKey(x => x.SocialAccountId);
        modelBuilder.Entity<KocSocialAccountCategoryEntity>().HasOne<KocCategoryEntity>().WithMany().HasForeignKey(x => x.CategoryId);
        modelBuilder.Entity<KocExperienceEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocProductionCapabilityEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocJobPreferenceEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocPortfolioEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocMediaAssetEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<SocialPlatformConnectionEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<SocialSyncJobEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.RequestedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SocialSyncJobEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SocialSyncJobEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SocialProfileMetricSnapshotEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);

        modelBuilder.Entity<CampaignEntity>().HasOne<BrandProfileEntity>().WithMany().HasForeignKey(x => x.BrandProfileId);
        modelBuilder.Entity<CampaignEntity>().HasOne<DisputeCaseEntity>().WithMany().HasForeignKey(x => x.CancellationDisputeCaseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.CancelledByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignStatusHistoryEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignDeliverableEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<CampaignDeliverableHistoryEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignDeliverableHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignReviewFeedbackItemEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignReviewFeedbackItemEntity>().HasOne<CampaignStatusHistoryEntity>().WithMany().HasForeignKey(x => x.CampaignStatusHistoryId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignReviewFeedbackItemEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignTargetingEntity>().HasOne<CampaignEntity>().WithOne().HasForeignKey<CampaignTargetingEntity>(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CampaignTargetProvinceEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CampaignTargetProvinceEntity>().HasOne<ProvinceEntity>().WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignTargetCategoryEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<CampaignTargetCategoryEntity>().HasOne<KocCategoryEntity>().WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignTargetingHistoryEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignTargetingHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ApplicationEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ApplicationEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocCampaignFavoriteEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<KocCampaignFavoriteEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.Cascade);
        modelBuilder.Entity<ApplicationStatusHistoryEntity>().HasOne<ApplicationEntity>().WithMany().HasForeignKey(x => x.ApplicationId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ApplicationStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShortlistEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ShortlistEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShortlistEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.FinalizedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShortlistItemEntity>().HasOne<ShortlistEntity>().WithMany().HasForeignKey(x => x.ShortlistId);
        modelBuilder.Entity<ShortlistItemEntity>().HasOne<ApplicationEntity>().WithMany().HasForeignKey(x => x.ApplicationId);
        modelBuilder.Entity<ParticipationEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ParticipationEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<ParticipationStatusHistoryEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ParticipationStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ScheduleEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId);
        modelBuilder.Entity<ScheduleStatusHistoryEntity>().HasOne<ScheduleEntity>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ScheduleStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ScheduleReminderEventEntity>().HasOne<ScheduleEntity>().WithMany().HasForeignKey(x => x.ScheduleId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId);
        modelBuilder.Entity<ShipmentStatusHistoryEntity>().HasOne<ShipmentEntity>().WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentEvidenceEntity>().HasOne<ShipmentEntity>().WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentEvidenceEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UploadedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentTrackingEventEntity>().HasOne<ShipmentEntity>().WithMany().HasForeignKey(x => x.ShipmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentTrackingEventEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SubmissionEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId);
        modelBuilder.Entity<SubmissionEntity>().HasOne<CampaignDeliverableEntity>().WithMany().HasForeignKey(x => x.DeliverableId);
        modelBuilder.Entity<SubmissionHistoryEntity>().HasOne<SubmissionEntity>().WithMany().HasForeignKey(x => x.SubmissionId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SubmissionHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<SubmissionPostEntity>().HasOne<SubmissionEntity>().WithMany().HasForeignKey(x => x.SubmissionId);
        modelBuilder.Entity<PostMetricSnapshotEntity>().HasOne<SubmissionPostEntity>().WithMany().HasForeignKey(x => x.SubmissionPostId);
        modelBuilder.Entity<CampaignKocDailyMetricEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<CampaignKocDailyMetricEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<ReviewEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ReviewEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ReviewerUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.RevieweeUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewStatusHistoryEntity>().HasOne<ReviewEntity>().WithMany().HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewReplyEntity>().HasOne<ReviewEntity>().WithMany().HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewReplyEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewAppealEntity>().HasOne<ReviewEntity>().WithMany().HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewAppealEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.AppellantUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewAppealEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ResolvedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewAppealHistoryEntity>().HasOne<ReviewAppealEntity>().WithMany().HasForeignKey(x => x.AppealId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewAppealHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewRevisionHistoryEntity>().HasOne<ReviewEntity>().WithMany().HasForeignKey(x => x.ReviewId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewRevisionHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewAppealSupplementEntity>().HasOne<ReviewAppealEntity>().WithMany().HasForeignKey(x => x.AppealId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewAppealSupplementEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignPackagePolicyEntity>().Property(x=>x.ReferenceBudget).HasPrecision(18,0);
        modelBuilder.Entity<CampaignPackagePolicyHistoryEntity>().Property(x=>x.ReferenceBudget).HasPrecision(18,0);
        modelBuilder.Entity<CampaignEntity>().Property(x=>x.KocPlatformFeeRateSnapshot).HasPrecision(5,2);
        modelBuilder.Entity<WalletTransactionEntity>().Property(x=>x.PlatformFeeRateSnapshot).HasPrecision(5,2);
        modelBuilder.Entity<WalletTransactionEntity>().Property(x=>x.PlatformFeeAmountSnapshot).HasPrecision(18,0);
        modelBuilder.Entity<WalletTransactionEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionEntity>().HasOne<DisputeCaseEntity>().WithMany().HasForeignKey(x => x.HoldDisputeCaseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<KocPayoutAccountEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PayoutRequestEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PayoutRequestEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ProcessedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionStatusHistoryEntity>().HasOne<WalletTransactionEntity>().WithMany().HasForeignKey(x => x.WalletTransactionId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionStatusHistoryEntity>().HasOne<DisputeCaseEntity>().WithMany().HasForeignKey(x => x.DisputeCaseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PayoutRequestStatusHistoryEntity>().HasOne<PayoutRequestEntity>().WithMany().HasForeignKey(x => x.PayoutRequestId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PayoutPolicyEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PayoutPolicyHistoryEntity>().HasOne<PayoutPolicyEntity>().WithMany().HasForeignKey(x=>x.PolicyId).OnDelete(DeleteBehavior.NoAction);modelBuilder.Entity<PayoutPolicyHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PayoutOtpChallengeEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x=>x.KocProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignBillingInvoiceEntity>().HasOne<CampaignEntity>().WithOne().HasForeignKey<CampaignBillingInvoiceEntity>(x=>x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignBillingInvoiceEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignBillingInvoiceEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignBillingInvoiceHistoryEntity>().HasOne<CampaignBillingInvoiceEntity>().WithMany().HasForeignKey(x=>x.InvoiceId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignBillingInvoiceHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignFeePolicyEntity>(e=>{e.ToTable("CampaignFeePolicies");e.HasKey(x=>x.Id);e.Property(x=>x.FeeRate).HasPrecision(9,6);e.Property(x=>x.GatewayFeeRate).HasPrecision(9,6);e.Property(x=>x.FixedFee).HasPrecision(18,2);e.Property(x=>x.MinimumFee).HasPrecision(18,2);e.Property(x=>x.MaximumFee).HasPrecision(18,2);e.Property(x=>x.DualApprovalThreshold).HasPrecision(18,2);});
        modelBuilder.Entity<CampaignFeePolicyHistoryEntity>(e=>e.Property(x=>x.DualApprovalThreshold).HasPrecision(18,2));
        modelBuilder.Entity<CampaignFeePolicyHistoryEntity>(e=>{e.ToTable("CampaignFeePolicyHistories");e.HasKey(x=>x.Id);e.Property(x=>x.FeeRate).HasPrecision(9,6);e.Property(x=>x.GatewayFeeRate).HasPrecision(9,6);e.Property(x=>x.FixedFee).HasPrecision(18,2);e.Property(x=>x.MinimumFee).HasPrecision(18,2);e.Property(x=>x.MaximumFee).HasPrecision(18,2);});
        // Both tables are protected by AFTER triggers. EF Core's default OUTPUT
        // clause is incompatible with those triggers on SQL Server.
        modelBuilder.Entity<CampaignFundingEntity>(e=>{e.ToTable("CampaignFundings",tb=>tb.UseSqlOutputClause(false));e.HasKey(x=>x.Id);e.Property(x=>x.CampaignBudgetSnapshot).HasPrecision(18,2);e.Property(x=>x.FeeRateSnapshot).HasPrecision(9,6);e.Property(x=>x.FixedFeeSnapshot).HasPrecision(18,2);e.Property(x=>x.PlatformFeeSnapshot).HasPrecision(18,2);e.Property(x=>x.GatewayFeeRateSnapshot).HasPrecision(9,6);e.Property(x=>x.GatewayFeeSnapshot).HasPrecision(18,2);e.Property(x=>x.TotalPayableSnapshot).HasPrecision(18,2);e.Property(x=>x.PaymentProofUrl).HasMaxLength(1000);e.Property(x=>x.PaymentProofNote).HasMaxLength(500);e.Property(x=>x.RefundedAmount).HasPrecision(18,2);e.Property(x=>x.RefundBankName).HasMaxLength(200);e.Property(x=>x.RefundAccountNumber).HasMaxLength(100);e.Property(x=>x.RefundAccountHolder).HasMaxLength(200);});
        modelBuilder.Entity<CampaignFundingAllocationEntity>(e=>{e.ToTable("CampaignFundingAllocations",tb=>tb.UseSqlOutputClause(false));e.HasKey(x=>x.Id);e.Property(x=>x.Amount).HasPrecision(18,2);e.Property(x=>x.AcceptedAmount).HasPrecision(18,2);e.Property(x=>x.RefundedAmount).HasPrecision(18,2);});
        modelBuilder.Entity<CampaignFundLedgerEntity>(e=>{e.ToTable("CampaignFundLedgers");e.HasKey(x=>x.Id);e.Property(x=>x.Amount).HasPrecision(18,2);});
        modelBuilder.Entity<CampaignFundingEvidenceEntity>(e=>{e.ToTable("CampaignFundingEvidences",tb=>tb.UseSqlOutputClause(false));e.HasKey(x=>x.Id);e.Property(x=>x.EvidenceType).HasMaxLength(40);e.Property(x=>x.FileUrl).HasMaxLength(1000);e.Property(x=>x.Note).HasMaxLength(500);e.Property(x=>x.Reference).HasMaxLength(200);e.Property(x=>x.Amount).HasPrecision(18,2);});
        modelBuilder.Entity<CampaignFundingEvidenceReviewEntity>(e=>{e.ToTable("CampaignFundingEvidenceReviews",tb=>tb.UseSqlOutputClause(false));e.HasKey(x=>x.Id);e.Property(x=>x.Status).HasMaxLength(30);e.Property(x=>x.Reason).HasMaxLength(500);e.HasIndex(x=>new{x.EvidenceId,x.CreatedAtUtc});});
        modelBuilder.Entity<CampaignFundingApprovalEntity>(e=>{e.ToTable("CampaignFundingApprovals");e.HasKey(x=>x.Id);e.Property(x=>x.ActionType).HasMaxLength(40);e.Property(x=>x.Reference).HasMaxLength(200);e.Property(x=>x.Status).HasMaxLength(20);e.Property(x=>x.Amount).HasPrecision(18,2);});
        modelBuilder.Entity<CampaignFeePolicyHistoryEntity>().HasOne<CampaignFeePolicyEntity>().WithMany().HasForeignKey(x=>x.PolicyId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<WalletTransactionEntity>().ToTable("WalletTransactions",t=>t.UseSqlOutputClause(false));
        modelBuilder.Entity<WalletTransactionEntity>().Property(x=>x.GrossAmountSnapshot).HasPrecision(18,0);
        modelBuilder.Entity<WalletTransactionEntity>().Property(x=>x.TaxAmountSnapshot).HasPrecision(18,0);
        modelBuilder.Entity<WalletTransactionEntity>().Property(x=>x.TaxRateSnapshot).HasPrecision(5,2);
        modelBuilder.Entity<CampaignFundingEntity>().Property(x=>x.VatRateSnapshot).HasPrecision(5,2);
        modelBuilder.Entity<CampaignFundingEntity>().Property(x=>x.VatAmountSnapshot).HasPrecision(18,0);
        modelBuilder.Entity<CampaignFundingEntity>().Property(x=>x.VatAddedSnapshot).HasPrecision(18,0);
        modelBuilder.Entity<BrandPackagePurchaseEntity>().Property(x=>x.VatRateSnapshot).HasPrecision(5,2);
        modelBuilder.Entity<BrandPackagePurchaseEntity>().Property(x=>x.VatAmountSnapshot).HasPrecision(18,0);
        modelBuilder.Entity<BrandPackagePurchaseEntity>().Property(x=>x.VatAddedSnapshot).HasPrecision(18,0);
        modelBuilder.Entity<CampaignFundingEntity>().Property(x=>x.BalanceAppliedAmount).HasPrecision(18,0);
        modelBuilder.Entity<CampaignFundingEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x=>x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignFundingAllocationEntity>().HasOne<CampaignFundingEntity>().WithMany().HasForeignKey(x=>x.FundingId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignFundingAllocationEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x=>x.ParticipationId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignFundLedgerEntity>().HasOne<CampaignFundingEntity>().WithMany().HasForeignKey(x=>x.FundingId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignFundingEvidenceEntity>().HasOne<CampaignFundingEntity>().WithMany().HasForeignKey(x=>x.FundingId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignFundingEvidenceReviewEntity>().HasOne<CampaignFundingEvidenceEntity>().WithMany().HasForeignKey(x=>x.EvidenceId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignFundingApprovalEntity>().HasOne<CampaignFundingEntity>().WithMany().HasForeignKey(x=>x.FundingId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<PayoutRequestStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DisputeCaseEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x=>x.ParticipationId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DisputeCaseEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DisputeCaseEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.AssignedAdminUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DisputeCaseStatusHistoryEntity>().HasOne<DisputeCaseEntity>().WithMany().HasForeignKey(x=>x.DisputeCaseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DisputeCaseStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DisputeMessageEntity>().HasOne<DisputeCaseEntity>().WithMany().HasForeignKey(x=>x.DisputeCaseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<DisputeMessageEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.AuthorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AuditLogEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId);
        modelBuilder.Entity<NotificationEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<NotificationTemplateEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<NotificationTemplateEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<NotificationTemplateHistoryEntity>().HasOne<NotificationTemplateEntity>().WithMany().HasForeignKey(x=>x.TemplateId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<NotificationTemplateHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserViolationEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserViolationEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserViolationEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.RevokedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserViolationHistoryEntity>().HasOne<UserViolationEntity>().WithMany().HasForeignKey(x=>x.ViolationId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserViolationHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserAccountStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.UserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<UserAccountStatusHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyCourseEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyCourseEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyCourseHistoryEntity>().HasOne<AcademyCourseEntity>().WithMany().HasForeignKey(x=>x.CourseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyCourseHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyLessonEntity>().HasOne<AcademyCourseEntity>().WithMany().HasForeignKey(x=>x.CourseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyLessonEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyLessonEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.UpdatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyLessonHistoryEntity>().HasOne<AcademyLessonEntity>().WithMany().HasForeignKey(x=>x.LessonId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyLessonHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyEnrollmentEntity>().HasOne<AcademyCourseEntity>().WithMany().HasForeignKey(x=>x.CourseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyEnrollmentEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.KocUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyLessonCompletionEntity>().HasOne<AcademyEnrollmentEntity>().WithMany().HasForeignKey(x=>x.EnrollmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyLessonCompletionEntity>().HasOne<AcademyLessonEntity>().WithMany().HasForeignKey(x=>x.LessonId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyEnrollmentLessonEntity>().HasOne<AcademyEnrollmentEntity>().WithMany().HasForeignKey(x=>x.EnrollmentId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AcademyEnrollmentLessonEntity>().HasOne<AcademyLessonEntity>().WithMany().HasForeignKey(x=>x.LessonId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<NotificationEventEntity>().HasOne<NotificationEntity>().WithMany().HasForeignKey(x=>x.NotificationId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<NotificationEventEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignPackagePolicyHistoryEntity>().HasOne<CampaignPackagePolicyEntity>().WithMany().HasForeignKey(x=>x.PolicyId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<CampaignPackagePolicyHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BrandPackagePurchaseEntity>().HasOne<BrandProfileEntity>().WithMany().HasForeignKey(x=>x.BrandProfileId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BrandPackagePurchaseEntity>().HasOne<CampaignPackagePolicyEntity>().WithMany().HasForeignKey(x=>x.PackagePolicyId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BrandPackagePurchaseEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x=>x.CampaignId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BrandPackagePurchaseHistoryEntity>().HasOne<BrandPackagePurchaseEntity>().WithMany().HasForeignKey(x=>x.PurchaseId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BrandPackagePurchaseHistoryEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x=>x.ActorUserId).OnDelete(DeleteBehavior.NoAction);

        modelBuilder.Entity<RolePermissionEntity>().HasOne<RoleEntity>().WithMany().HasForeignKey(x => x.RoleId);
        modelBuilder.Entity<RolePermissionEntity>().HasOne<PermissionEntity>().WithMany().HasForeignKey(x => x.PermissionId);
        modelBuilder.Entity<UserPermissionEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<UserPermissionEntity>().HasOne<PermissionEntity>().WithMany().HasForeignKey(x => x.PermissionId);
        modelBuilder.Entity<MenuEntity>().HasOne<MenuEntity>().WithMany().HasForeignKey(x => x.ParentId);
        modelBuilder.Entity<RoleMenuEntity>().HasOne<RoleEntity>().WithMany().HasForeignKey(x => x.RoleId);
        modelBuilder.Entity<RoleMenuEntity>().HasOne<MenuEntity>().WithMany().HasForeignKey(x => x.MenuId);
        modelBuilder.Entity<UserMenuEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<UserMenuEntity>().HasOne<MenuEntity>().WithMany().HasForeignKey(x => x.MenuId);
        modelBuilder.Entity<RoleDashboardConfigEntity>().HasOne<RoleEntity>().WithMany().HasForeignKey(x => x.RoleId);
        modelBuilder.Entity<RoleDashboardConfigEntity>().HasOne<DashboardWidgetEntity>().WithMany().HasForeignKey(x => x.WidgetId);
        modelBuilder.Entity<UserDashboardConfigEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<UserDashboardConfigEntity>().HasOne<DashboardWidgetEntity>().WithMany().HasForeignKey(x => x.WidgetId);

        modelBuilder.Entity<UserDeviceEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<AuthEventEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<AuthEventEntity>().HasOne<UserDeviceEntity>().WithMany().HasForeignKey(x => x.UserDeviceId);
        modelBuilder.Entity<AuthSessionEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<AuthSessionEntity>().HasOne<UserDeviceEntity>().WithMany().HasForeignKey(x => x.UserDeviceId);
        modelBuilder.Entity<PasswordResetTokenEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<PasswordResetTokenEntity>().HasOne<UserDeviceEntity>().WithMany().HasForeignKey(x => x.RequestedByDeviceId);
        modelBuilder.Entity<EmailVerificationTokenEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);
        modelBuilder.Entity<WardEntity>().HasOne<ProvinceEntity>().WithMany().HasForeignKey(x => x.ProvinceId);
        modelBuilder.Entity<BrandProfileEntity>().HasOne<WardEntity>().WithMany().HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<BrandProfileEntity>().HasOne<ProvinceEntity>().WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<KocProfileEntity>().HasOne<WardEntity>().WithMany().HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<KocProfileEntity>().HasOne<ProvinceEntity>().WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentEntity>().HasOne<WardEntity>().WithMany().HasForeignKey(x => x.WardId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShipmentEntity>().HasOne<ProvinceEntity>().WithMany().HasForeignKey(x => x.ProvinceId).OnDelete(DeleteBehavior.NoAction);
    }
}

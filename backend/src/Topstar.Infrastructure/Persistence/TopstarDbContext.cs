using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;
using Topstar.Application.Notifications;

namespace Topstar.Infrastructure.Persistence;

public sealed class TopstarDbContext(DbContextOptions<TopstarDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<BrandProfileEntity> BrandProfiles => Set<BrandProfileEntity>();
    public DbSet<BrandProfileStatusHistoryEntity> BrandProfileStatusHistories => Set<BrandProfileStatusHistoryEntity>();
    public DbSet<KocProfileEntity> KocProfiles => Set<KocProfileEntity>();
    public DbSet<KocProfileStatusHistoryEntity> KocProfileStatusHistories => Set<KocProfileStatusHistoryEntity>();
    public DbSet<KocBodyProfileEntity> KocBodyProfiles => Set<KocBodyProfileEntity>();
    public DbSet<KocChildEntity> KocChildren => Set<KocChildEntity>();
    public DbSet<KocSocialAccountEntity> KocSocialAccounts => Set<KocSocialAccountEntity>();
    public DbSet<KocCategoryEntity> KocCategories => Set<KocCategoryEntity>();
    public DbSet<KocCategoryHistoryEntity> KocCategoryHistories => Set<KocCategoryHistoryEntity>();
    public DbSet<KocProfileCategoryEntity> KocProfileCategories => Set<KocProfileCategoryEntity>();
    public DbSet<KocSocialAccountCategoryEntity> KocSocialAccountCategories => Set<KocSocialAccountCategoryEntity>();
    public DbSet<KocExperienceEntity> KocExperiences => Set<KocExperienceEntity>();
    public DbSet<KocProductionCapabilityEntity> KocProductionCapabilities => Set<KocProductionCapabilityEntity>();
    public DbSet<KocJobPreferenceEntity> KocJobPreferences => Set<KocJobPreferenceEntity>();
    public DbSet<KocPortfolioEntity> KocPortfolios => Set<KocPortfolioEntity>();
    public DbSet<KocMediaAssetEntity> KocMediaAssets => Set<KocMediaAssetEntity>();
    public DbSet<SocialPlatformConnectionEntity> SocialPlatformConnections => Set<SocialPlatformConnectionEntity>();
    public DbSet<SocialSyncJobEntity> SocialSyncJobs => Set<SocialSyncJobEntity>();
    public DbSet<SocialProfileMetricSnapshotEntity> SocialProfileMetricSnapshots => Set<SocialProfileMetricSnapshotEntity>();
    public DbSet<CampaignEntity> Campaigns => Set<CampaignEntity>();
    public DbSet<CampaignStatusHistoryEntity> CampaignStatusHistories => Set<CampaignStatusHistoryEntity>();
    public DbSet<CampaignDeliverableEntity> CampaignDeliverables => Set<CampaignDeliverableEntity>();
    public DbSet<CampaignDeliverableHistoryEntity> CampaignDeliverableHistories => Set<CampaignDeliverableHistoryEntity>();
    public DbSet<CampaignReviewFeedbackItemEntity> CampaignReviewFeedbackItems => Set<CampaignReviewFeedbackItemEntity>();
    public DbSet<CampaignTargetingEntity> CampaignTargetings => Set<CampaignTargetingEntity>();
    public DbSet<CampaignTargetProvinceEntity> CampaignTargetProvinces => Set<CampaignTargetProvinceEntity>();
    public DbSet<CampaignTargetCategoryEntity> CampaignTargetCategories => Set<CampaignTargetCategoryEntity>();
    public DbSet<CampaignTargetingHistoryEntity> CampaignTargetingHistories => Set<CampaignTargetingHistoryEntity>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<KocCampaignFavoriteEntity> KocCampaignFavorites => Set<KocCampaignFavoriteEntity>();
    public DbSet<ApplicationStatusHistoryEntity> ApplicationStatusHistories => Set<ApplicationStatusHistoryEntity>();
    public DbSet<ShortlistEntity> Shortlists => Set<ShortlistEntity>();
    public DbSet<ShortlistItemEntity> ShortlistItems => Set<ShortlistItemEntity>();
    public DbSet<ParticipationEntity> Participations => Set<ParticipationEntity>();
    public DbSet<ParticipationStatusHistoryEntity> ParticipationStatusHistories => Set<ParticipationStatusHistoryEntity>();
    public DbSet<ScheduleEntity> Schedules => Set<ScheduleEntity>();
    public DbSet<ScheduleStatusHistoryEntity> ScheduleStatusHistories => Set<ScheduleStatusHistoryEntity>();
    public DbSet<ScheduleReminderEventEntity> ScheduleReminderEvents => Set<ScheduleReminderEventEntity>();
    public DbSet<ShipmentEntity> Shipments => Set<ShipmentEntity>();
    public DbSet<ShipmentEvidenceEntity> ShipmentEvidence => Set<ShipmentEvidenceEntity>();
    public DbSet<ShipmentTrackingEventEntity> ShipmentTrackingEvents => Set<ShipmentTrackingEventEntity>();
    public DbSet<ShipmentStatusHistoryEntity> ShipmentStatusHistories => Set<ShipmentStatusHistoryEntity>();
    public DbSet<SubmissionEntity> Submissions => Set<SubmissionEntity>();
    public DbSet<SubmissionHistoryEntity> SubmissionHistories => Set<SubmissionHistoryEntity>();
    public DbSet<SubmissionPostEntity> SubmissionPosts => Set<SubmissionPostEntity>();
    public DbSet<PostMetricSnapshotEntity> PostMetricSnapshots => Set<PostMetricSnapshotEntity>();
    public DbSet<CampaignKocDailyMetricEntity> CampaignKocDailyMetrics => Set<CampaignKocDailyMetricEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();
    public DbSet<ReviewReplyEntity> ReviewReplies => Set<ReviewReplyEntity>();
    public DbSet<ReviewAppealEntity> ReviewAppeals => Set<ReviewAppealEntity>();
    public DbSet<ReviewAppealHistoryEntity> ReviewAppealHistories => Set<ReviewAppealHistoryEntity>();
    public DbSet<ReviewStatusHistoryEntity> ReviewStatusHistories => Set<ReviewStatusHistoryEntity>();
    public DbSet<ReviewRevisionHistoryEntity> ReviewRevisionHistories => Set<ReviewRevisionHistoryEntity>();
    public DbSet<ReviewAppealSupplementEntity> ReviewAppealSupplements => Set<ReviewAppealSupplementEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
    public DbSet<NotificationTemplateEntity> NotificationTemplates => Set<NotificationTemplateEntity>();
    public DbSet<NotificationTemplateHistoryEntity> NotificationTemplateHistories => Set<NotificationTemplateHistoryEntity>();
    public DbSet<UserViolationEntity> UserViolations => Set<UserViolationEntity>();
    public DbSet<UserViolationHistoryEntity> UserViolationHistories => Set<UserViolationHistoryEntity>();
    public DbSet<UserAccountStatusHistoryEntity> UserAccountStatusHistories => Set<UserAccountStatusHistoryEntity>();
    public DbSet<UserAccountAdminNoteEntity> UserAccountAdminNotes => Set<UserAccountAdminNoteEntity>();
    public DbSet<AcademyCourseEntity> AcademyCourses => Set<AcademyCourseEntity>();
    public DbSet<AcademyCourseHistoryEntity> AcademyCourseHistories => Set<AcademyCourseHistoryEntity>();
    public DbSet<AcademyLessonEntity> AcademyLessons => Set<AcademyLessonEntity>();
    public DbSet<AcademyLessonHistoryEntity> AcademyLessonHistories => Set<AcademyLessonHistoryEntity>();
    public DbSet<AcademyEnrollmentEntity> AcademyEnrollments => Set<AcademyEnrollmentEntity>();
    public DbSet<AcademyLessonCompletionEntity> AcademyLessonCompletions => Set<AcademyLessonCompletionEntity>();
    public DbSet<AcademyEnrollmentLessonEntity> AcademyEnrollmentLessons => Set<AcademyEnrollmentLessonEntity>();
    public DbSet<NotificationEventEntity> NotificationEvents => Set<NotificationEventEntity>();
    public DbSet<OperationalEscalationEntity> OperationalEscalations => Set<OperationalEscalationEntity>();
    public DbSet<OperationalEscalationHistoryEntity> OperationalEscalationHistories => Set<OperationalEscalationHistoryEntity>();
    public DbSet<OperationalScanRunEntity> OperationalScanRuns => Set<OperationalScanRunEntity>();
    public DbSet<OperationalSlaPolicyEntity> OperationalSlaPolicies => Set<OperationalSlaPolicyEntity>();
    public DbSet<OperationalSlaPolicyHistoryEntity> OperationalSlaPolicyHistories => Set<OperationalSlaPolicyHistoryEntity>();
    public DbSet<OperationalRuntimePolicyEntity> OperationalRuntimePolicies => Set<OperationalRuntimePolicyEntity>();
    public DbSet<OperationalRuntimePolicyHistoryEntity> OperationalRuntimePolicyHistories => Set<OperationalRuntimePolicyHistoryEntity>();
    public DbSet<OperationalCatalogItemEntity> OperationalCatalogItems => Set<OperationalCatalogItemEntity>();
    public DbSet<OperationalCatalogHistoryEntity> OperationalCatalogHistories => Set<OperationalCatalogHistoryEntity>();
    public DbSet<AdminProfileEntity> AdminProfiles => Set<AdminProfileEntity>();
    public DbSet<PermissionEntity> Permissions => Set<PermissionEntity>();
    public DbSet<RolePermissionEntity> RolePermissions => Set<RolePermissionEntity>();
    public DbSet<UserPermissionEntity> UserPermissions => Set<UserPermissionEntity>();
    public DbSet<MenuEntity> Menus => Set<MenuEntity>();
    public DbSet<RoleMenuEntity> RoleMenus => Set<RoleMenuEntity>();
    public DbSet<UserMenuEntity> UserMenus => Set<UserMenuEntity>();
    public DbSet<DashboardWidgetEntity> DashboardWidgets => Set<DashboardWidgetEntity>();
    public DbSet<RoleDashboardConfigEntity> RoleDashboardConfigs => Set<RoleDashboardConfigEntity>();
    public DbSet<UserDashboardConfigEntity> UserDashboardConfigs => Set<UserDashboardConfigEntity>();
    public DbSet<UserDeviceEntity> UserDevices => Set<UserDeviceEntity>();
    public DbSet<AuthEventEntity> AuthEvents => Set<AuthEventEntity>();
    public DbSet<AuthSessionEntity> AuthSessions => Set<AuthSessionEntity>();
    public DbSet<PasswordResetTokenEntity> PasswordResetTokens => Set<PasswordResetTokenEntity>();
    public DbSet<EmailVerificationTokenEntity> EmailVerificationTokens => Set<EmailVerificationTokenEntity>();
    public DbSet<ProvinceEntity> Provinces => Set<ProvinceEntity>();
    public DbSet<WardEntity> Wards => Set<WardEntity>();
    public DbSet<SystemEmailAccountEntity> SystemEmailAccounts => Set<SystemEmailAccountEntity>();
    public DbSet<SocialLoginConfigurationEntity> SocialLoginConfigurations => Set<SocialLoginConfigurationEntity>();
    public DbSet<SystemShippingConfigEntity> SystemShippingConfigs => Set<SystemShippingConfigEntity>();
    public DbSet<SystemWebsiteConfigEntity> SystemWebsiteConfigs => Set<SystemWebsiteConfigEntity>();
    public DbSet<HomepageContentHistoryEntity> HomepageContentHistories => Set<HomepageContentHistoryEntity>();
    public DbSet<AcademyContentHistoryEntity> AcademyContentHistories => Set<AcademyContentHistoryEntity>();
    public DbSet<PublicPageSeoConfigEntity> PublicPageSeoConfigs => Set<PublicPageSeoConfigEntity>();
    public DbSet<CampaignPackagePolicyEntity> CampaignPackagePolicies => Set<CampaignPackagePolicyEntity>();
    public DbSet<CampaignPackagePolicyHistoryEntity> CampaignPackagePolicyHistories => Set<CampaignPackagePolicyHistoryEntity>();
    public DbSet<BrandPackagePurchaseEntity> BrandPackagePurchases => Set<BrandPackagePurchaseEntity>();
    public DbSet<BrandPackagePurchaseHistoryEntity> BrandPackagePurchaseHistories => Set<BrandPackagePurchaseHistoryEntity>();
    public DbSet<PaymentMethodConfigurationEntity> PaymentMethodConfigurations => Set<PaymentMethodConfigurationEntity>();
    public DbSet<VnPayCallbackLogEntity> VnPayCallbackLogs => Set<VnPayCallbackLogEntity>();
    public DbSet<WalletTransactionEntity> WalletTransactions => Set<WalletTransactionEntity>();
    public DbSet<KocPayoutAccountEntity> KocPayoutAccounts => Set<KocPayoutAccountEntity>();
    public DbSet<PayoutRequestEntity> PayoutRequests => Set<PayoutRequestEntity>();
    public DbSet<WalletTransactionStatusHistoryEntity> WalletTransactionStatusHistories => Set<WalletTransactionStatusHistoryEntity>();
    public DbSet<PayoutRequestStatusHistoryEntity> PayoutRequestStatusHistories => Set<PayoutRequestStatusHistoryEntity>();
    public DbSet<PayoutPolicyEntity> PayoutPolicies => Set<PayoutPolicyEntity>();
    public DbSet<PayoutPolicyHistoryEntity> PayoutPolicyHistories => Set<PayoutPolicyHistoryEntity>();
    public DbSet<PayoutOtpChallengeEntity> PayoutOtpChallenges => Set<PayoutOtpChallengeEntity>();
    public DbSet<CampaignBillingInvoiceEntity> CampaignBillingInvoices => Set<CampaignBillingInvoiceEntity>();
    public DbSet<CampaignBillingInvoiceHistoryEntity> CampaignBillingInvoiceHistories => Set<CampaignBillingInvoiceHistoryEntity>();
    public DbSet<CampaignFeePolicyEntity> CampaignFeePolicies => Set<CampaignFeePolicyEntity>();
    public DbSet<CampaignFeePolicyHistoryEntity> CampaignFeePolicyHistories => Set<CampaignFeePolicyHistoryEntity>();
    public DbSet<CampaignFundingEntity> CampaignFundings => Set<CampaignFundingEntity>();
    public DbSet<CampaignFundingAllocationEntity> CampaignFundingAllocations => Set<CampaignFundingAllocationEntity>();
    public DbSet<CampaignFundLedgerEntity> CampaignFundLedgers => Set<CampaignFundLedgerEntity>();
    public DbSet<CampaignFundingEvidenceEntity> CampaignFundingEvidences => Set<CampaignFundingEvidenceEntity>();
    public DbSet<CampaignFundingEvidenceReviewEntity> CampaignFundingEvidenceReviews => Set<CampaignFundingEvidenceReviewEntity>();
    public DbSet<CampaignFundingApprovalEntity> CampaignFundingApprovals => Set<CampaignFundingApprovalEntity>();
    public DbSet<DisputeCaseEntity> DisputeCases => Set<DisputeCaseEntity>();
    public DbSet<DisputeCaseStatusHistoryEntity> DisputeCaseStatusHistories => Set<DisputeCaseStatusHistoryEntity>();
    public DbSet<DisputeMessageEntity> DisputeMessages => Set<DisputeMessageEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        TopstarModelBuilder.Configure(modelBuilder);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess){Topstar.Infrastructure.Finance.CampaignCreationPayment.ApplyAsync(this,CancellationToken.None).GetAwaiter().GetResult();Topstar.Infrastructure.Finance.KocIncomeTax.ApplyAsync(this,CancellationToken.None).GetAwaiter().GetResult();ApplyAuditSnapshots();ApplyNotificationTemplates();return base.SaveChanges(acceptAllChangesOnSuccess);}
    public override async Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess,CancellationToken cancellationToken=default){await Topstar.Infrastructure.Finance.CampaignCreationPayment.ApplyAsync(this,cancellationToken);await Topstar.Infrastructure.Finance.KocIncomeTax.ApplyAsync(this,cancellationToken);await ApplyAuditSnapshotsAsync(cancellationToken);await ApplyNotificationTemplatesAsync(cancellationToken);return await base.SaveChangesAsync(acceptAllChangesOnSuccess,cancellationToken);}
    private void ApplyAuditSnapshots()
    {
        var pending=ChangeTracker.Entries<AuditLogEntity>().Where(x=>x.State==EntityState.Added).Select(x=>x.Entity).ToList();if(pending.Count==0)return;
        var ids=pending.Where(x=>x.ActorUserId.HasValue).Select(x=>x.ActorUserId!.Value).Distinct().ToArray();
        var actors=Users.AsNoTracking().Where(x=>ids.Contains(x.Id)).Select(x=>new{x.Id,x.Email}).ToList();
        var roles=UserRoles.AsNoTracking().Where(x=>ids.Contains(x.UserId)).Join(Roles,x=>x.RoleId,x=>x.Id,(ur,r)=>new{ur.UserId,r.Code}).ToList();
        ApplyAudit(pending,actors.ToDictionary(x=>x.Id,x=>x.Email),roles.GroupBy(x=>x.UserId).ToDictionary(x=>x.Key,x=>string.Join(",",x.Select(r=>r.Code).OrderBy(c=>c))));
    }
    private async Task ApplyAuditSnapshotsAsync(CancellationToken ct)
    {
        var pending=ChangeTracker.Entries<AuditLogEntity>().Where(x=>x.State==EntityState.Added).Select(x=>x.Entity).ToList();if(pending.Count==0)return;
        var ids=pending.Where(x=>x.ActorUserId.HasValue).Select(x=>x.ActorUserId!.Value).Distinct().ToArray();
        var actors=await Users.AsNoTracking().Where(x=>ids.Contains(x.Id)).Select(x=>new{x.Id,x.Email}).ToListAsync(ct);
        var roles=await UserRoles.AsNoTracking().Where(x=>ids.Contains(x.UserId)).Join(Roles,x=>x.RoleId,x=>x.Id,(ur,r)=>new{ur.UserId,r.Code}).ToListAsync(ct);
        ApplyAudit(pending,actors.ToDictionary(x=>x.Id,x=>x.Email),roles.GroupBy(x=>x.UserId).ToDictionary(x=>x.Key,x=>string.Join(",",x.Select(r=>r.Code).OrderBy(c=>c))));
    }
    private static void ApplyAudit(IEnumerable<AuditLogEntity> pending,IReadOnlyDictionary<Guid,string> emails,IReadOnlyDictionary<Guid,string> roles)
    {
        foreach(var item in pending){if(!item.ActorUserId.HasValue){item.ActorEmailSnapshot="System";item.ActorRoleSnapshot="SYSTEM";continue;}emails.TryGetValue(item.ActorUserId.Value,out var email);roles.TryGetValue(item.ActorUserId.Value,out var role);item.ActorEmailSnapshot=email??"Unknown";item.ActorRoleSnapshot=role??"UNKNOWN";}
    }
    private void ApplyNotificationTemplates()
    {
        var pending=ChangeTracker.Entries<NotificationEntity>().Where(x=>x.State==EntityState.Added).Select(x=>x.Entity).ToList();if(pending.Count==0)return;
        var types=pending.Select(x=>x.Type).Distinct().ToArray();var roles=pending.Select(x=>x.AudienceRole).Where(x=>x!=null).Distinct().ToArray();
        var templates=NotificationTemplates.AsNoTracking().Where(x=>x.IsActive&&types.Contains(x.Type)&&roles.Contains(x.AudienceRole)).ToList();Apply(pending,templates);
    }
    private async Task ApplyNotificationTemplatesAsync(CancellationToken ct)
    {
        var pending=ChangeTracker.Entries<NotificationEntity>().Where(x=>x.State==EntityState.Added).Select(x=>x.Entity).ToList();if(pending.Count==0)return;
        var types=pending.Select(x=>x.Type).Distinct().ToArray();var roles=pending.Select(x=>x.AudienceRole).Where(x=>x!=null).Distinct().ToArray();
        var templates=await NotificationTemplates.AsNoTracking().Where(x=>x.IsActive&&types.Contains(x.Type)&&roles.Contains(x.AudienceRole)).ToListAsync(ct);Apply(pending,templates);
    }
    private static void Apply(IEnumerable<NotificationEntity> pending,IReadOnlyCollection<NotificationTemplateEntity> templates)
    {
        foreach(var notification in pending){var template=templates.FirstOrDefault(x=>x.Type==notification.Type&&x.AudienceRole==notification.AudienceRole);if(template is null)continue;var originalTitle=notification.Title;var originalBody=notification.Body;notification.Title=NotificationTemplateRules.Render(template.TitleTemplate,originalTitle,originalBody,200);notification.Body=NotificationTemplateRules.Render(template.BodyTemplate,originalTitle,originalBody,1000);}
    }
}

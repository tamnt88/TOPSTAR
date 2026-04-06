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
        modelBuilder.Entity<BrandProfileEntity>(e => { e.ToTable("BrandProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); });
        modelBuilder.Entity<KocProfileEntity>(e => { e.ToTable("KocProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.UserId).IsUnique(); });
        modelBuilder.Entity<KocBodyProfileEntity>(e => { e.ToTable("KocBodyProfiles"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); });
        modelBuilder.Entity<KocChildEntity>(e => { e.ToTable("KocChildren"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<KocSocialAccountEntity>(e => { e.ToTable("KocSocialAccounts"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<KocCategoryEntity>(e => { e.ToTable("KocCategories"); e.HasKey(x => x.Id); e.HasIndex(x => x.Code).IsUnique(); });
        modelBuilder.Entity<KocProfileCategoryEntity>(e => { e.ToTable("KocProfileCategories"); e.HasKey(x => new { x.KocProfileId, x.CategoryId }); });
        modelBuilder.Entity<KocExperienceEntity>(e => { e.ToTable("KocExperiences"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); });
        modelBuilder.Entity<KocProductionCapabilityEntity>(e => { e.ToTable("KocProductionCapabilities"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); });
        modelBuilder.Entity<KocJobPreferenceEntity>(e => { e.ToTable("KocJobPreferences"); e.HasKey(x => x.Id); e.HasIndex(x => x.KocProfileId).IsUnique(); });
        modelBuilder.Entity<KocPortfolioEntity>(e => { e.ToTable("KocPortfolios"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<CampaignEntity>(e => { e.ToTable("Campaigns"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<CampaignDeliverableEntity>(e => { e.ToTable("CampaignDeliverables"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<ApplicationEntity>(e => { e.ToTable("Applications"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.KocProfileId }).IsUnique(); });
        modelBuilder.Entity<ShortlistEntity>(e => { e.ToTable("Shortlists"); e.HasKey(x => x.Id); e.HasIndex(x => x.CampaignId).IsUnique(); });
        modelBuilder.Entity<ShortlistItemEntity>(e => { e.ToTable("ShortlistItems"); e.HasKey(x => new { x.ShortlistId, x.ApplicationId }); });
        modelBuilder.Entity<ParticipationEntity>(e => { e.ToTable("Participations"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.KocProfileId }).IsUnique(); });
        modelBuilder.Entity<ScheduleEntity>(e => { e.ToTable("Schedules"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<ShipmentEntity>(e => { e.ToTable("Shipments"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SubmissionEntity>(e => { e.ToTable("Submissions"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<SubmissionPostEntity>(e => { e.ToTable("SubmissionPosts"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<PostMetricSnapshotEntity>(e => { e.ToTable("PostMetricSnapshots"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<CampaignKocDailyMetricEntity>(e => { e.ToTable("CampaignKocDailyMetrics"); e.HasKey(x => x.Id); e.HasIndex(x => new { x.CampaignId, x.KocProfileId, x.MetricDate }).IsUnique(); });
        modelBuilder.Entity<ReviewEntity>(e => { e.ToTable("Reviews"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<AuditLogEntity>(e => { e.ToTable("AuditLogs"); e.HasKey(x => x.Id); });
        modelBuilder.Entity<NotificationEntity>(e => { e.ToTable("Notifications"); e.HasKey(x => x.Id); });
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
        modelBuilder.Entity<EmailVerificationTokenEntity>(e => { e.ToTable("EmailVerificationTokens"); e.HasKey(x => x.Id); });
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
    }

    private static void ConfigureRelationships(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserRoleEntity>().HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
        modelBuilder.Entity<UserRoleEntity>().HasOne(x => x.Role).WithMany().HasForeignKey(x => x.RoleId);
        modelBuilder.Entity<BrandProfileEntity>().HasOne(x => x.User).WithOne(x => x.BrandProfile).HasForeignKey<BrandProfileEntity>(x => x.UserId);
        modelBuilder.Entity<KocProfileEntity>().HasOne(x => x.User).WithOne(x => x.KocProfile).HasForeignKey<KocProfileEntity>(x => x.UserId);
        modelBuilder.Entity<AdminProfileEntity>().HasOne(x => x.User).WithOne(x => x.AdminProfile).HasForeignKey<AdminProfileEntity>(x => x.UserId);

        modelBuilder.Entity<KocBodyProfileEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocChildEntity>().HasOne<KocBodyProfileEntity>().WithMany().HasForeignKey(x => x.KocBodyProfileId);
        modelBuilder.Entity<KocSocialAccountEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocProfileCategoryEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocProfileCategoryEntity>().HasOne<KocCategoryEntity>().WithMany().HasForeignKey(x => x.CategoryId);
        modelBuilder.Entity<KocExperienceEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocProductionCapabilityEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocJobPreferenceEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<KocPortfolioEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);

        modelBuilder.Entity<CampaignEntity>().HasOne<BrandProfileEntity>().WithMany().HasForeignKey(x => x.BrandProfileId);
        modelBuilder.Entity<CampaignDeliverableEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ApplicationEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ApplicationEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<ShortlistEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ShortlistEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ShortlistItemEntity>().HasOne<ShortlistEntity>().WithMany().HasForeignKey(x => x.ShortlistId);
        modelBuilder.Entity<ShortlistItemEntity>().HasOne<ApplicationEntity>().WithMany().HasForeignKey(x => x.ApplicationId);
        modelBuilder.Entity<ParticipationEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ParticipationEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<ScheduleEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId);
        modelBuilder.Entity<ShipmentEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId);
        modelBuilder.Entity<SubmissionEntity>().HasOne<ParticipationEntity>().WithMany().HasForeignKey(x => x.ParticipationId);
        modelBuilder.Entity<SubmissionEntity>().HasOne<CampaignDeliverableEntity>().WithMany().HasForeignKey(x => x.DeliverableId);
        modelBuilder.Entity<SubmissionPostEntity>().HasOne<SubmissionEntity>().WithMany().HasForeignKey(x => x.SubmissionId);
        modelBuilder.Entity<PostMetricSnapshotEntity>().HasOne<SubmissionPostEntity>().WithMany().HasForeignKey(x => x.SubmissionPostId);
        modelBuilder.Entity<CampaignKocDailyMetricEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<CampaignKocDailyMetricEntity>().HasOne<KocProfileEntity>().WithMany().HasForeignKey(x => x.KocProfileId);
        modelBuilder.Entity<ReviewEntity>().HasOne<CampaignEntity>().WithMany().HasForeignKey(x => x.CampaignId);
        modelBuilder.Entity<ReviewEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ReviewerUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<ReviewEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.RevieweeUserId).OnDelete(DeleteBehavior.NoAction);
        modelBuilder.Entity<AuditLogEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.ActorUserId);
        modelBuilder.Entity<NotificationEntity>().HasOne<UserEntity>().WithMany().HasForeignKey(x => x.UserId);

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

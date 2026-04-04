using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;

namespace Topstar.Infrastructure.Persistence;

public sealed class TopstarDbContext(DbContextOptions<TopstarDbContext> options) : DbContext(options)
{
    public DbSet<UserEntity> Users => Set<UserEntity>();
    public DbSet<RoleEntity> Roles => Set<RoleEntity>();
    public DbSet<UserRoleEntity> UserRoles => Set<UserRoleEntity>();
    public DbSet<BrandProfileEntity> BrandProfiles => Set<BrandProfileEntity>();
    public DbSet<KocProfileEntity> KocProfiles => Set<KocProfileEntity>();
    public DbSet<KocBodyProfileEntity> KocBodyProfiles => Set<KocBodyProfileEntity>();
    public DbSet<KocChildEntity> KocChildren => Set<KocChildEntity>();
    public DbSet<KocSocialAccountEntity> KocSocialAccounts => Set<KocSocialAccountEntity>();
    public DbSet<KocCategoryEntity> KocCategories => Set<KocCategoryEntity>();
    public DbSet<KocProfileCategoryEntity> KocProfileCategories => Set<KocProfileCategoryEntity>();
    public DbSet<KocExperienceEntity> KocExperiences => Set<KocExperienceEntity>();
    public DbSet<KocProductionCapabilityEntity> KocProductionCapabilities => Set<KocProductionCapabilityEntity>();
    public DbSet<KocJobPreferenceEntity> KocJobPreferences => Set<KocJobPreferenceEntity>();
    public DbSet<KocPortfolioEntity> KocPortfolios => Set<KocPortfolioEntity>();
    public DbSet<CampaignEntity> Campaigns => Set<CampaignEntity>();
    public DbSet<CampaignDeliverableEntity> CampaignDeliverables => Set<CampaignDeliverableEntity>();
    public DbSet<ApplicationEntity> Applications => Set<ApplicationEntity>();
    public DbSet<ShortlistEntity> Shortlists => Set<ShortlistEntity>();
    public DbSet<ShortlistItemEntity> ShortlistItems => Set<ShortlistItemEntity>();
    public DbSet<ParticipationEntity> Participations => Set<ParticipationEntity>();
    public DbSet<ScheduleEntity> Schedules => Set<ScheduleEntity>();
    public DbSet<ShipmentEntity> Shipments => Set<ShipmentEntity>();
    public DbSet<SubmissionEntity> Submissions => Set<SubmissionEntity>();
    public DbSet<SubmissionPostEntity> SubmissionPosts => Set<SubmissionPostEntity>();
    public DbSet<PostMetricSnapshotEntity> PostMetricSnapshots => Set<PostMetricSnapshotEntity>();
    public DbSet<CampaignKocDailyMetricEntity> CampaignKocDailyMetrics => Set<CampaignKocDailyMetricEntity>();
    public DbSet<ReviewEntity> Reviews => Set<ReviewEntity>();
    public DbSet<AuditLogEntity> AuditLogs => Set<AuditLogEntity>();
    public DbSet<NotificationEntity> Notifications => Set<NotificationEntity>();
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        TopstarModelBuilder.Configure(modelBuilder);
    }
}

using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Topstar.Application;
using Topstar.Application.Menus;
using Topstar.Application.Navigation;
using Topstar.Application.KocRegistration;
using Topstar.Application.Dashboard;
using Topstar.Application.SocialSync;
using Topstar.Application.SystemSettings;
using Topstar.Application.Website;
using Topstar.Infrastructure.Auth;
using Topstar.Infrastructure.Campaigns;
using Topstar.Application.Campaigns;
using Topstar.Infrastructure.Dashboard;
using Topstar.Infrastructure.Deliverables;
using Topstar.Application.Deliverables;
using Topstar.Infrastructure.Koc;
using Topstar.Infrastructure.Menus;
using Topstar.Infrastructure.Matching;
using Topstar.Application.Matching;
using Topstar.Infrastructure.Navigation;
using Topstar.Infrastructure.Persistence;
using Topstar.Application.Settings;
using Topstar.Infrastructure.Settings;
using Topstar.Infrastructure.Profiles;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Reviews;
using Topstar.Application.Reviews;
using Topstar.Infrastructure.Finance;
using Topstar.Application.Finance;
using Topstar.Infrastructure.SocialSync;
using Topstar.Infrastructure.SystemSettings;
using Topstar.Infrastructure.Website;
using Topstar.Application.Shipments;
using Topstar.Infrastructure.Shipments;
using Topstar.Application.Schedules;
using Topstar.Infrastructure.Schedules;
using Topstar.Application.Operations;
using Topstar.Infrastructure.Operations;
using Topstar.Application.Disputes;
using Topstar.Infrastructure.Disputes;
using Topstar.Application.Notifications;
using Topstar.Infrastructure.Notifications;
using Topstar.Application.Collaborations;
using Topstar.Infrastructure.Collaborations;
using Topstar.Application.Billing;
using Topstar.Application.Reporting;
using Topstar.Infrastructure.Reporting;
using Topstar.Application.Academy;
using Topstar.Application.Auditing;
using Topstar.Infrastructure.Academy;
using Topstar.Infrastructure.Auditing;

namespace Topstar.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddTopstarInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TopstarDb")
            ?? "Server=(localdb)\MSSQLLocalDB;Database=TopstarDb;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<TopstarDbContext>(options => options.UseSqlServer(connectionString));
        services.AddDataProtection();
        services.AddMemoryCache();
        services.AddHttpClient();
        services.AddScoped<IAuthRepository, EfAuthRepository>();
        services.AddScoped<IAccountActivationService, AccountActivationService>();
        services.AddScoped<IKocRegistrationService, KocRegistrationService>();
        services.AddScoped<IKocCategoryAdminService, KocCategoryAdminService>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IMenuAdminService, MenuAdminService>();
        services.AddScoped<ISystemSettingsAdminService, SystemSettingsAdminService>();
        services.AddScoped<IWebsiteContentService, WebsiteContentService>();
        services.AddScoped<IHomepageContentService, HomepageContentService>();
        services.AddScoped<IAcademyLandingService, AcademyLandingService>();
        services.AddScoped<IDashboardDataService, DashboardDataService>();
        services.AddScoped<IProfileAccessService, ProfileAccessService>();
        services.AddScoped<IProfileReviewService, ProfileReviewService>();
        services.AddScoped<IUserDirectoryService, UserDirectoryService>();
        services.AddScoped<IUserAccountAdminService, UserAccountAdminService>();
        services.AddScoped<IAcademyService, AcademyService>();
        services.AddScoped<IAuditLogReader, AuditLogReader>();
        services.AddScoped<IAuditLogExportRecorder, AuditLogExportRecorder>();
        services.AddScoped<IUserViolationService, UserViolationService>();
        services.AddScoped<ICampaignService, CampaignService>();
        services.AddScoped<ICampaignWorkspaceService, CampaignWorkspaceService>();
        services.AddScoped<ICampaignApplicationService, CampaignApplicationService>();
        services.AddScoped<IMatchingService, MatchingService>();
        services.AddScoped<IPackagePolicyAdminService, PackagePolicyAdminService>();
        services.AddScoped<IBrandPackagePurchaseService, BrandPackagePurchaseService>();
        services.AddScoped<IDeliverableWorkflowService, DeliverableWorkflowService>();
        services.AddScoped<ICampaignReviewService, CampaignReviewService>();
        services.AddScoped<IShipmentWorkflowService, ShipmentWorkflowService>();
        services.AddScoped<IScheduleWorkflowService, ScheduleWorkflowService>();
        if (configuration.GetValue<bool?>("BackgroundJobs:Enabled") ?? true) services.AddHostedService<ScheduleReminderBackgroundService>();
        services.AddScoped<IOperationsAdminService, OperationsAdminService>();
        services.AddScoped<IDisputeService, DisputeService>();
        services.AddScoped<INotificationInboxService, NotificationInboxService>();
        services.AddScoped<INotificationTemplateAdminService, NotificationTemplateAdminService>();
        services.AddScoped<IOperationalSetupService, OperationalSetupService>();
        services.AddScoped<ISampleDataCleanupService, SampleDataCleanupService>();
        services.AddScoped<ICollaborationHistoryService, CollaborationHistoryService>();
        services.AddScoped<IBillingService, BillingService>();
        services.AddScoped<ICampaignFundingService, CampaignFundingService>();
        if (configuration.GetValue<bool?>("BackgroundJobs:Enabled") ?? true) services.AddHostedService<CampaignFinanceBackgroundService>();
        services.AddScoped<IReportingService, ReportingService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<ISocialSyncOrchestrator, SocialSyncOrchestrator>();
        services.AddScoped<ISocialTokenCipher, SocialTokenCipher>();
        services.AddScoped<IYouTubeMetricsClient, YouTubeMetricsClient>();
        services.AddScoped<ITikTokMetricsClient, TikTokMetricsClient>();
        if (configuration.GetValue<bool?>("BackgroundJobs:Enabled") ?? true) services.AddHostedService<SocialSyncBackgroundService>();

        return services;
    }
}


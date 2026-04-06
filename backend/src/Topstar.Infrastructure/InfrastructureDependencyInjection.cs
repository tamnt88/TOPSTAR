using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Topstar.Application;
using Topstar.Application.Menus;
using Topstar.Application.Navigation;
using Topstar.Application.KocRegistration;
using Topstar.Application.Dashboard;
using Topstar.Application.SystemSettings;
using Topstar.Application.Website;
using Topstar.Infrastructure.Auth;
using Topstar.Infrastructure.Dashboard;
using Topstar.Infrastructure.Koc;
using Topstar.Infrastructure.Menus;
using Topstar.Infrastructure.Navigation;
using Topstar.Infrastructure.Persistence;
using Topstar.Infrastructure.SystemSettings;
using Topstar.Infrastructure.Website;

namespace Topstar.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddTopstarInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TopstarDb")
            ?? "Server=SHENLUNG\\SQLSERVER2022;Database=TopstarDb;User Id=sa;Password=123456;TrustServerCertificate=True";

        services.AddDbContext<TopstarDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAuthRepository, EfAuthRepository>();
        services.AddScoped<IAccountActivationService, AccountActivationService>();
        services.AddScoped<IKocRegistrationService, KocRegistrationService>();
        services.AddScoped<INavigationService, NavigationService>();
        services.AddScoped<IMenuAdminService, MenuAdminService>();
        services.AddScoped<ISystemSettingsAdminService, SystemSettingsAdminService>();
        services.AddScoped<IWebsiteContentService, WebsiteContentService>();
        services.AddScoped<IDashboardDataService, DashboardDataService>();

        return services;
    }
}


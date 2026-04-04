using Microsoft.Extensions.DependencyInjection;

namespace Topstar.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddTopstarApplication(this IServiceCollection services)
    {
        services.AddScoped<IPlatformInfoService, PlatformInfoService>();
        services.AddScoped<IAuthService, AuthService>();
        return services;
    }
}

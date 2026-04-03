using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Topstar.Application;

namespace Topstar.Infrastructure;

public sealed class TopstarDbContext(DbContextOptions<TopstarDbContext> options) : DbContext(options)
{
}

public static class DependencyInjection
{
    public static IServiceCollection AddTopstarInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TopstarDb")
            ?? "Server=localhost;Database=TopstarDb;Trusted_Connection=True;TrustServerCertificate=True";

        services.AddDbContext<TopstarDbContext>(options =>
            options.UseSqlServer(connectionString));

        services.AddScoped<IPlatformInfoService, PlatformInfoService>();
        return services;
    }
}

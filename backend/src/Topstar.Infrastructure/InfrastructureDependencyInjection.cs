using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Topstar.Application;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure;

public static class InfrastructureDependencyInjection
{
    public static IServiceCollection AddTopstarInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("TopstarDb")
            ?? "Server=SHENLUNG\\SQLSERVER2022;Database=TopstarDb;User Id=sa;Password=123456;TrustServerCertificate=True";

        services.AddDbContext<TopstarDbContext>(options => options.UseSqlServer(connectionString));
        services.AddScoped<IAuthRepository, EfAuthRepository>();

        return services;
    }
}


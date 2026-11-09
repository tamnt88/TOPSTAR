using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Topstar.Application.SocialSync;

namespace Topstar.Infrastructure.SocialSync;

internal sealed class SocialSyncBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<SocialSyncBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var orchestrator = scope.ServiceProvider.GetRequiredService<ISocialSyncOrchestrator>();
                await orchestrator.ProcessPendingJobsAsync(20, stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Lỗi SocialSyncBackgroundService");
            }

            await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
        }
    }
}

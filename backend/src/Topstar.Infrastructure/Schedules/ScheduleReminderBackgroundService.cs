using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Topstar.Application.Schedules;

namespace Topstar.Infrastructure.Schedules;

internal sealed class ScheduleReminderBackgroundService(IServiceScopeFactory scopeFactory,ILogger<ScheduleReminderBackgroundService> logger):BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer=new PeriodicTimer(TimeSpan.FromMinutes(5));
        while(await timer.WaitForNextTickAsync(stoppingToken))
        {
            try{using var scope=scopeFactory.CreateScope();var service=scope.ServiceProvider.GetRequiredService<IScheduleWorkflowService>();var sent=await service.ProcessDueRemindersAsync(stoppingToken);if(sent>0)logger.LogInformation("Đã gửi {Count} mốc nhắc booking.",sent);}
            catch(OperationCanceledException) when(stoppingToken.IsCancellationRequested){break;}
            catch(Exception ex){logger.LogError(ex,"Lỗi ScheduleReminderBackgroundService");}
        }
    }
}

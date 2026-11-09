using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Topstar.Application.Billing;
using Topstar.Application.Finance;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Finance;

/// <summary>
/// Closes overdue payment orders and releases KOC earnings after the dispute hold.
/// Every transition is conditional and writes status history, so repeated runs are safe.
/// </summary>
public sealed class CampaignFinanceBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<CampaignFinanceBackgroundService> logger) : BackgroundService
{
    private static readonly TimeSpan Interval = TimeSpan.FromMinutes(2);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await RunOnceAsync(stoppingToken);
        using var timer = new PeriodicTimer(Interval);
        while (await timer.WaitForNextTickAsync(stoppingToken)) await RunOnceAsync(stoppingToken);
    }

    private async Task RunOnceAsync(CancellationToken ct)
    {
        try
        {
            await using var scope = scopeFactory.CreateAsyncScope();
            var db = scope.ServiceProvider.GetRequiredService<TopstarDbContext>();
            var now = DateTime.UtcNow;

            var expired = await db.CampaignFundings
                .Where(x => x.Status == CampaignFundingStatuses.Pending && x.DueAtUtc < now)
                .ToListAsync(ct);
            foreach (var funding in expired)
            {
                funding.Status = CampaignFundingStatuses.Cancelled;
                funding.CancelledAtUtc = now;
                funding.CancellationReason = "Đơn tự động hết hạn do chưa thanh toán đúng hạn.";
                funding.UpdatedAtUtc = now;
                db.CampaignFundLedgers.Add(new CampaignFundLedgerEntity
                {
                    Id = Guid.NewGuid(), FundingId = funding.Id, EntryType = "PaymentExpired",
                    Amount = 0, Note = funding.CancellationReason, CreatedAtUtc = now
                });
            }

            var pending = await db.WalletTransactions
                .Where(x => x.Status == WalletStatuses.Pending)
                .ToListAsync(ct);
            foreach (var wallet in pending)
            {
                var hasDispute = await db.DisputeCases.AsNoTracking()
                    .AnyAsync(x => x.ParticipationId == wallet.ParticipationId && x.IsActive, ct);
                if (hasDispute) continue;

                var holdDays = await (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                                      join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                                      where allocation.WalletTransactionId == wallet.Id
                                      select (int?)funding.DisputeHoldDaysSnapshot)
                    .FirstOrDefaultAsync(ct);
                holdDays ??= await db.CampaignFundings.AsNoTracking()
                    .Where(x => x.CampaignId == wallet.CampaignId &&
                                (x.Status == CampaignFundingStatuses.Paid ||
                                 x.Status == CampaignFundingStatuses.PartiallyReleased ||
                                 x.Status == CampaignFundingStatuses.Released))
                    .OrderByDescending(x => x.VersionNo)
                    .Select(x => (int?)x.DisputeHoldDaysSnapshot)
                    .FirstOrDefaultAsync(ct);
                if (!holdDays.HasValue) continue;
                if (wallet.CreatedAtUtc.AddDays(holdDays.Value) > now) continue;

                wallet.Status = WalletStatuses.Available;
                wallet.AvailableAtUtc = now;
                wallet.UpdatedAtUtc = now;
                db.WalletTransactionStatusHistories.Add(new WalletTransactionStatusHistoryEntity
                {
                    Id = Guid.NewGuid(), WalletTransactionId = wallet.Id,
                    FromStatus = WalletStatuses.Pending, ToStatus = WalletStatuses.Available,
                    Reason = $"Tự động khả dụng sau {holdDays.Value} ngày chờ tranh chấp.", CreatedAtUtc = now
                });
            }

            if (expired.Count > 0 || db.ChangeTracker.HasChanges()) await db.SaveChangesAsync(ct);
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
        catch (Exception exception)
        {
            logger.LogError(exception, "Campaign finance lifecycle background job failed.");
        }
    }
}

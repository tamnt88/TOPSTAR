using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Topstar.Application.SocialSync;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.SocialSync;

internal sealed class SocialSyncOrchestrator(
    TopstarDbContext dbContext,
    IYouTubeMetricsClient youTubeMetricsClient,
    ITikTokMetricsClient tikTokMetricsClient,
    ILogger<SocialSyncOrchestrator> logger) : ISocialSyncOrchestrator
{
    public async Task<IReadOnlyList<AdminSocialSyncKocItem>> GetAdminKocsAsync(CancellationToken cancellationToken=default)
    {
        var rows=await(from k in dbContext.KocProfiles.AsNoTracking()
                       join u in dbContext.Users.AsNoTracking() on k.UserId equals u.Id
                       join c in dbContext.SocialPlatformConnections.AsNoTracking() on u.Id equals c.UserId
                       where k.Status=="Approved"&&c.Status=="Active"
                       orderby k.DisplayName
                       select new{k.Id,k.DisplayName,u.Email,c.Platform}).Take(1000).ToListAsync(cancellationToken);
        var profiles=rows.GroupBy(x=>new{x.Id,x.DisplayName,x.Email}).Take(300).ToList();
        var ids=profiles.Select(x=>x.Key.Id).ToArray();
        var jobs=await dbContext.SocialSyncJobs.AsNoTracking().Where(x=>x.KocProfileId.HasValue&&ids.Contains(x.KocProfileId.Value))
            .OrderByDescending(x=>x.RequestedAtUtc).Select(x=>new{x.KocProfileId,x.Status,x.RequestedAtUtc}).ToListAsync(cancellationToken);
        return profiles.Select(x=>{var last=jobs.FirstOrDefault(j=>j.KocProfileId==x.Key.Id);return new AdminSocialSyncKocItem(x.Key.Id,x.Key.DisplayName,x.Key.Email,string.Join(", ",x.Select(p=>p.Platform).Distinct()),last?.Status,last?.RequestedAtUtc);}).ToList();
    }

    public async Task<IReadOnlyList<AdminSocialSyncJobItem>> GetAdminRecentJobsAsync(int take=30,CancellationToken cancellationToken=default)
        =>await(from j in dbContext.SocialSyncJobs.AsNoTracking()
                join k0 in dbContext.KocProfiles.AsNoTracking() on j.KocProfileId equals k0.Id into kocs
                from k in kocs.DefaultIfEmpty()
                orderby j.RequestedAtUtc descending
                select new AdminSocialSyncJobItem(j.Id,k!=null?k.DisplayName:"Không xác định",j.Scope,j.RequestedByRole,j.Status,j.ErrorMessage,j.RequestedAtUtc,j.StartedAtUtc,j.FinishedAtUtc))
            .Take(Math.Clamp(take,1,100)).ToListAsync(cancellationToken);

    public async Task<SocialSyncRequestResult> RequestKocSelfSyncAsync(Guid requesterUserId, CancellationToken cancellationToken = default)
    {
        var koc = await dbContext.KocProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.UserId == requesterUserId, cancellationToken);
        if (koc is null)
        {
            return new SocialSyncRequestResult(false, "Không tìm thấy hồ sơ KOC.");
        }

        var hasConnection = await dbContext.SocialPlatformConnections.AsNoTracking()
            .AnyAsync(x => x.UserId == requesterUserId && x.Status == "Active", cancellationToken);
        if (!hasConnection)
        {
            return new SocialSyncRequestResult(false, "Bạn chưa kết nối OAuth nền tảng nào. Vui lòng kết nối YouTube/TikTok/Facebook trước.");
        }

        var job = NewJob(requesterUserId, "Koc", SocialSyncScope.KocSelf, koc.Id, null);
        dbContext.SocialSyncJobs.Add(job);
        try{await dbContext.SaveChangesAsync(cancellationToken);}
        catch(DbUpdateException){return new SocialSyncRequestResult(false,"KOC vừa có một job khác được tạo. Vui lòng tải lại.");}
        return new SocialSyncRequestResult(true, "Đã đưa yêu cầu đồng bộ vào hàng đợi.", job.Id, 1);
    }

    public async Task<SocialSyncRequestResult> RequestAdminSyncSingleKocAsync(Guid requesterUserId, Guid kocProfileId, CancellationToken cancellationToken = default)
    {
        var exists = await dbContext.KocProfiles.AsNoTracking().AnyAsync(x => x.Id == kocProfileId
            && x.Status == "Approved"
            && dbContext.SocialPlatformConnections.Any(c => c.UserId == x.UserId && c.Status == "Active"), cancellationToken);
        if (!exists)
        {
            return new SocialSyncRequestResult(false, "KOC không tồn tại.");
        }

        if(await dbContext.SocialSyncJobs.AsNoTracking().AnyAsync(x=>x.KocProfileId==kocProfileId&&(x.Status=="Pending"||x.Status=="Running"),cancellationToken))
            return new SocialSyncRequestResult(false,"KOC đã có job đang chờ hoặc đang chạy.");
        var job = NewJob(requesterUserId, "Admin", SocialSyncScope.AdminSingleKoc, kocProfileId, null);
        dbContext.SocialSyncJobs.Add(job);
        try{await dbContext.SaveChangesAsync(cancellationToken);}
        catch(DbUpdateException){return new SocialSyncRequestResult(false,"KOC vừa có một job khác được tạo. Vui lòng tải lại.");}
        return new SocialSyncRequestResult(true, "Đã tạo yêu cầu đồng bộ KOC.", job.Id, 1);
    }

    public async Task<SocialSyncRequestResult> RequestAdminSyncAllKocsAsync(Guid requesterUserId, CancellationToken cancellationToken = default)
    {
        var allKocIds = await dbContext.KocProfiles.AsNoTracking()
            .Where(x=>x.Status=="Approved"
                &&dbContext.SocialPlatformConnections.Any(c=>c.UserId==x.UserId&&c.Status=="Active")
                &&!dbContext.SocialSyncJobs.Any(j=>j.KocProfileId==x.Id&&(j.Status=="Pending"||j.Status=="Running")))
            .Select(x => x.Id).ToListAsync(cancellationToken);
        if (allKocIds.Count == 0)
        {
            return new SocialSyncRequestResult(false, "Không có KOC nào trong hệ thống.");
        }

        foreach (var kocId in allKocIds)
        {
            dbContext.SocialSyncJobs.Add(NewJob(requesterUserId, "Admin", SocialSyncScope.AdminAllKoc, kocId, null));
        }

        try{await dbContext.SaveChangesAsync(cancellationToken);}
        catch(DbUpdateException){return new SocialSyncRequestResult(false,"Một số KOC vừa có job khác được tạo. Vui lòng tải lại danh sách.");}
        return new SocialSyncRequestResult(true, $"Đã tạo {allKocIds.Count} job đồng bộ toàn hệ thống.", null, allKocIds.Count);
    }

    public async Task<SocialSyncRequestResult> RequestBrandCampaignSyncAsync(Guid requesterUserId, Guid campaignId, CancellationToken cancellationToken = default)
    {
        var brand = await dbContext.BrandProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == requesterUserId, cancellationToken);
        if (brand is null)
        {
            return new SocialSyncRequestResult(false, "Không tìm thấy tài khoản Brand.");
        }

        var campaign = await dbContext.Campaigns.AsNoTracking().FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null || campaign.BrandProfileId != brand.Id)
        {
            return new SocialSyncRequestResult(false, "Chiến dịch không hợp lệ hoặc không thuộc Brand hiện tại.");
        }

        var kocIds = await dbContext.Participations.AsNoTracking()
            .Where(x => x.CampaignId == campaignId)
            .Where(x=>dbContext.KocProfiles.Any(k=>k.Id==x.KocProfileId
                &&dbContext.SocialPlatformConnections.Any(c=>c.UserId==k.UserId&&c.Status=="Active")))
            .Where(x=>!dbContext.SocialSyncJobs.Any(j=>j.KocProfileId==x.KocProfileId&&(j.Status=="Pending"||j.Status=="Running")))
            .Select(x => x.KocProfileId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (kocIds.Count == 0)
        {
            return new SocialSyncRequestResult(false, "Chiến dịch chưa có KOC tham gia.");
        }

        foreach (var kocId in kocIds)
        {
            dbContext.SocialSyncJobs.Add(NewJob(requesterUserId, "Brand", SocialSyncScope.BrandCampaign, kocId, campaignId));
        }
        try{await dbContext.SaveChangesAsync(cancellationToken);}
        catch(DbUpdateException){return new SocialSyncRequestResult(false,"Một số KOC vừa có job khác được tạo. Vui lòng thử lại.");}

        return new SocialSyncRequestResult(true, $"Đã tạo {kocIds.Count} job đồng bộ cho chiến dịch.", null, kocIds.Count);
    }

    public async Task<int> ProcessPendingJobsAsync(int maxJobs, CancellationToken cancellationToken = default)
    {
        var jobs = await dbContext.SocialSyncJobs
            .Where(x => x.Status == "Pending")
            .OrderBy(x => x.RequestedAtUtc)
            .Take(Math.Max(1, maxJobs))
            .ToListAsync(cancellationToken);

        var processed = 0;
        foreach (var job in jobs)
        {
            processed += await ProcessOneJobAsync(job, cancellationToken);
        }

        return processed;
    }

    private async Task<int> ProcessOneJobAsync(SocialSyncJobEntity job, CancellationToken cancellationToken)
    {
        if (!job.KocProfileId.HasValue)
        {
            job.Status = "Failed";
            job.ErrorMessage = "Job không có KOC mục tiêu.";
            job.FinishedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return 0;
        }

        job.Status = "Running";
        job.StartedAtUtc = DateTime.UtcNow;
        job.ErrorMessage = null;
        await dbContext.SaveChangesAsync(cancellationToken);

        try
        {
            var koc = await dbContext.KocProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.Id == job.KocProfileId.Value, cancellationToken);
            if (koc is null)
            {
                throw new InvalidOperationException("KOC không tồn tại.");
            }

            var socials = await dbContext.KocSocialAccounts
                .Where(x => x.KocProfileId == koc.Id && x.IsVisible)
                .ToListAsync(cancellationToken);
            var connections = await dbContext.SocialPlatformConnections
                .Where(x => x.UserId == koc.UserId && x.Status == "Active")
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var social in socials)
            {
                if (social.Platform.Equals("YouTube", StringComparison.OrdinalIgnoreCase))
                {
                    var ytConn = connections.FirstOrDefault(x => x.Platform.Equals("YouTube", StringComparison.OrdinalIgnoreCase));
                    if (ytConn is not null)
                    {
                        var ytMetrics = await youTubeMetricsClient.FetchProfileMetricsAsync(ytConn.Id, cancellationToken);
                        if (ytMetrics is not null)
                        {
                            social.Followers = ytMetrics.Followers ?? social.Followers;
                            social.AvgViews = ytMetrics.AvgViews ?? social.AvgViews;
                            if(string.IsNullOrWhiteSpace(social.ChannelUrl)&&!string.IsNullOrWhiteSpace(ytConn.ProfileUrl))
                                social.ChannelUrl=ytConn.ProfileUrl;
                            if (!string.IsNullOrWhiteSpace(ytMetrics.ChannelHandle))
                            {
                                social.Handle = ytMetrics.ChannelHandle;
                            }
                        }
                    }
                }
                else if (social.Platform.Equals("TikTok", StringComparison.OrdinalIgnoreCase))
                {
                    var tiktokConn = connections.FirstOrDefault(x => x.Platform.Equals("TikTok", StringComparison.OrdinalIgnoreCase));
                    if (tiktokConn is not null)
                    {
                        var tiktokMetrics = await tikTokMetricsClient.FetchProfileMetricsAsync(tiktokConn.Id, cancellationToken);
                        if (tiktokMetrics is not null)
                        {
                            social.Followers = tiktokMetrics.Followers ?? social.Followers;
                            social.AvgViews = tiktokMetrics.AvgViews ?? social.AvgViews;
                            if (!string.IsNullOrWhiteSpace(tiktokMetrics.DisplayName))
                            {
                                social.Handle = tiktokMetrics.DisplayName;
                            }
                        }
                    }
                }

                dbContext.SocialProfileMetricSnapshots.Add(new SocialProfileMetricSnapshotEntity
                {
                    Id = Guid.NewGuid(),
                    KocProfileId = koc.Id,
                    Platform = social.Platform,
                    SnapshotAtUtc = now,
                    Followers = social.Followers,
                    AvgViews = social.AvgViews,
                    AvgEngagementRate = social.AvgEngagementRate,
                    Reach = social.AvgViews,
                    Impressions = social.AvgViews,
                    Likes = null,
                    Comments = null,
                    Shares = null,
                    PostsCount = null,
                    DataSource = "OAuthApi",
                    ConfidenceScore = 95,
                    CreatedAtUtc = now
                });

                social.UpdatedAtUtc = now;
            }

            koc.Followers = socials.Select(x => x.Followers).DefaultIfEmpty(0).Max();
            koc.UpdatedAtUtc = now;

            await dbContext.SaveChangesAsync(cancellationToken);
            job.Status = "Succeeded";
            job.FinishedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return 1;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Lỗi xử lý social sync job {JobId}", job.Id);
            job.Status = "Failed";
            job.ErrorMessage = ex.Message;
            job.FinishedAtUtc = DateTime.UtcNow;
            await dbContext.SaveChangesAsync(cancellationToken);
            return 0;
        }
    }

    private static SocialSyncJobEntity NewJob(Guid requesterUserId, string requesterRole, SocialSyncScope scope, Guid? kocProfileId, Guid? campaignId)
    {
        return new SocialSyncJobEntity
        {
            Id = Guid.NewGuid(),
            RequestedByUserId = requesterUserId,
            RequestedByRole = requesterRole,
            Scope = scope.ToString(),
            KocProfileId = kocProfileId,
            CampaignId = campaignId,
            Status = "Pending",
            RequestedAtUtc = DateTime.UtcNow
        };
    }
}

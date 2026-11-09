using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Application.Matching;
using Topstar.Application.Deliverables;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Matching;

public sealed class MatchingService(TopstarDbContext db) : IMatchingService
{
    public async Task<IReadOnlyList<MatchingCampaignItem>> GetAdminCampaignsAsync(CancellationToken cancellationToken = default)
        => await (from c in db.Campaigns.AsNoTracking()
                  join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                  where c.Status == CampaignStatuses.Published || c.Status == CampaignStatuses.InProgress
                  orderby c.UpdatedAtUtc ?? c.CreatedAtUtc descending
                  select new MatchingCampaignItem(c.Id, c.Title, b.CompanyName, c.Status,
                      db.Applications.Count(x => x.CampaignId == c.Id && x.Status == ApplicationStatuses.Applied),
                      db.Applications.Count(x => x.CampaignId == c.Id && x.Status == "Shortlisted"),
                      db.Applications.Count(x => x.CampaignId == c.Id && x.Status == "Confirmed")))
            .ToListAsync(cancellationToken);

    public async Task<AdminMatchingDetail?> GetAdminDetailAsync(Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await (from c in db.Campaigns.AsNoTracking()
                              join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                              where c.Id == campaignId
                              select new { c.Id, c.Title, BrandName = b.CompanyName, c.Status, c.BudgetAmount, c.DesiredKocCount }).FirstOrDefaultAsync(cancellationToken);
        if (campaign is null) return null;
        var rows = await (from a in db.Applications.AsNoTracking()
                                  join k in db.KocProfiles.AsNoTracking() on a.KocProfileId equals k.Id
                                  where a.CampaignId == campaignId && a.Status != ApplicationStatuses.Withdrawn
                                  select new {Application=a,Koc=k}).ToListAsync(cancellationToken);
        var userIds=rows.Select(x=>x.Koc.UserId).Distinct().ToArray();
        var rowKocIds = rows.Select(x => x.Koc.Id).Distinct().ToArray();
        var socialFollowerMap = await db.KocSocialAccounts.AsNoTracking()
            .Where(x => rowKocIds.Contains(x.KocProfileId) && x.IsVisible)
            .GroupBy(x => x.KocProfileId)
            .Select(x => new { KocProfileId = x.Key, Followers = x.Max(s => s.Followers) })
            .ToDictionaryAsync(x => x.KocProfileId, x => x.Followers, cancellationToken);
        var penalties=await db.UserViolations.AsNoTracking().Where(x=>userIds.Contains(x.UserId)&&x.Status=="Active").GroupBy(x=>x.UserId).Select(g=>new{UserId=g.Key,Points=g.Sum(x=>x.PenaltyPoints)}).ToDictionaryAsync(x=>x.UserId,x=>x.Points,cancellationToken);
        var matchMap=await BuildMatchMapAsync(campaignId,rows.Select(x=>x.Koc.Id),cancellationToken);
        var applications=rows.Select(x=>new AdminApplicationItem(x.Application.Id,x.Koc.DisplayName,Math.Max(x.Koc.Followers, socialFollowerMap.GetValueOrDefault(x.Koc.Id)),UserViolationRules.EffectiveScore(x.Koc.Score,penalties.GetValueOrDefault(x.Koc.UserId)),x.Application.Status,x.Application.PitchNote,x.Application.AppliedAtUtc){KocProfileId=x.Koc.Id,AvatarUrl=x.Koc.AvatarUrl,TargetingMatchScore=matchMap[x.Koc.Id].Score,ProposedAmount=x.Application.ProposedAmount,UnmatchedCriteria=matchMap[x.Koc.Id].Unmatched}).OrderByDescending(x=>x.TargetingMatchScore).ThenByDescending(x=>x.Score).ThenByDescending(x=>x.Followers).ThenBy(x=>x.AppliedAtUtc).ToList();
        var policy = await ResolvePolicyAsync(campaign.Id, campaign.BudgetAmount, cancellationToken);
        IReadOnlyList<DirectKocCandidate> directCandidates = [];
        if (policy is { BrandSelectionEnabled: false })
        {
            var existingKocIds = rows.Select(x => x.Koc.Id).ToArray();
            var pool = await db.KocProfiles.AsNoTracking()
                .Where(x => x.Status == "Approved" && x.IsVisible && !existingKocIds.Contains(x.Id))
                .OrderByDescending(x => x.Score).ThenByDescending(x => x.Followers).Take(50).ToListAsync(cancellationToken);
            var poolMatch = await BuildMatchMapAsync(campaignId, pool.Select(x => x.Id), cancellationToken);
            var poolIds = pool.Select(x => x.Id).ToArray();
            var poolSocialFollowers = await db.KocSocialAccounts.AsNoTracking().Where(x => poolIds.Contains(x.KocProfileId) && x.IsVisible)
                .GroupBy(x => x.KocProfileId).Select(x => new { KocProfileId = x.Key, Followers = x.Max(s => s.Followers) })
                .ToDictionaryAsync(x => x.KocProfileId, x => x.Followers, cancellationToken);
            directCandidates = pool.Select(x => new DirectKocCandidate(x.Id, x.DisplayName, Math.Max(x.Followers, poolSocialFollowers.GetValueOrDefault(x.Id)), x.Score, poolMatch[x.Id].Score, poolMatch[x.Id].Unmatched) { AvatarUrl = x.AvatarUrl })
                .OrderByDescending(x => x.TargetingMatchScore).ThenByDescending(x => x.Score).ToList();
        }
        return new(campaign.Id, campaign.Title, campaign.BrandName, campaign.Status, applications)
        {
            DesiredKocCount = campaign.DesiredKocCount,
            ShortlistedCount = applications.Count(x => x.Status == "Shortlisted" || x.Status == "Confirmed"),
            BrandSelectionEnabled = policy?.BrandSelectionEnabled ?? false,
            DirectCandidates = directCandidates
        };
    }

    public async Task<MatchingResult> SendShortlistAsync(Guid adminUserId, Guid campaignId, IReadOnlyCollection<Guid> applicationIds, string? note, CancellationToken cancellationToken = default)
    {
        var selectedIds = applicationIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (selectedIds.Length == 0) return new(false, "Vui lòng chọn ít nhất một KOC.");
        var campaign = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null) return new(false, "Không tìm thấy chiến dịch.");
        var policy = await ResolvePolicyAsync(campaign.Id, campaign.BudgetAmount, cancellationToken);
        if (policy is null) return new(false, "Không tìm thấy quyền tuyển chọn của gói chiến dịch.");
        var existingShortlistedCount = await db.Applications.CountAsync(x => x.CampaignId == campaignId && (x.Status == "Shortlisted" || x.Status == "Confirmed"), cancellationToken);
        var resultingCount = existingShortlistedCount + selectedIds.Length;
        if (resultingCount > campaign.DesiredKocCount)
            return new(false, $"Danh sách vượt quá {campaign.DesiredKocCount} KOC chiến dịch yêu cầu.");
        var brandUserId = await db.BrandProfiles.AsNoTracking().Where(x => x.Id == campaign.BrandProfileId).Select(x => (Guid?)x.UserId).FirstOrDefaultAsync(cancellationToken);
        if (!brandUserId.HasValue) return new(false, "Không tìm thấy Brand sở hữu chiến dịch.");
        var applications = await db.Applications.Where(x => selectedIds.Contains(x.Id) && x.CampaignId == campaignId).ToListAsync(cancellationToken);
        if (applications.Count != selectedIds.Length) return new(false, "Danh sách application không hợp lệ.");
        if (applications.Any(x => !MatchingTransitions.CanShortlist(x.Status, campaign.Status)))
            return new(false, "Chỉ application Applied của campaign Published mới được shortlist.");

        var now = DateTime.UtcNow;
        var shortlist = await db.Shortlists.FirstOrDefaultAsync(x => x.CampaignId == campaignId, cancellationToken);
        if (shortlist is null)
        {
            shortlist = new ShortlistEntity { Id = Guid.NewGuid(), CampaignId = campaignId, CreatedByUserId = adminUserId, CreatedAtUtc = now };
            db.Shortlists.Add(shortlist);
        }
        else if (shortlist.FinalizedAtUtc.HasValue)
        {
            return new(false, "Shortlist đã được Brand chốt, không thể gửi thêm KOC.");
        }
        shortlist.SentToBrandAtUtc = now; shortlist.UpdatedAtUtc = now;
        var matchMap=await BuildMatchMapAsync(campaignId,applications.Select(x=>x.KocProfileId),cancellationToken);
        foreach (var application in applications)
        {
            application.Status = "Shortlisted"; application.UpdatedAtUtc = now;
            AddApplicationHistory(application.Id, ApplicationStatuses.Applied, "Shortlisted", note, adminUserId, now);
            var match=matchMap[application.KocProfileId];
            db.ShortlistItems.Add(new ShortlistItemEntity { ShortlistId = shortlist.Id, ApplicationId = application.Id, SelectionSource = "Manual", Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(), TargetingMatchScoreSnapshot=match.Score,TargetingMatchDetailsJson=JsonSerializer.Serialize(match), CreatedAtUtc = now });
        }
        AddAudit(adminUserId, shortlist.Id, "SendShortlist", new { campaignId, ApplicationIds = selectedIds, Note = note });
        db.Notifications.Add(NotificationFactory.Create(brandUserId.Value,"Shortlist","Có shortlist KOC mới",$"Admin đã gửi {applications.Count} KOC cho chiến dịch {campaign.Title}.",now,"Shortlist",shortlist.Id,audienceRole:"BRAND"));
        await db.SaveChangesAsync(cancellationToken);
        return new(true, $"Đã gửi {applications.Count} KOC vào shortlist cho Brand.", shortlist.Id);
    }

    public async Task<MatchingResult> AssignDirectAsync(Guid adminUserId, Guid campaignId, IReadOnlyCollection<Guid> kocProfileIds, string? note, CancellationToken cancellationToken = default)
    {
        var ids = kocProfileIds.Where(x => x != Guid.Empty).Distinct().ToArray();
        if (ids.Length == 0) return new(false, "Vui lòng chọn KOC cần phân công.");
        var campaign = await db.Campaigns.FirstOrDefaultAsync(x => x.Id == campaignId, cancellationToken);
        if (campaign is null || campaign.Status is not (CampaignStatuses.Published or CampaignStatuses.InProgress)) return new(false, "Chiến dịch không còn ở trạng thái có thể bổ sung KOC.");
        var policy = await ResolvePolicyAsync(campaign.Id, campaign.BudgetAmount, cancellationToken);
        if (policy is null || policy.BrandSelectionEnabled) return new(false, "Gói này không sử dụng cơ chế Topstar phân công trực tiếp.");
        var existingCount = await db.Applications.CountAsync(x => x.CampaignId == campaignId && (x.Status == "Shortlisted" || x.Status == "Confirmed"), cancellationToken);
        var missing = campaign.DesiredKocCount - existingCount;
        if (missing <= 0) return new(false, "Danh sách đã đủ số KOC chiến dịch yêu cầu.");
        if (ids.Length > missing) return new(false, $"Chỉ được chọn tối đa {missing} KOC còn thiếu.");
        var alreadyUsed = await db.Applications.Where(x => x.CampaignId == campaignId && ids.Contains(x.KocProfileId)).Select(x => x.KocProfileId).ToListAsync(cancellationToken);
        if (alreadyUsed.Count > 0) return new(false, "Có KOC đã nằm trong danh sách chiến dịch. Vui lòng tải lại trang.");
        var kocs = await db.KocProfiles.Where(x => ids.Contains(x.Id) && x.Status == "Approved" && x.IsVisible).ToListAsync(cancellationToken);
        if (kocs.Count != ids.Length) return new(false, "Một hoặc nhiều KOC không còn đủ điều kiện phân công.");
        var now = DateTime.UtcNow;
        var shortlist = await db.Shortlists.FirstOrDefaultAsync(x => x.CampaignId == campaignId, cancellationToken);
        if (shortlist?.FinalizedAtUtc is not null) return new(false, "Danh sách đã được Brand xác nhận.");
        if (shortlist is null) { shortlist = new() { Id=Guid.NewGuid(), CampaignId=campaignId, CreatedByUserId=adminUserId, CreatedAtUtc=now }; db.Shortlists.Add(shortlist); }
        shortlist.SentToBrandAtUtc=now; shortlist.UpdatedAtUtc=now;
        var matchMap = await BuildMatchMapAsync(campaignId, ids, cancellationToken);
        foreach (var koc in kocs)
        {
            var application = new ApplicationEntity { Id=Guid.NewGuid(), CampaignId=campaignId, KocProfileId=koc.Id, Status="Shortlisted", PitchNote="Topstar phân công theo gói dịch vụ.", AppliedAtUtc=now, CreatedAtUtc=now };
            db.Applications.Add(application);
            AddApplicationHistory(application.Id, null, "Shortlisted", note ?? "Topstar phân công trực tiếp", adminUserId, now);
            var match=matchMap[koc.Id];
            db.ShortlistItems.Add(new() { ShortlistId=shortlist.Id, ApplicationId=application.Id, SelectionSource="Manual", Note=string.IsNullOrWhiteSpace(note)?null:note.Trim(), TargetingMatchScoreSnapshot=match.Score, TargetingMatchDetailsJson=JsonSerializer.Serialize(match), CreatedAtUtc=now });
        }
        var brandUserId = await db.BrandProfiles.AsNoTracking().Where(x => x.Id == campaign.BrandProfileId).Select(x => x.UserId).FirstAsync(cancellationToken);
        var remainingAfter = missing - ids.Length;
        db.Notifications.Add(NotificationFactory.Create(brandUserId,"Shortlist",remainingAfter == 0 ? "Topstar đã bổ sung đủ KOC" : "Topstar đã bổ sung KOC",remainingAfter == 0 ? $"Topstar đã bổ sung {ids.Length} KOC và hoàn tất danh sách cho chiến dịch {campaign.Title}." : $"Topstar đã bổ sung {ids.Length} KOC cho chiến dịch {campaign.Title}; danh sách còn thiếu {remainingAfter} KOC.",now,"Shortlist",shortlist.Id,audienceRole:"BRAND"));
        AddAudit(adminUserId, shortlist.Id, "AssignDirectKoc", new { campaignId, KocProfileIds=ids, MissingBefore=missing, Note=note });
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return new(false, "Chưa thể lưu KOC được phân công. Vui lòng tải lại trang và thử lại."); }
        return new(true, remainingAfter == 0
            ? $"Đã phân công {ids.Length} KOC và hoàn tất danh sách cho Brand."
            : $"Đã phân công {ids.Length} KOC; có thể tiếp tục chọn thêm {remainingAfter} KOC sau.", shortlist.Id);
    }

    public async Task<MatchingResult> RemoveAssignedAsync(Guid adminUserId, Guid campaignId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var row = await (from application in db.Applications
                         join campaign in db.Campaigns on application.CampaignId equals campaign.Id
                         join koc in db.KocProfiles on application.KocProfileId equals koc.Id
                         where application.Id == applicationId && application.CampaignId == campaignId
                         select new { Application = application, Campaign = campaign, Koc = koc }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy KOC trong chiến dịch này.");
        if (row.Campaign.Status is not (CampaignStatuses.Published or CampaignStatuses.InProgress))
            return new(false, "Chỉ có thể gỡ KOC khi chiến dịch đang tuyển chọn.");
        if (row.Application.Status != ApplicationStatuses.Shortlisted)
            return new(false, row.Application.Status == ApplicationStatuses.Confirmed
                ? "KOC đã được Brand xác nhận nên không thể gỡ tại bước tuyển chọn."
                : "KOC này không còn ở trạng thái chờ Brand xác nhận.");

        var shortlist = await db.Shortlists.FirstOrDefaultAsync(x => x.CampaignId == campaignId, cancellationToken);
        if (shortlist?.FinalizedAtUtc is not null) return new(false, "Danh sách đã được Brand chốt nên không thể gỡ KOC.");
        if (shortlist is not null)
        {
            var item = await db.ShortlistItems.FirstOrDefaultAsync(x => x.ShortlistId == shortlist.Id && x.ApplicationId == applicationId, cancellationToken);
            if (item is not null) db.ShortlistItems.Remove(item);
            shortlist.UpdatedAtUtc = DateTime.UtcNow;
        }

        var now = DateTime.UtcNow;
        row.Application.Status = ApplicationStatuses.Withdrawn;
        row.Application.UpdatedAtUtc = now;
        AddApplicationHistory(applicationId, ApplicationStatuses.Shortlisted, ApplicationStatuses.Withdrawn, "Admin gỡ khỏi danh sách phân công", adminUserId, now);
        AddAudit(adminUserId, applicationId, "RemoveAssignedKoc", new { campaignId, row.Application.KocProfileId });
        await db.SaveChangesAsync(cancellationToken);
        return new(true, $"Đã gỡ {row.Koc.DisplayName} khỏi danh sách phân công.");
    }

    public async Task<BrandShortlistDetail?> GetBrandShortlistAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await (from c in db.Campaigns.AsNoTracking()
                              join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                              where c.Id == campaignId && b.UserId == brandUserId
                              select new { c.Id, c.Title, c.Status, c.BudgetAmount, c.DesiredKocCount }).FirstOrDefaultAsync(cancellationToken);
        if (campaign is null) return null;
        var policy = await ResolvePolicyAsync(campaign.Id, campaign.BudgetAmount, cancellationToken);
        if (policy is null) return null;
        var shortlistState = await db.Shortlists.AsNoTracking().Where(x => x.CampaignId == campaignId).Select(x => new { x.FinalizedAtUtc }).FirstOrDefaultAsync(cancellationToken);
        var rows = await (from item in db.ShortlistItems.AsNoTracking()
                           join shortlist in db.Shortlists.AsNoTracking() on item.ShortlistId equals shortlist.Id
                           join application in db.Applications.AsNoTracking() on item.ApplicationId equals application.Id
                           join koc in db.KocProfiles.AsNoTracking() on application.KocProfileId equals koc.Id
                           where shortlist.CampaignId == campaignId && application.Status != "NotSelected"
                           orderby koc.Score descending
                           select new { application.Id, KocProfileId = koc.Id,koc.UserId, koc.Followers, koc.Score, application.Status, application.PitchNote,application.ProposedAmount,item.TargetingMatchScoreSnapshot,item.TargetingMatchDetailsJson,
                               IsConfirmed = db.Participations.Any(x => x.CampaignId == campaignId && x.KocProfileId == koc.Id && x.Status == "Confirmed") }).ToListAsync(cancellationToken);
        var shortlistUserIds=rows.Select(x=>x.UserId).Distinct().ToArray();var shortlistPenalties=await db.UserViolations.AsNoTracking().Where(x=>shortlistUserIds.Contains(x.UserId)&&x.Status=="Active").GroupBy(x=>x.UserId).Select(g=>new{UserId=g.Key,Points=g.Sum(x=>x.PenaltyPoints)}).ToDictionaryAsync(x=>x.UserId,x=>x.Points,cancellationToken);
        var items = rows.Select(x => {var details=ParseMatch(x.TargetingMatchDetailsJson);return new BrandShortlistItem(x.Id, $"KOC-{x.KocProfileId:N}"[..12].ToUpperInvariant(), x.Followers, UserViolationRules.EffectiveScore(x.Score,shortlistPenalties.GetValueOrDefault(x.UserId)), x.Status, null, x.IsConfirmed){TargetingMatchScore=x.TargetingMatchScoreSnapshot,ProposedAmount=x.ProposedAmount,UnmatchedCriteria=details?.Unmatched??[]};}).OrderByDescending(x=>x.TargetingMatchScore).ThenByDescending(x=>x.Score).ToList();
        return new(campaign.Id, campaign.Title, campaign.Status, policy.Name, policy.BrandSelectionEnabled, policy.SelectionQuota, shortlistState?.FinalizedAtUtc is not null, items.Count(x => x.IsConfirmed), items) { DesiredKocCount = campaign.DesiredKocCount };
    }

    public async Task<MatchingResult> RejectProposedAsync(Guid brandUserId, Guid applicationId, string reason, CancellationToken cancellationToken = default)
    {
        reason = reason?.Trim() ?? string.Empty;
        if (reason.Length is < 10 or > 500) return new(false, "Lý do từ chối phải từ 10 đến 500 ký tự.");
        var row = await (from application in db.Applications
                         join campaign in db.Campaigns on application.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where application.Id == applicationId && brand.UserId == brandUserId
                         select new { Application=application, Campaign=campaign }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy hồ sơ KOC hoặc Brand không có quyền thao tác.");
        if (row.Campaign.Status is not (CampaignStatuses.Published or CampaignStatuses.InProgress) || row.Application.Status != "Shortlisted")
            return new(false, "Chỉ được từ chối KOC đang được đề xuất trước khi chốt danh sách.");
        var policy = await ResolvePolicyAsync(row.Campaign.Id, row.Campaign.BudgetAmount, cancellationToken);
        if (policy is null || policy.BrandSelectionEnabled)
            return new(false, "Gói Brand tự chọn không cần từ chối hồ sơ; hãy chỉ chọn các KOC phù hợp.");
        var shortlist = await db.Shortlists.FirstOrDefaultAsync(x => x.CampaignId == row.Campaign.Id, cancellationToken);
        if (shortlist?.FinalizedAtUtc is not null) return new(false, "Danh sách đã được chốt, không thể từ chối KOC.");
        var now = DateTime.UtcNow;
        row.Application.Status="NotSelected"; row.Application.UpdatedAtUtc=now;
        AddApplicationHistory(row.Application.Id,"Shortlisted","NotSelected",reason,brandUserId,now);
        if (shortlist is not null) shortlist.UpdatedAtUtc=now;
        AddAudit(brandUserId,row.Application.Id,"RejectProposedKoc",new{row.Campaign.Id,Reason=reason});
        await db.SaveChangesAsync(cancellationToken);
        return new(true,"Đã từ chối KOC. Topstar sẽ bổ sung hồ sơ thay thế.",row.Application.Id);
    }

    public async Task<MatchingResult> ConfirmAsync(Guid brandUserId, Guid applicationId, CancellationToken cancellationToken = default)
    {
        var row = await (from application in db.Applications
                         join campaign in db.Campaigns on application.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where application.Id == applicationId && brand.UserId == brandUserId
                         select new { Application = application, Campaign = campaign }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy application hoặc bạn không có quyền xác nhận.");
        if (!MatchingTransitions.CanConfirm(row.Application.Status, row.Campaign.Status))
            return new(false, "Chỉ KOC Shortlisted của campaign Published mới được xác nhận.");
        var existsInShortlist = await (from item in db.ShortlistItems.AsNoTracking()
                                       join shortlistRow in db.Shortlists.AsNoTracking() on item.ShortlistId equals shortlistRow.Id
                                       where item.ApplicationId == applicationId && shortlistRow.CampaignId == row.Campaign.Id
                                       select item.ApplicationId).AnyAsync(cancellationToken);
        if (!existsInShortlist) return new(false, "Application không nằm trong shortlist đã gửi.");
        var shortlist = await db.Shortlists.AsNoTracking().FirstOrDefaultAsync(x => x.CampaignId == row.Campaign.Id, cancellationToken);
        if (shortlist?.FinalizedAtUtc is not null) return new(false, "Shortlist đã được chốt.");
        var policy = await ResolvePolicyAsync(row.Campaign.Id, row.Campaign.BudgetAmount, cancellationToken);
        if (policy is null) return new(false, "Không tìm thấy package policy phù hợp.");
        var confirmedCount = await db.Participations.CountAsync(x => x.CampaignId == row.Campaign.Id && (x.Status == ParticipationStatuses.Invited || x.Status == ParticipationStatuses.Confirmed), cancellationToken);
        if (confirmedCount >= row.Campaign.DesiredKocCount)
            return new(false, $"Chiến dịch đã đủ {row.Campaign.DesiredKocCount} KOC mong muốn.");
        if (!MatchingTransitions.CanBrandSelect(policy.BrandSelectionEnabled, shortlist?.FinalizedAtUtc is not null, confirmedCount, policy.SelectionQuota))
            return new(false, policy.BrandSelectionEnabled ? $"Đã đạt quota {policy.SelectionQuota} KOC hoặc shortlist đã chốt." : "Gói này do Admin phân công KOC. Brand chỉ cần xác nhận toàn bộ shortlist.");
        if (await db.Participations.AnyAsync(x => x.CampaignId == row.Campaign.Id && x.KocProfileId == row.Application.KocProfileId, cancellationToken))
            return new(false, "KOC đã được xác nhận cho chiến dịch này.");
        var now = DateTime.UtcNow;
        row.Application.Status = "Confirmed"; row.Application.UpdatedAtUtc = now;
        AddApplicationHistory(row.Application.Id, "Shortlisted", "Confirmed", "Brand selected", brandUserId, now);
        var participation = new ParticipationEntity { Id = Guid.NewGuid(), CampaignId = row.Campaign.Id, KocProfileId = row.Application.KocProfileId, Status = ParticipationStatuses.Invited, ConfirmedAtUtc = now, CreatedAtUtc = now };
        db.Participations.Add(participation);
        AddParticipationHistory(participation.Id, null, ParticipationStatuses.Invited, "Brand gửi lời mời tham gia chiến dịch", brandUserId, now);
        var kocUserId = await db.KocProfiles.AsNoTracking().Where(x => x.Id == row.Application.KocProfileId).Select(x => (Guid?)x.UserId).FirstOrDefaultAsync(cancellationToken);
        if (kocUserId.HasValue)
            db.Notifications.Add(NotificationFactory.Create(kocUserId.Value,"Participation","Lời mời tham gia chiến dịch",$"Brand mời bạn tham gia chiến dịch {row.Campaign.Title}. Vui lòng xem thông tin và xác nhận đồng ý hoặc từ chối.",now,"Participation",participation.Id,audienceRole:"KOC"));
        AddAudit(brandUserId, participation.Id, "ConfirmParticipation", new { applicationId, row.Campaign.Id, row.Application.KocProfileId });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return new(false, (ex.InnerException?.Message.Contains("CK_Participations_Status", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message.Contains("CK_ParticipationStatusHistories_", StringComparison.OrdinalIgnoreCase) == true)
                ? "Hệ thống chưa cập nhật trạng thái lời mời KOC. Vui lòng liên hệ quản trị kỹ thuật."
                : "Không thể gửi lời mời KOC vì dữ liệu vừa thay đổi. Vui lòng tải lại trang.");
        }
        return new(true, "Đã xác nhận KOC tham gia chiến dịch.", participation.Id);
    }

    public async Task<MatchingResult> AcceptInvitationAsync(Guid kocUserId, Guid participationId, CancellationToken cancellationToken = default)
    {
        await using var invitationTx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,cancellationToken);
        var row = await (from participation in db.Participations
                         join koc in db.KocProfiles on participation.KocProfileId equals koc.Id
                         join campaign in db.Campaigns on participation.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where participation.Id == participationId && koc.UserId == kocUserId
                         select new { Participation = participation, Campaign = campaign, BrandUserId = brand.UserId, KocName = koc.DisplayName }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy lời mời hoặc bạn không có quyền xử lý.");
        if (row.Campaign.Status is not (CampaignStatuses.Published or CampaignStatuses.InProgress)) return new(false, "Chiến dịch đang tạm ngừng hoặc đã kết thúc; chưa thể phản hồi lời mời.");
        if (row.Participation.Status != ParticipationStatuses.Invited) return new(false, "Lời mời này đã được xử lý trước đó.");
        if(row.Campaign.CheckoutAtCreation&&!await (from a in db.CampaignFundingAllocations join f in db.CampaignFundings on a.FundingId equals f.Id where a.ParticipationId==participationId&&a.Amount>0&&a.Status=="Active"&&(f.Status=="Paid"||f.Status=="PartiallyReleased") select a.Id).AnyAsync(cancellationToken))return new(false,"Brand chưa chốt thù lao cho lời mời. Vui lòng đợi phân bổ trước khi nhận việc.");
        var now = DateTime.UtcNow;
        row.Participation.Status = ParticipationStatuses.Confirmed;
        row.Participation.ConfirmedAtUtc = now;
        row.Participation.UpdatedAtUtc = now;
        AddParticipationHistory(participationId, ParticipationStatuses.Invited, ParticipationStatuses.Confirmed, "KOC đồng ý tham gia", kocUserId, now);
        await AddSubmissionTasksAsync(row.Participation, now, cancellationToken);
        db.Notifications.Add(NotificationFactory.Create(row.BrandUserId, "Participation", "KOC đã đồng ý tham gia", $"{row.KocName} đã đồng ý tham gia chiến dịch {row.Campaign.Title}.", now, "Participation", participationId, audienceRole: "Brand"));
        AddAudit(kocUserId, participationId, "AcceptCampaignInvitation", new { row.Campaign.Id });
        await db.SaveChangesAsync(cancellationToken);
        await invitationTx.CommitAsync(cancellationToken);
        return new(true, "Bạn đã đồng ý tham gia chiến dịch. Hãy chờ Brand mở công việc.", participationId);
    }

    public async Task<MatchingResult> DeclineInvitationAsync(Guid kocUserId, Guid participationId, string? reason, CancellationToken cancellationToken = default)
    {
        await using var invitationTx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,cancellationToken);
        var row = await (from participation in db.Participations
                         join koc in db.KocProfiles on participation.KocProfileId equals koc.Id
                         join campaign in db.Campaigns on participation.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where participation.Id == participationId && koc.UserId == kocUserId
                         select new { Participation = participation, Campaign = campaign, BrandUserId = brand.UserId, KocName = koc.DisplayName }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy lời mời hoặc bạn không có quyền xử lý.");
        if (row.Campaign.Status is not (CampaignStatuses.Published or CampaignStatuses.InProgress)) return new(false, "Chiến dịch đang tạm ngừng hoặc đã kết thúc; chưa thể phản hồi lời mời.");
        if (row.Participation.Status != ParticipationStatuses.Invited) return new(false, "Lời mời này đã được xử lý trước đó.");
        var cleanReason = string.IsNullOrWhiteSpace(reason) ? "KOC không cung cấp lý do" : reason.Trim();
        if (cleanReason.Length > 500) return new(false, "Lý do từ chối không được vượt quá 500 ký tự.");
        var now = DateTime.UtcNow;
        row.Participation.Status = ParticipationStatuses.Declined;
        row.Participation.UpdatedAtUtc = now;
        AddParticipationHistory(participationId, ParticipationStatuses.Invited, ParticipationStatuses.Declined, cleanReason, kocUserId, now);
        var application = await db.Applications.FirstOrDefaultAsync(x => x.CampaignId == row.Campaign.Id && x.KocProfileId == row.Participation.KocProfileId && x.Status == "Confirmed", cancellationToken);
        if (application is not null)
        {
            application.Status = "NotSelected";
            application.UpdatedAtUtc = now;
            AddApplicationHistory(application.Id, "Confirmed", "NotSelected", $"KOC từ chối lời mời: {cleanReason}", kocUserId, now);
        }
        db.Notifications.Add(NotificationFactory.Create(row.BrandUserId, "Participation", "KOC đã từ chối lời mời", $"{row.KocName} đã từ chối tham gia chiến dịch {row.Campaign.Title}. Lý do: {cleanReason}", now, "Participation", participationId, audienceRole: "Brand"));
        AddAudit(kocUserId, participationId, "DeclineCampaignInvitation", new { row.Campaign.Id, Reason = cleanReason });
        await db.SaveChangesAsync(cancellationToken);
        await invitationTx.CommitAsync(cancellationToken);
        return new(true, "Bạn đã từ chối lời mời tham gia chiến dịch.", participationId);
    }

    public async Task<MatchingResult> FinalizeAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default)
    {
        var campaign = await (from c in db.Campaigns
                              join b in db.BrandProfiles on c.BrandProfileId equals b.Id
                              where c.Id == campaignId && b.UserId == brandUserId
                              select c).FirstOrDefaultAsync(cancellationToken);
        if (campaign is null) return new(false, "Không tìm thấy chiến dịch hoặc bạn không có quyền chốt shortlist.");
        if (campaign.Status is not (CampaignStatuses.Published or CampaignStatuses.InProgress)) return new(false, "Chiến dịch không còn ở trạng thái có thể xác nhận KOC.");
        var shortlist = await db.Shortlists.FirstOrDefaultAsync(x => x.CampaignId == campaignId, cancellationToken);
        if (shortlist is null) return new(false, "Admin chưa gửi shortlist.");
        if (shortlist.FinalizedAtUtc.HasValue) return new(false, "Shortlist đã được chốt trước đó.");
        var policy = await ResolvePolicyAsync(campaign.Id, campaign.BudgetAmount, cancellationToken);
        if (policy is null) return new(false, "Không tìm thấy package policy phù hợp.");

        var applications = await (from item in db.ShortlistItems
                                  join application in db.Applications on item.ApplicationId equals application.Id
                                  where item.ShortlistId == shortlist.Id
                                  select application).ToListAsync(cancellationToken);
        if (applications.Count == 0) return new(false, "Shortlist không có KOC.");
        var now = DateTime.UtcNow;
        var newlyConfirmedKocIds = new HashSet<Guid>();
        var newInvitationIds = new List<Guid>();
        if (!policy.BrandSelectionEnabled)
        {
            foreach (var application in applications.Where(x => x.Status == "Shortlisted"))
            {
                application.Status = "Confirmed"; application.UpdatedAtUtc = now;
                newlyConfirmedKocIds.Add(application.KocProfileId);
                AddApplicationHistory(application.Id, "Shortlisted", "Confirmed", "Assigned package finalized", brandUserId, now);
                if (!await db.Participations.AnyAsync(x => x.CampaignId == campaignId && x.KocProfileId == application.KocProfileId, cancellationToken))
                {
                    var participation = new ParticipationEntity { Id = Guid.NewGuid(), CampaignId = campaignId, KocProfileId = application.KocProfileId, Status = ParticipationStatuses.Invited, ConfirmedAtUtc = now, CreatedAtUtc = now };
                    db.Participations.Add(participation);
                    newInvitationIds.Add(participation.Id);
                    AddParticipationHistory(participation.Id, null, ParticipationStatuses.Invited, "Brand gửi lời mời từ danh sách được phân bổ", brandUserId, now);
                }
            }
        }

        var selectedCount = applications.Count(x => x.Status == "Confirmed");
        if (selectedCount > campaign.DesiredKocCount)
            return new(false, $"Danh sách vượt quá số lượng {campaign.DesiredKocCount} KOC của chiến dịch.");
        if (selectedCount < campaign.DesiredKocCount)
        {
            if (newlyConfirmedKocIds.Count > 0)
            {
                var recipients = await db.KocProfiles.AsNoTracking().Where(x => newlyConfirmedKocIds.Contains(x.Id)).Select(x => new { x.Id, x.UserId }).ToListAsync(cancellationToken);
                foreach (var recipient in recipients)
                    db.Notifications.Add(NotificationFactory.Create(recipient.UserId,"Participation","Lời mời tham gia chiến dịch",$"Brand mời bạn tham gia chiến dịch {campaign.Title}. Vui lòng đồng ý hoặc từ chối lời mời.",now,"Campaign",campaign.Id,audienceRole:"KOC"));
            }
            campaign.UpdatedAtUtc = now;
            AddAudit(brandUserId, shortlist.Id, "ConfirmShortlistBatch", new { campaignId, policy.Code, ConfirmedCount = selectedCount, RemainingCount = campaign.DesiredKocCount - selectedCount });
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateException ex)
            {
                return new(false, (ex.InnerException?.Message.Contains("CK_Participations_Status", StringComparison.OrdinalIgnoreCase) == true
                    || ex.InnerException?.Message.Contains("CK_ParticipationStatusHistories_", StringComparison.OrdinalIgnoreCase) == true)
                    ? "Hệ thống chưa cập nhật trạng thái lời mời KOC. Vui lòng liên hệ quản trị kỹ thuật."
                    : "Không thể lưu xác nhận danh sách KOC. Vui lòng thử lại hoặc liên hệ hỗ trợ.");
            }
            return new MatchingResult(true, $"Đã gửi lời mời cho {selectedCount}/{campaign.DesiredKocCount} KOC. TopStar sẽ tiếp tục bổ sung {campaign.DesiredKocCount - selectedCount} KOC còn thiếu.", shortlist.Id) { RelatedIds = newInvitationIds };
        }
        if (!MatchingTransitions.CanFinalize(policy.BrandSelectionEnabled, false, applications.Count, selectedCount, policy.SelectionQuota))
            return new(false, "Danh sách chưa đủ điều kiện chốt hoặc số KOC đã chọn vượt quota.");
        foreach (var application in applications.Where(x => x.Status == "Shortlisted"))
        {
            application.Status = "NotSelected"; application.UpdatedAtUtc = now;
            AddApplicationHistory(application.Id, "Shortlisted", "NotSelected", "Shortlist finalized", brandUserId, now);
        }
        var shortlistedKocIds = applications.Select(a => a.KocProfileId).Distinct().ToArray();
        var kocUserMap = await db.KocProfiles.AsNoTracking().Where(x => shortlistedKocIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.UserId, cancellationToken);
        foreach (var application in applications)
        {
            if (!kocUserMap.TryGetValue(application.KocProfileId, out var userId)) continue;
            var selected = application.Status == "Confirmed";
            if (selected && (policy.BrandSelectionEnabled || !newlyConfirmedKocIds.Contains(application.KocProfileId))) continue;
            db.Notifications.Add(NotificationFactory.Create(userId,"Participation",selected ? "Lời mời tham gia chiến dịch" : "Kết quả chiến dịch",selected ? $"Brand mời bạn tham gia chiến dịch {campaign.Title}. Vui lòng đồng ý hoặc từ chối lời mời." : $"Bạn chưa được chọn cho chiến dịch {campaign.Title}.",now,"Application",application.Id,audienceRole:"KOC"));
        }
        shortlist.FinalizedAtUtc = now; shortlist.FinalizedByUserId = brandUserId; shortlist.UpdatedAtUtc = now;
        // Chốt shortlist chỉ xác lập KOC chính thức. Chiến dịch được chuyển sang InProgress
        // sau khi quỹ chiến dịch được thanh toán đầy đủ.
        campaign.UpdatedAtUtc = now;
        AddAudit(brandUserId, shortlist.Id, "FinalizeShortlist", new { campaignId, policy.Code, SelectedCount = selectedCount });
        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex)
        {
            return new(false, (ex.InnerException?.Message.Contains("CK_Participations_Status", StringComparison.OrdinalIgnoreCase) == true
                || ex.InnerException?.Message.Contains("CK_ParticipationStatusHistories_", StringComparison.OrdinalIgnoreCase) == true)
                ? "Hệ thống chưa cập nhật trạng thái lời mời KOC. Vui lòng liên hệ quản trị kỹ thuật."
                : "Không thể chốt danh sách vì dữ liệu vừa thay đổi. Vui lòng tải lại trang và kiểm tra trạng thái KOC.");
        }
        return new MatchingResult(true, $"Đã chốt danh sách và gửi {selectedCount} lời mời đến KOC.", shortlist.Id) { RelatedIds = newInvitationIds };
    }

    public async Task<MatchingResult> RemoveConfirmedAsync(Guid brandUserId, Guid applicationId, string reason, CancellationToken cancellationToken = default)
    {
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length is < 10 or > 500) return new(false, "Lý do thay KOC phải từ 10 đến 500 ký tự.");
        await using var transaction = await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var row = await (from application in db.Applications
                         join campaign in db.Campaigns on application.CampaignId equals campaign.Id
                         join brand in db.BrandProfiles on campaign.BrandProfileId equals brand.Id
                         where application.Id == applicationId && brand.UserId == brandUserId
                         select new { Application = application, Campaign = campaign }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return new(false, "Không tìm thấy KOC hoặc Brand không có quyền thao tác.");
        if (row.Campaign.Status != CampaignStatuses.Published || row.Application.Status != "Confirmed") return new(false, "Chỉ được thay KOC đã xác nhận trước khi chiến dịch bắt đầu.");
        var funding = await db.CampaignFundings.Where(x => x.CampaignId == row.Campaign.Id).OrderByDescending(x => x.VersionNo).FirstOrDefaultAsync(cancellationToken);
        if (funding is not null && funding.Status != "Draft") return new(false, "Đơn quỹ đã được chốt. Hãy hủy đơn chờ thanh toán trước, hoặc mở tranh chấp nếu chiến dịch đã thanh toán.");
        var participation = await db.Participations.FirstOrDefaultAsync(x => x.CampaignId == row.Campaign.Id && x.KocProfileId == row.Application.KocProfileId && x.Status == "Confirmed", cancellationToken);
        if (participation is null) return new(false, "KOC không còn trong danh sách cộng tác đang hiệu lực.");
        var now = DateTime.UtcNow;
        participation.Status = "Removed"; participation.UpdatedAtUtc = now;
        AddParticipationHistory(participation.Id, "Confirmed", "Removed", reason, brandUserId, now);
        row.Application.Status = "Shortlisted"; row.Application.UpdatedAtUtc = now;
        AddApplicationHistory(row.Application.Id, "Confirmed", "Shortlisted", reason, brandUserId, now);
        var shortlist = await db.Shortlists.FirstOrDefaultAsync(x => x.CampaignId == row.Campaign.Id, cancellationToken);
        if (shortlist is not null) { shortlist.FinalizedAtUtc = null; shortlist.FinalizedByUserId = null; shortlist.UpdatedAtUtc = now; }
        if (funding is not null) { var allocations = await db.CampaignFundingAllocations.Where(x => x.FundingId == funding.Id).ToListAsync(cancellationToken); db.CampaignFundingAllocations.RemoveRange(allocations); funding.UpdatedAtUtc = now; funding.UpdatedByUserId = brandUserId; }
        AddAudit(brandUserId, participation.Id, "RemoveConfirmedKoc", new { applicationId, row.Campaign.Id, Reason = reason, FundingDraftReset = funding is not null });
        await db.SaveChangesAsync(cancellationToken); await transaction.CommitAsync(cancellationToken);
        return new(true, "Đã đưa KOC khỏi danh sách và mở lại shortlist. Hãy chọn KOC thay thế rồi lập lại bảng thù lao.", participation.Id);
    }

    private async Task<CampaignPolicySnapshot?> ResolvePolicyAsync(Guid campaignId, decimal budgetAmount, CancellationToken cancellationToken)
    {
        var campaign=await db.Campaigns.AsNoTracking().SingleOrDefaultAsync(c=>c.Id==campaignId,cancellationToken);
        if(campaign?.CheckoutAtCreation==true)return new(campaign.PackageCode,"Nhu cầu chiến dịch",campaign.BrandSelectionSnapshot,campaign.SelectionQuotaSnapshot);
        var purchased = await db.BrandPackagePurchases.AsNoTracking().Where(x => x.CampaignId == campaignId)
            .Select(x => new CampaignPolicySnapshot(x.PackageCodeSnapshot, x.PackageNameSnapshot, x.BrandSelectionEnabledSnapshot, x.SelectionQuotaSnapshot))
            .FirstOrDefaultAsync(cancellationToken);
        if (purchased is not null) return purchased;
        return await db.CampaignPackagePolicies.AsNoTracking()
            .Where(x => x.IsActive && !x.IsDeleted && x.MinBudgetAmount <= budgetAmount && (!x.MaxBudgetAmount.HasValue || budgetAmount <= x.MaxBudgetAmount.Value))
            .OrderByDescending(x => x.MinBudgetAmount)
            .Select(x => new CampaignPolicySnapshot(x.Code, x.Name, x.BrandSelectionEnabled, x.SelectionQuota))
            .FirstOrDefaultAsync(cancellationToken);
    }

    private sealed record CampaignPolicySnapshot(string Code, string Name, bool BrandSelectionEnabled, int? SelectionQuota);

    private async Task<Dictionary<Guid,TargetingMatchScore>> BuildMatchMapAsync(Guid campaignId,IEnumerable<Guid> requestedKocIds,CancellationToken ct)
    {
        var ids=requestedKocIds.Distinct().ToArray();var result=new Dictionary<Guid,TargetingMatchScore>();if(ids.Length==0)return result;
        var target=await db.CampaignTargetings.AsNoTracking().FirstOrDefaultAsync(x=>x.CampaignId==campaignId,ct);
        var provinces=await db.CampaignTargetProvinces.AsNoTracking().Where(x=>x.CampaignId==campaignId).Select(x=>x.ProvinceId).ToListAsync(ct);
        var categories=await db.CampaignTargetCategories.AsNoTracking().Where(x=>x.CampaignId==campaignId).Select(x=>x.CategoryId).ToListAsync(ct);
        var profiles=await db.KocProfiles.AsNoTracking().Where(x=>ids.Contains(x.Id)).Select(x=>new{x.Id,x.Gender,x.ProvinceId,Followers=Math.Max(x.Followers,db.KocSocialAccounts.Where(s=>s.KocProfileId==x.Id&&s.IsVisible).Max(s=>(int?)s.Followers)??0),Engagement=db.KocSocialAccounts.Where(s=>s.KocProfileId==x.Id&&s.IsVisible).Max(s=>(decimal?)s.AvgEngagementRate)}).ToListAsync(ct);
        var links=await db.KocProfileCategories.AsNoTracking().Where(x=>ids.Contains(x.KocProfileId)).ToListAsync(ct);
        foreach(var profile in profiles)
        {
            if(target is null){result[profile.Id]=new(100,[],[]);continue;}
            var ownCategories=links.Where(x=>x.KocProfileId==profile.Id).Select(x=>x.CategoryId).ToHashSet();
            result[profile.Id]=TargetingMatchRules.Calculate([
                new("Giới tính",target.Gender!="Any",string.Equals(target.Gender,profile.Gender,StringComparison.OrdinalIgnoreCase)),
                new("Follower",target.MinimumFollowers.HasValue||target.MaximumFollowers.HasValue,(!target.MinimumFollowers.HasValue||profile.Followers>=target.MinimumFollowers.Value)&&(!target.MaximumFollowers.HasValue||profile.Followers<=target.MaximumFollowers.Value)),
                new("Engagement",target.MinimumEngagementRate.HasValue||target.MaximumEngagementRate.HasValue,(!target.MinimumEngagementRate.HasValue||profile.Engagement.GetValueOrDefault()>=target.MinimumEngagementRate.Value)&&(!target.MaximumEngagementRate.HasValue||profile.Engagement.GetValueOrDefault()<=target.MaximumEngagementRate.Value)),
                new("Tỉnh/thành",provinces.Count>0,profile.ProvinceId.HasValue&&provinces.Contains(profile.ProvinceId.Value)),
                new("Nhóm nội dung",categories.Count>0,categories.Any(ownCategories.Contains))]);
        }
        return result;
    }
    private static TargetingMatchScore? ParseMatch(string? json){if(string.IsNullOrWhiteSpace(json))return null;try{return JsonSerializer.Deserialize<TargetingMatchScore>(json);}catch(JsonException){return null;}}

    private async Task AddSubmissionTasksAsync(ParticipationEntity participation, DateTime now, CancellationToken cancellationToken)
    {
        var deliverableIds = await db.CampaignDeliverables.AsNoTracking()
            .Where(x => x.CampaignId == participation.CampaignId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);
        foreach (var deliverableId in deliverableIds)
            db.Submissions.Add(new SubmissionEntity { Id = Guid.NewGuid(), ParticipationId = participation.Id, DeliverableId = deliverableId, Status = SubmissionStatuses.NotSubmitted, IsVisible = true, CreatedAtUtc = now });
    }

    private void AddAudit(Guid actor, Guid id, string action, object payload) => db.AuditLogs.Add(new AuditLogEntity { ActorUserId = actor, EntityName = "Matching", EntityId = id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = DateTime.UtcNow });
    private void AddApplicationHistory(Guid id, string? from, string to, string? note, Guid actor, DateTime now) => db.ApplicationStatusHistories.Add(new ApplicationStatusHistoryEntity { Id = Guid.NewGuid(), ApplicationId = id, FromStatus = from, ToStatus = to, Note = note, ActorUserId = actor, CreatedAtUtc = now });
    private void AddParticipationHistory(Guid id, string? from, string to, string? note, Guid actor, DateTime now) => db.ParticipationStatusHistories.Add(new ParticipationStatusHistoryEntity { Id = Guid.NewGuid(), ParticipationId = id, FromStatus = from, ToStatus = to, Note = note, ActorUserId = actor, CreatedAtUtc = now });
}

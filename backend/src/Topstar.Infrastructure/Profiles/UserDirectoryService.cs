using Topstar.Core.Common.Pagination;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Profiles;

public sealed class UserDirectoryService(TopstarDbContext db) : IUserDirectoryService
{
    public async Task<UserDirectoryPageResult> GetPageAsync(
        UserDirectoryPageRequest request,
        CancellationToken cancellationToken = default)
    {
        var isKoc = string.Equals(request.Type, "KOC", StringComparison.OrdinalIgnoreCase);
        var search = request.Search?.Trim();
        if (string.IsNullOrWhiteSpace(search)) search = null;
        var profileStatus = string.IsNullOrWhiteSpace(request.ProfileStatus) ? null : request.ProfileStatus.Trim();
        var userStatus = string.IsNullOrWhiteSpace(request.UserStatus) ? null : request.UserStatus.Trim();
        var location = string.IsNullOrWhiteSpace(request.Location) ? null : request.Location.Trim();
        var sort = UserDirectorySorts.Normalize(request.Sort);
        var skip = Math.Max(0, request.Skip);
        var take = PaginationDefaults.Normalize(request.Take);

        if (!isKoc)
        {
            var total = await (from p in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" select p.Id).CountAsync(cancellationToken);
            var query =
                from profile in db.BrandProfiles.AsNoTracking()
                join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
                let activeViolations = db.UserViolations.Count(x => x.UserId == user.Id && x.Status == "Active")
                where user.Status!="Disabled"
                      && (search == null || profile.CompanyName.Contains(search) || user.Email.Contains(search))
                      && (profileStatus == null || profile.Status == profileStatus)
                      && (userStatus == null || user.Status == userStatus)
                      && (location == null || (profile.Industry != null && profile.Industry.Contains(location)))
                      && (!request.HasViolation.HasValue || (request.HasViolation.Value ? activeViolations > 0 : activeViolations == 0))
                select new
                {
                    ProfileId = profile.Id,
                    UserId = user.Id,
                    profile.CompanyName,
                    user.Email,
                    profile.Industry,
                    ProfileStatus = profile.Status,
                    UserStatus = user.Status,
                    CampaignCount = db.Campaigns.Count(x => x.BrandProfileId == profile.Id),
                    ActiveCampaignCount = db.Campaigns.Count(x => x.BrandProfileId == profile.Id && x.Status != "Completed" && x.Status != "Rejected" && x.Status != "Cancelled"),
                    CompletedCampaignCount = db.Campaigns.Count(x => x.BrandProfileId == profile.Id && x.Status == "Completed"),
                    TotalCampaignBudget = db.Campaigns.Where(x => x.BrandProfileId == profile.Id).Sum(x => (decimal?)x.BudgetAmount) ?? 0,
                    ActiveViolationCount = activeViolations,
                    profile.CreatedAtUtc
                };
            var filtered = await query.CountAsync(cancellationToken);
            query = sort == UserDirectorySorts.Name
                ? query.OrderBy(x => x.CompanyName).ThenBy(x => x.ProfileId)
                : query.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.ProfileId);
            var rows = await query.Skip(skip).Take(take).ToListAsync(cancellationToken);
            var brands = rows.Select(x => new BrandDirectoryItem(
                x.ProfileId, x.UserId, x.CompanyName, x.Email, x.Industry, x.ProfileStatus, x.UserStatus,
                x.CampaignCount, x.ActiveCampaignCount, x.CompletedCampaignCount, x.TotalCampaignBudget, x.ActiveViolationCount, x.CreatedAtUtc)).ToList();
            return new UserDirectoryPageResult(brands, [], total, filtered);
        }

        var kocTotal = await (from p in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" select p.Id).CountAsync(cancellationToken);
        var kocQuery =
            from profile in db.KocProfiles.AsNoTracking()
            join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
            let activePenalty = db.UserViolations
                .Where(x => x.UserId == user.Id && x.Status == "Active")
                .Sum(x => (int?)x.PenaltyPoints) ?? 0
            where user.Status!="Disabled"
                  && (search == null || profile.DisplayName.Contains(search) || user.Email.Contains(search))
                  && (profileStatus == null || profile.Status == profileStatus)
                  && (userStatus == null || user.Status == userStatus)
                  && (location == null || (profile.Region != null && profile.Region.Contains(location)))
                  && (!request.HasViolation.HasValue || (request.HasViolation.Value ? activePenalty > 0 : activePenalty == 0))
            select new
            {
                ProfileId = profile.Id,
                UserId = user.Id,
                profile.DisplayName,
                user.Email,
                profile.Region,
                ProfileStatus = profile.Status,
                UserStatus = user.Status,
                profile.Followers,
                BaseScore = profile.Score,
                ActivePenalty = activePenalty,
                EffectiveScore = profile.Score - activePenalty < 0 ? 0 : profile.Score - activePenalty,
                CompletedCollaborationCount = db.Participations.Count(x => x.KocProfileId == profile.Id && x.Status == "Completed"),
                ActiveViolationCount = db.UserViolations.Count(x => x.UserId == user.Id && x.Status == "Active"),
                profile.CreatedAtUtc
            };
        var kocFiltered = await kocQuery.CountAsync(cancellationToken);
        kocQuery = sort switch
        {
            UserDirectorySorts.Name => kocQuery.OrderBy(x => x.DisplayName).ThenBy(x => x.ProfileId),
            UserDirectorySorts.Score => kocQuery.OrderByDescending(x => x.EffectiveScore)
                .ThenByDescending(x => x.CompletedCollaborationCount)
                .ThenByDescending(x => x.Followers)
                .ThenBy(x => x.ProfileId),
            _ => kocQuery.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.ProfileId)
        };
        var kocRows = await kocQuery.Skip(skip).Take(take).ToListAsync(cancellationToken);
        var kocs = kocRows.Select(x => new KocRankingItem(
            x.ProfileId, x.UserId, x.DisplayName, x.Email, x.Region, x.ProfileStatus, x.UserStatus,
            x.Followers, x.BaseScore, x.ActivePenalty, x.EffectiveScore, x.CompletedCollaborationCount,
            x.ActiveViolationCount, x.CreatedAtUtc)).ToList();
        return new UserDirectoryPageResult([], kocs, kocTotal, kocFiltered);
    }

    public async Task<UserDirectoryResult> GetAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken = default)
    {
        search = search?.Trim();
        status = string.IsNullOrWhiteSpace(status) ? null : status.Trim();
        sort = UserDirectorySorts.Normalize(sort);

        var brandQuery =
            from profile in db.BrandProfiles.AsNoTracking()
            join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
            where user.Status!="Disabled"
                  && (search == null || profile.CompanyName.Contains(search) || user.Email.Contains(search))
                  && (status == null || profile.Status == status)
            select new
            {
                ProfileId = profile.Id,
                UserId = user.Id,
                profile.CompanyName,
                user.Email,
                profile.Industry,
                ProfileStatus = profile.Status,
                UserStatus = user.Status,
                CampaignCount = db.Campaigns.Count(x => x.BrandProfileId == profile.Id),
                ActiveCampaignCount = db.Campaigns.Count(x => x.BrandProfileId == profile.Id && x.Status != "Completed" && x.Status != "Rejected" && x.Status != "Cancelled"),
                CompletedCampaignCount = db.Campaigns.Count(x => x.BrandProfileId == profile.Id && x.Status == "Completed"),
                TotalCampaignBudget = db.Campaigns.Where(x => x.BrandProfileId == profile.Id).Sum(x => (decimal?)x.BudgetAmount) ?? 0,
                ActiveViolationCount = db.UserViolations.Count(x => x.UserId == user.Id && x.Status == "Active"),
                profile.CreatedAtUtc
            };

        brandQuery = sort == UserDirectorySorts.Name
            ? brandQuery.OrderBy(x => x.CompanyName).ThenBy(x => x.ProfileId)
            : brandQuery.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.ProfileId);

        var kocQuery =
            from profile in db.KocProfiles.AsNoTracking()
            join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
            let activePenalty = db.UserViolations
                .Where(x => x.UserId == user.Id && x.Status == "Active")
                .Sum(x => (int?)x.PenaltyPoints) ?? 0
            where user.Status!="Disabled"
                  && (search == null || profile.DisplayName.Contains(search) || user.Email.Contains(search))
                  && (status == null || profile.Status == status)
            select new
            {
                ProfileId = profile.Id,
                UserId = user.Id,
                profile.DisplayName,
                user.Email,
                profile.Region,
                ProfileStatus = profile.Status,
                UserStatus = user.Status,
                profile.Followers,
                BaseScore = profile.Score,
                ActivePenalty = activePenalty,
                EffectiveScore = profile.Score - activePenalty < 0 ? 0 : profile.Score - activePenalty,
                CompletedCollaborationCount = db.Participations.Count(x => x.KocProfileId == profile.Id && x.Status == "Completed"),
                ActiveViolationCount = db.UserViolations.Count(x => x.UserId == user.Id && x.Status == "Active"),
                profile.CreatedAtUtc
            };

        kocQuery = sort switch
        {
            UserDirectorySorts.Name => kocQuery.OrderBy(x => x.DisplayName).ThenBy(x => x.ProfileId),
            UserDirectorySorts.Score => kocQuery.OrderByDescending(x => x.EffectiveScore)
                .ThenByDescending(x => x.CompletedCollaborationCount)
                .ThenByDescending(x => x.Followers)
                .ThenBy(x => x.ProfileId),
            _ => kocQuery.OrderByDescending(x => x.CreatedAtUtc).ThenBy(x => x.ProfileId)
        };

        var brandRows = await brandQuery.Take(150).ToListAsync(cancellationToken);
        var kocRows = await kocQuery.Take(150).ToListAsync(cancellationToken);
        var brands = brandRows.Select(x => new BrandDirectoryItem(
            x.ProfileId,
            x.UserId,
            x.CompanyName,
            x.Email,
            x.Industry,
            x.ProfileStatus,
            x.UserStatus,
            x.CampaignCount,
            x.ActiveCampaignCount,
            x.CompletedCampaignCount,
            x.TotalCampaignBudget,
            x.ActiveViolationCount,
            x.CreatedAtUtc)).ToList();
        var kocs = kocRows.Select(x => new KocRankingItem(
            x.ProfileId,
            x.UserId,
            x.DisplayName,
            x.Email,
            x.Region,
            x.ProfileStatus,
            x.UserStatus,
            x.Followers,
            x.BaseScore,
            x.ActivePenalty,
            x.EffectiveScore,
            x.CompletedCollaborationCount,
            x.ActiveViolationCount,
            x.CreatedAtUtc)).ToList();
        var userIds=brands.Select(x=>x.UserId).Concat(kocs.Select(x=>x.UserId)).Distinct().ToArray();
        var accountHistory=await(from h in db.UserAccountStatusHistories.AsNoTracking() join actor in db.Users.AsNoTracking() on h.ActorUserId equals actor.Id where userIds.Contains(h.UserId) orderby h.CreatedAtUtc select new{History=h,actor.Email}).ToListAsync(cancellationToken);
        brands=brands.Select(x=>x with{AccountHistory=accountHistory.Where(h=>h.History.UserId==x.UserId).Select(h=>new UserAccountStatusEvent(h.History.FromStatus,h.History.ToStatus,h.History.Reason,h.Email,h.History.CreatedAtUtc)).ToList()}).ToList();
        kocs=kocs.Select(x=>x with{AccountHistory=accountHistory.Where(h=>h.History.UserId==x.UserId).Select(h=>new UserAccountStatusEvent(h.History.FromStatus,h.History.ToStatus,h.History.Reason,h.Email,h.History.CreatedAtUtc)).ToList()}).ToList();
        // A scoped DbContext does not support concurrent operations.
        var totalBrands = await (from profile in db.BrandProfiles.AsNoTracking()
                                 join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
                                 where user.Status != "Disabled"
                                 select profile.Id).CountAsync(cancellationToken);
        var approvedBrands = await (from profile in db.BrandProfiles.AsNoTracking()
                                    join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
                                    where user.Status != "Disabled" && profile.Status == ProfileStatuses.Approved
                                    select profile.Id).CountAsync(cancellationToken);
        var totalKocs = await (from profile in db.KocProfiles.AsNoTracking()
                               join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
                               where user.Status != "Disabled"
                               select profile.Id).CountAsync(cancellationToken);
        var approvedKocs = await (from profile in db.KocProfiles.AsNoTracking()
                                  join user in db.Users.AsNoTracking() on profile.UserId equals user.Id
                                  where user.Status != "Disabled" && profile.Status == ProfileStatuses.Approved
                                  select profile.Id).CountAsync(cancellationToken);

        return new UserDirectoryResult(brands, kocs, totalBrands, approvedBrands, totalKocs, approvedKocs);
    }
}

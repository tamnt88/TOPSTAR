using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Profiles;

public sealed class KocProfileViewService(TopstarDbContext db) : IKocProfileViewService
{
    public async Task<KocProfileViewData?> GetAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var account = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new
        {
            User = x,
            Profile = db.KocProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == x.Id),
            ProvinceName = db.KocProfiles.Where(profile => profile.UserId == x.Id).Select(profile => db.Provinces.Where(p => p.Id == profile.ProvinceId).Select(p => p.Name).FirstOrDefault()).FirstOrDefault(),
            WardName = db.KocProfiles.Where(profile => profile.UserId == x.Id).Select(profile => db.Wards.Where(w => w.Id == profile.WardId).Select(w => w.Name).FirstOrDefault()).FirstOrDefault()
        }).FirstOrDefaultAsync(cancellationToken);
        if (account?.Profile is null) return null;
        var profile = account.Profile;
        var socialEntities = await db.KocSocialAccounts.AsNoTracking().Where(x => x.KocProfileId == profile.Id).OrderByDescending(x => x.Followers).ToListAsync(cancellationToken);
        var socialIds = socialEntities.Select(x => x.Id).ToArray();
        Dictionary<Guid, List<string>> categories = new();
        if (socialIds.Length > 0)
            try
            {
                var rows = await (from link in db.KocSocialAccountCategories.AsNoTracking()
                    join category in db.KocCategories.AsNoTracking() on link.CategoryId equals category.Id
                    where socialIds.Contains(link.SocialAccountId) select new { link.SocialAccountId, category.Name }).ToListAsync(cancellationToken);
                categories = rows.GroupBy(x => x.SocialAccountId).ToDictionary(x => x.Key, x => x.Select(y => y.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            }
            catch (SqlException ex) when (ex.Message.Contains("KocSocialAccountCategories", StringComparison.OrdinalIgnoreCase)) { }
        var socials = socialEntities.Select(x => new KocProfileSocialItem(x.Platform, x.Handle, x.ChannelUrl, x.Followers, x.AvgEngagementRate, x.AvgViews, categories.GetValueOrDefault(x.Id, []))).ToList();
        var body = await db.KocBodyProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == profile.Id, cancellationToken);
        var children = body is null ? [] : await db.KocChildren.AsNoTracking().Where(x => x.KocBodyProfileId == body.Id).OrderBy(x => x.AgeYears).ToListAsync(cancellationToken);
        var preference = await db.KocJobPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == profile.Id, cancellationToken);
        var capability = await db.KocProductionCapabilities.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == profile.Id, cancellationToken);
        var portfolio = await (from participation in db.Participations.AsNoTracking()
            join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
            where participation.KocProfileId == profile.Id
            orderby participation.UpdatedAtUtc ?? participation.CreatedAtUtc descending
            select new KocProfilePortfolioItem(campaign.Title, participation.Status)).Take(6).ToListAsync(cancellationToken);
        var connections = await db.SocialPlatformConnections.AsNoTracking().Where(x => x.UserId == userId).OrderBy(x => x.Platform)
            .Select(x => new KocPlatformConnectionItem(x.Platform, x.Status, x.LastSyncedAtUtc, x.LastError)).ToListAsync(cancellationToken);
        var media = new List<MediaRow>();
        try
        {
            media = await db.KocMediaAssets.AsNoTracking().Where(x => x.KocProfileId == profile.Id && x.IsVisible)
                .Select(x => new MediaRow(x.MediaType, x.FileUrl, x.SortOrder, x.CreatedAtUtc, x.UpdatedAtUtc)).ToListAsync(cancellationToken);
        }
        catch (SqlException ex) when (ex.Message.Contains("KocMediaAssets", StringComparison.OrdinalIgnoreCase)) { }
        var workModes = new List<string>();
        if (capability?.CanLivestream == true) workModes.Add("Livestream");
        if (capability?.CanScript == true) workModes.Add("Lên kịch bản");
        if (capability?.CanShoot == true) workModes.Add("Quay dựng");
        if (capability?.CanEdit == true) workModes.Add("Hậu kỳ");
        var totalFollowers = socials.Sum(x => x.Followers);
        return new KocProfileViewData
        {
            DisplayName = string.IsNullOrWhiteSpace(profile.DisplayName) ? "KOC" : profile.DisplayName, AvatarUrl = profile.AvatarUrl,
            Email = account.User.Email, Phone = account.User.Phone ?? "", GenderDisplay = MapGender(account.User.Gender), DateOfBirth = account.User.DateOfBirth,
            ProvinceName = account.ProvinceName ?? "", WardName = account.WardName ?? "", StreetAddress = profile.StreetAddress ?? "",
            HeightCm = body?.HeightCm, WeightKg = body?.WeightKg, BustCm = body?.BustCm, WaistCm = body?.WaistCm, HipCm = body?.HipCm,
            MaritalStatus = MapMaritalStatus(body?.MaritalStatus), HasChildren = body?.HasChildren,
            Children = children.Select(x => new KocProfileChildItem(x.AgeYears, MapGender(x.Gender))).ToList(), WorkModes = workModes,
            JobTypes = ParseCsv(preference?.AvailabilityNote), PreferredRegions = ParseCsv(preference?.PreferredRegions), TotalFollowers = totalFollowers,
            TierLabel = totalFollowers >= 100000 ? "Premium" : totalFollowers >= 10000 ? "Advanced" : "Standard", SocialChannels = socials, Portfolios = portfolio,
            PlatformConnections = connections,
            GalleryImages = media.Where(x => x.Type == "GalleryImage").OrderBy(x => x.Sort).Select(x => new KocProfileMediaItem(x.Url)).ToList(),
            Videos = media.Where(x => x.Type == "GalleryVideo").OrderBy(x => x.Sort).Select(x => new KocProfileMediaItem(x.Url)).ToList(),
            ProfileDocumentUrl = media.Where(x => x.Type == "ProfileDocument").OrderByDescending(x => x.Updated ?? x.Created).Select(x => x.Url).FirstOrDefault()
        };
    }

    private static IReadOnlyList<string> ParseCsv(string? value) => string.IsNullOrWhiteSpace(value) ? [] : value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    private static string MapGender(string? value) => value?.Trim().ToLowerInvariant() switch { "male" => "Nam", "female" => "Nữ", "other" => "Khác", _ => "Chưa cập nhật" };
    private static string MapMaritalStatus(string? value) => value?.Trim().ToLowerInvariant() switch { "single" => "Độc thân", "married" => "Đã kết hôn", "other" => "Khác", _ => "Chưa cập nhật" };
    private sealed record MediaRow(string Type, string Url, int Sort, DateTime Created, DateTime? Updated);
}

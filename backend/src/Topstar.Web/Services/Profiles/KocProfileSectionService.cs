using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Profiles;

public sealed class KocProfileSectionService(TopstarDbContext db) : IKocProfileSectionService
{
    public async Task<KocProfileEditSnapshot?> GetEditSnapshotAsync(Guid userId, IReadOnlyList<string> platforms, CancellationToken cancellationToken = default)
    {
        var account = await db.Users.AsNoTracking().Where(x => x.Id == userId).Select(x => new
        {
            User = x,
            Profile = db.KocProfiles.AsNoTracking().FirstOrDefault(profile => profile.UserId == x.Id)
        }).FirstOrDefaultAsync(cancellationToken);
        if (account?.Profile is null) return null;
        var profile = account.Profile;
        var body = await db.KocBodyProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == profile.Id, cancellationToken);
        var children = body is null ? [] : await db.KocChildren.AsNoTracking().Where(x => x.KocBodyProfileId == body.Id).ToListAsync(cancellationToken);
        var socials = await db.KocSocialAccounts.AsNoTracking().Where(x => x.KocProfileId == profile.Id).ToListAsync(cancellationToken);
        var socialIds = socials.Select(x => x.Id).ToArray();
        Dictionary<Guid, List<string>> categoryMap = new();
        List<string> legacyCategories = [];
        if (socialIds.Length > 0)
            try
            {
                var links = await (from link in db.KocSocialAccountCategories.AsNoTracking()
                    join category in db.KocCategories.AsNoTracking() on link.CategoryId equals category.Id
                    where socialIds.Contains(link.SocialAccountId)
                    select new { link.SocialAccountId, category.Code }).ToListAsync(cancellationToken);
                categoryMap = links.GroupBy(x => x.SocialAccountId).ToDictionary(x => x.Key, x => x.Select(y => y.Code).Distinct(StringComparer.OrdinalIgnoreCase).ToList());
            }
            catch (SqlException ex) when (ex.Message.Contains("KocSocialAccountCategories", StringComparison.OrdinalIgnoreCase))
            {
                legacyCategories = await (from link in db.KocProfileCategories.AsNoTracking()
                    join category in db.KocCategories.AsNoTracking() on link.CategoryId equals category.Id
                    where link.KocProfileId == profile.Id select category.Code).Distinct().ToListAsync(cancellationToken);
            }
        var preference = await db.KocJobPreferences.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == profile.Id, cancellationToken);
        List<KocMediaAssetEntity> media;
        try { media = await db.KocMediaAssets.AsNoTracking().Where(x => x.KocProfileId == profile.Id && x.IsVisible).ToListAsync(cancellationToken); }
        catch (SqlException ex) when (ex.Message.Contains("KocMediaAssets", StringComparison.OrdinalIgnoreCase)) { media = []; }
        var socialItems = platforms.Select(platform =>
        {
            var social = socials.FirstOrDefault(x => x.Platform.Equals(platform, StringComparison.OrdinalIgnoreCase));
            return new KocSocialEditItem(platform, social?.ChannelUrl, social?.Followers,
                social is null ? [] : categoryMap.GetValueOrDefault(social.Id, legacyCategories));
        }).ToList();
        return new(
            profile.DisplayName, account.User.Email, account.User.Phone ?? "", account.User.Gender ?? "", account.User.DateOfBirth,
            profile.AvatarUrl, profile.StreetAddress ?? "", profile.ProvinceId, profile.WardId,
            body?.WeightKg, body?.HeightCm, body?.BustCm, body?.WaistCm, body?.HipCm, body?.MaritalStatus, body?.HasChildren ?? false,
            preference?.MinBudgetAmount,
            children.Select(x => new KocChildSectionItem(x.AgeYears, x.Gender)).ToList(), socialItems,
            media.Where(x => x.MediaType == "GalleryImage").OrderBy(x => x.SortOrder).Select(x => new KocMediaEditItem(x.Id, x.FileUrl, x.SortOrder)).ToList(),
            media.Where(x => x.MediaType == "GalleryVideo").OrderBy(x => x.SortOrder).Select(x => new KocMediaEditItem(x.Id, x.FileUrl, x.SortOrder)).ToList(),
            media.Where(x => x.MediaType == "ProfileDocument").OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc).Select(x => x.FileUrl).FirstOrDefault(),
            profile.UpdatedAtUtc);
    }

    public Task<Guid?> GetKocProfileIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => db.KocProfiles.AsNoTracking().Where(x => x.UserId == userId).Select(x => (Guid?)x.Id).FirstOrDefaultAsync(cancellationToken);

    public async Task SaveBasicAsync(Guid userId, KocBasicSectionInput input, CancellationToken cancellationToken = default)
    {
        var account = await db.Users.Where(x => x.Id == userId).Select(x => new
        {
            User = x,
            Profile = db.KocProfiles.FirstOrDefault(profile => profile.UserId == x.Id)
        }).FirstOrDefaultAsync(cancellationToken);
        if (account?.Profile is null) throw new InvalidOperationException("Không tìm thấy hồ sơ KOC.");
        var now = DateTime.UtcNow;
        account.User.Email = input.Email.Trim();
        account.User.Phone = input.Phone.Trim();
        account.User.Gender = input.Gender;
        account.User.DateOfBirth = input.DateOfBirth;
        account.User.UpdatedAtUtc = now;
        account.Profile.DisplayName = input.FullName.Trim();
        account.Profile.StreetAddress = input.StreetAddress.Trim();
        account.Profile.ProvinceId = input.ProvinceId;
        account.Profile.WardId = input.WardId;
        account.Profile.Status = "PendingReview";
        account.Profile.UpdatedAtUtc = now;
        if (!string.IsNullOrWhiteSpace(input.AvatarUrl)) account.Profile.AvatarUrl = input.AvatarUrl;
        if (string.IsNullOrWhiteSpace(input.ProfileDocumentUrl)) return;
        var document = await db.KocMediaAssets.FirstOrDefaultAsync(x => x.KocProfileId == account.Profile.Id && x.MediaType == "ProfileDocument" && x.IsVisible, cancellationToken);
        if (document is null)
            db.KocMediaAssets.Add(new KocMediaAssetEntity { Id = Guid.NewGuid(), KocProfileId = account.Profile.Id, MediaType = "ProfileDocument", FileUrl = input.ProfileDocumentUrl, SortOrder = 1, IsVisible = true, CreatedAtUtc = now });
        else
        {
            document.FileUrl = input.ProfileDocumentUrl;
            document.UpdatedAtUtc = now;
        }
    }

    public async Task SaveMediaAsync(Guid kocProfileId, string mediaType, IReadOnlyList<Guid> orderIds, IReadOnlyList<Guid> deleteIds, IReadOnlyList<string> newFileUrls, CancellationToken cancellationToken = default)
    {
        if (mediaType is not ("GalleryImage" or "GalleryVideo")) throw new ArgumentOutOfRangeException(nameof(mediaType));
        var rows = await db.KocMediaAssets
            .Where(x => x.KocProfileId == kocProfileId && x.MediaType == mediaType)
            .ToListAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var deleteSet = deleteIds.ToHashSet();
        foreach (var row in rows.Where(x => deleteSet.Contains(x.Id)))
        {
            row.IsVisible = false;
            row.UpdatedAtUtc = now;
        }
        var rowMap = rows.ToDictionary(x => x.Id);
        for (var index = 0; index < orderIds.Count; index++)
            if (rowMap.TryGetValue(orderIds[index], out var row) && !deleteSet.Contains(row.Id))
            {
                row.SortOrder = index + 1;
                row.UpdatedAtUtc = now;
            }
        var maxSort = rows.Count == 0 ? 0 : rows.Max(x => x.SortOrder);
        foreach (var fileUrl in newFileUrls.Where(x => !string.IsNullOrWhiteSpace(x)))
            db.KocMediaAssets.Add(new KocMediaAssetEntity
            {
                Id = Guid.NewGuid(),
                KocProfileId = kocProfileId,
                MediaType = mediaType,
                FileUrl = fileUrl,
                SortOrder = ++maxSort,
                IsVisible = true,
                CreatedAtUtc = now
            });
    }

    public async Task SavePersonalAsync(Guid kocProfileId, KocPersonalSectionInput input, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        var body = await db.KocBodyProfiles.FirstOrDefaultAsync(x => x.KocProfileId == kocProfileId, cancellationToken);
        if (body is null)
        {
            body = new KocBodyProfileEntity { Id = Guid.NewGuid(), KocProfileId = kocProfileId, CreatedAtUtc = now };
            db.KocBodyProfiles.Add(body);
        }
        body.WeightKg = input.WeightKg;
        body.HeightCm = input.HeightCm;
        // Measurements are retained only as historical data. They are no longer
        // collected or updated by the KOC profile workflow.
        body.MaritalStatus = input.MaritalStatus;
        body.HasChildren = input.HasChildren;
        body.ChildrenCount = input.HasChildren ? input.Children.Count : 0;
        body.UpdatedAtUtc = now;

        var existingChildren = await db.KocChildren.Where(x => x.KocBodyProfileId == body.Id).ToListAsync(cancellationToken);
        db.KocChildren.RemoveRange(existingChildren);
        if (input.HasChildren)
            foreach (var child in input.Children)
                db.KocChildren.Add(new KocChildEntity
                {
                    Id = Guid.NewGuid(),
                    KocBodyProfileId = body.Id,
                    AgeYears = child.Age,
                    Gender = child.Gender,
                    BirthYear = child.Age.HasValue ? now.Year - child.Age.Value : null,
                    CreatedAtUtc = now
                });

        var preference = await db.KocJobPreferences.FirstOrDefaultAsync(x => x.KocProfileId == kocProfileId, cancellationToken);
        if (preference is null)
        {
            preference = new KocJobPreferenceEntity { Id = Guid.NewGuid(), KocProfileId = kocProfileId, CreatedAtUtc = now };
            db.KocJobPreferences.Add(preference);
        }
        preference.MinBudgetAmount = input.ServicePriceVnd;
        preference.UpdatedAtUtc = now;
    }

    public async Task SaveSocialAsync(Guid kocProfileId, IReadOnlyList<KocSocialSectionItem> items, CancellationToken cancellationToken = default)
    {
        var existingSocial = await db.KocSocialAccounts.Where(x => x.KocProfileId == kocProfileId).ToListAsync(cancellationToken);
        var existingSocialIds = existingSocial.Select(x => x.Id).ToList();
        var useLegacyCategoryTable = false;
        try
        {
            var existingCategories = await db.KocSocialAccountCategories
                .Where(x => existingSocialIds.Contains(x.SocialAccountId))
                .ToListAsync(cancellationToken);
            db.KocSocialAccountCategories.RemoveRange(existingCategories);
        }
        catch (SqlException ex) when (ex.Message.Contains("KocSocialAccountCategories", StringComparison.OrdinalIgnoreCase))
        {
            useLegacyCategoryTable = true;
            var existingProfileCategories = await db.KocProfileCategories.Where(x => x.KocProfileId == kocProfileId).ToListAsync(cancellationToken);
            db.KocProfileCategories.RemoveRange(existingProfileCategories);
        }
        db.KocSocialAccounts.RemoveRange(existingSocial);

        var categoryMap = await db.KocCategories.AsNoTracking().ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);
        var legacyCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var now = DateTime.UtcNow;
        foreach (var item in items.Where(x => !string.IsNullOrWhiteSpace(x.ChannelUrl)))
        {
            var socialId = Guid.NewGuid();
            db.KocSocialAccounts.Add(new KocSocialAccountEntity
            {
                Id = socialId,
                KocProfileId = kocProfileId,
                Platform = item.Platform,
                ChannelUrl = item.ChannelUrl.Trim(),
                Followers = item.Followers,
                IsPrimary = item.Platform.Equals("TikTok", StringComparison.OrdinalIgnoreCase),
                IsVerified = false,
                ViewCount = 0,
                IsVisible = true,
                CreatedAtUtc = now
            });
            foreach (var code in item.CategoryCodes.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct(StringComparer.OrdinalIgnoreCase))
            {
                if (!categoryMap.TryGetValue(code, out var categoryId)) continue;
                if (useLegacyCategoryTable) legacyCodes.Add(code);
                else db.KocSocialAccountCategories.Add(new KocSocialAccountCategoryEntity { SocialAccountId = socialId, CategoryId = categoryId, CreatedAtUtc = now });
            }
        }
        if (!useLegacyCategoryTable) return;
        foreach (var code in legacyCodes)
            if (categoryMap.TryGetValue(code, out var categoryId))
                db.KocProfileCategories.Add(new KocProfileCategoryEntity { KocProfileId = kocProfileId, CategoryId = categoryId, CreatedAtUtc = now });
    }
}

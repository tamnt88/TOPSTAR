using Microsoft.EntityFrameworkCore;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Profiles;

public sealed class ProfileAccessService(TopstarDbContext db) : IProfileAccessService
{
    private readonly Dictionary<Guid, ProfileAccessResult> brandRequestCache = new();
    private readonly Dictionary<Guid, ProfileAccessResult> kocRequestCache = new();

    public async Task<ProfileAccessResult> CheckBrandAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (brandRequestCache.TryGetValue(userId, out var cached)) return cached;
        var data = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Email,
                x.Phone,
                Profile = db.BrandProfiles.AsNoTracking().FirstOrDefault(p => p.UserId == x.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data?.Profile is null)
        {
            var missingProfile = new ProfileAccessResult(false, false, false, "Missing", ["BrandProfile"]);
            brandRequestCache[userId] = missingProfile;
            return missingProfile;
        }

        var profile = data.Profile;
        var missing = new List<string>();
        AddIfMissing(missing, data.Email, "Email");
        AddIfMissing(missing, data.Phone, "Phone");
        AddIfMissing(missing, profile.CompanyName, "CompanyName");
        AddIfMissing(missing, profile.ContactName, "ContactName");
        AddIfMissing(missing, profile.ContactPhone, "ContactPhone");
        AddIfMissing(missing, profile.StreetAddress, "StreetAddress");
        if (!profile.ProvinceId.HasValue) missing.Add("ProvinceId");
        if (!profile.WardId.HasValue) missing.Add("WardId");
        AddIfMissing(missing, profile.TaxCode, "TaxCode");
        AddIfMissing(missing, profile.TaxCompanyName, "TaxCompanyName");
        AddIfMissing(missing, profile.TaxInvoiceAddress, "TaxInvoiceAddress");
        AddIfMissing(missing, profile.BusinessLicenseFileUrl, "BusinessLicenseFileUrl");

        var result = Build(profile.Status, missing);
        brandRequestCache[userId] = result;
        return result;
    }

    public async Task<ProfileAccessResult> CheckKocAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        if (kocRequestCache.TryGetValue(userId, out var cached)) return cached;
        var data = await db.Users.AsNoTracking()
            .Where(x => x.Id == userId)
            .Select(x => new
            {
                x.Email,
                x.Phone,
                x.Gender,
                x.DateOfBirth,
                Profile = db.KocProfiles.AsNoTracking().FirstOrDefault(p => p.UserId == x.Id)
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (data?.Profile is null)
        {
            var missingProfile = new ProfileAccessResult(false, false, false, "Missing", ["KocProfile"]);
            kocRequestCache[userId] = missingProfile;
            return missingProfile;
        }

        var profile = data.Profile;
        var body = await db.KocBodyProfiles.AsNoTracking()
            .FirstOrDefaultAsync(x => x.KocProfileId == profile.Id, cancellationToken);
        var hasSocial = await db.KocSocialAccounts.AsNoTracking()
            .AnyAsync(x => x.KocProfileId == profile.Id && x.ChannelUrl != null && x.ChannelUrl != "", cancellationToken);
        var categoryCount = await (
            from social in db.KocSocialAccounts.AsNoTracking()
            join category in db.KocSocialAccountCategories.AsNoTracking() on social.Id equals category.SocialAccountId
            where social.KocProfileId == profile.Id
            select category.CategoryId)
            .Distinct()
            .CountAsync(cancellationToken);
        if (categoryCount == 0)
        {
            categoryCount = await db.KocProfileCategories.AsNoTracking()
                .CountAsync(x => x.KocProfileId == profile.Id, cancellationToken);
        }

        var missing = new List<string>();
        AddIfMissing(missing, data.Email, "Email");
        AddIfMissing(missing, data.Phone, "Phone");
        AddIfMissing(missing, data.Gender, "Gender");
        if (!data.DateOfBirth.HasValue) missing.Add("DateOfBirth");
        AddIfMissing(missing, profile.DisplayName, "DisplayName");
        AddIfMissing(missing, profile.StreetAddress, "StreetAddress");
        if (!profile.ProvinceId.HasValue) missing.Add("ProvinceId");
        if (!profile.WardId.HasValue) missing.Add("WardId");
        if (body is null) missing.Add("BodyProfile");
        else
        {
            if (!body.WeightKg.HasValue) missing.Add("WeightKg");
            if (!body.HeightCm.HasValue) missing.Add("HeightCm");
            AddIfMissing(missing, body.MaritalStatus, "MaritalStatus");
        }
        if (!hasSocial) missing.Add("SocialAccount");
        if (categoryCount < 3) missing.Add("Categories");

        var result = Build(profile.Status, missing);
        kocRequestCache[userId] = result;
        return result;
    }

    private static ProfileAccessResult Build(string status, IReadOnlyList<string> missing)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(status) ? "Draft" : status;
        return new(true, missing.Count == 0, string.Equals(normalizedStatus, "Approved", StringComparison.OrdinalIgnoreCase), normalizedStatus, missing);
    }

    private static void AddIfMissing(ICollection<string> missing, string? value, string field)
    {
        if (string.IsNullOrWhiteSpace(value)) missing.Add(field);
    }
}

using Microsoft.EntityFrameworkCore;
using Topstar.Application.KocRegistration;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Koc;

internal sealed class KocRegistrationService(TopstarDbContext dbContext) : IKocRegistrationService
{
    public async Task<IReadOnlyList<ProvinceOption>> GetActiveProvincesAsync(CancellationToken cancellationToken = default)
        => await dbContext.Provinces
            .AsNoTracking()
            .Where(x => x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new ProvinceOption(x.Id, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<WardOption>> GetActiveWardsByProvinceAsync(Guid provinceId, CancellationToken cancellationToken = default)
        => await dbContext.Wards
            .AsNoTracking()
            .Where(x => x.ProvinceId == provinceId && x.IsActive)
            .OrderBy(x => x.Name)
            .Select(x => new WardOption(x.Id, x.Name))
            .ToListAsync(cancellationToken);

    public Task<bool> IsValidProvinceAsync(Guid provinceId, CancellationToken cancellationToken = default)
        => dbContext.Provinces
            .AsNoTracking()
            .AnyAsync(x => x.Id == provinceId && x.IsActive, cancellationToken);

    public Task<bool> IsValidWardAsync(Guid wardId, Guid? provinceId, CancellationToken cancellationToken = default)
        => dbContext.Wards
            .AsNoTracking()
            .AnyAsync(x => x.Id == wardId && x.IsActive && (!provinceId.HasValue || x.ProvinceId == provinceId.Value), cancellationToken);

    public async Task<IReadOnlyList<KocCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default)
        => await dbContext.KocCategories
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .Select(x => new KocCategoryOption(x.Code, x.Name))
            .ToListAsync(cancellationToken);

    public async Task<CompleteKocRegistrationProfileResult> CompleteProfileAsync(CompleteKocRegistrationProfileRequest request, CancellationToken cancellationToken = default)
    {
        var kocProfile = await dbContext.KocProfiles.FirstOrDefaultAsync(x => x.UserId == request.UserId, cancellationToken);
        if (kocProfile is null)
        {
            return new CompleteKocRegistrationProfileResult(false, "Không tìm thấy hồ sơ KOC.");
        }

        kocProfile.StreetAddress = request.StreetAddress;
        kocProfile.ProvinceId = request.ProvinceId;
        kocProfile.WardId = request.WardId;
        kocProfile.UpdatedAtUtc = DateTime.UtcNow;

        var bodyProfile = await dbContext.KocBodyProfiles.FirstOrDefaultAsync(x => x.KocProfileId == kocProfile.Id, cancellationToken);
        if (bodyProfile is null)
        {
            bodyProfile = new KocBodyProfileEntity
            {
                Id = Guid.NewGuid(),
                KocProfileId = kocProfile.Id,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.KocBodyProfiles.Add(bodyProfile);
        }

        bodyProfile.WeightKg = request.WeightKg;
        bodyProfile.HeightCm = request.HeightCm;
        bodyProfile.MaritalStatus = request.MaritalStatus;
        bodyProfile.HasChildren = request.HasChildren;
        bodyProfile.ChildrenCount = request.HasChildren ? request.Children.Count : 0;
        bodyProfile.UpdatedAtUtc = DateTime.UtcNow;

        var existingChildren = dbContext.KocChildren.Where(x => x.KocBodyProfileId == bodyProfile.Id);
        dbContext.KocChildren.RemoveRange(existingChildren);

        if (request.HasChildren)
        {
            foreach (var child in request.Children)
            {
                dbContext.KocChildren.Add(new KocChildEntity
                {
                    Id = Guid.NewGuid(),
                    KocBodyProfileId = bodyProfile.Id,
                    AgeYears = child.Age,
                    Gender = child.Gender,
                    BirthYear = child.Age.HasValue ? DateTime.UtcNow.Year - child.Age.Value : null,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        var existingSocial = dbContext.KocSocialAccounts.Where(x => x.KocProfileId == kocProfile.Id);
        dbContext.KocSocialAccounts.RemoveRange(existingSocial);
        var existingProfileCategories = dbContext.KocProfileCategories.Where(x => x.KocProfileId == kocProfile.Id);
        dbContext.KocProfileCategories.RemoveRange(existingProfileCategories);

        var categoryMap = await dbContext.KocCategories
            .AsNoTracking()
            .ToDictionaryAsync(x => x.Code, x => x.Id, StringComparer.OrdinalIgnoreCase, cancellationToken);

        var allCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var social in request.SocialAccounts)
        {
            if (string.IsNullOrWhiteSpace(social.ChannelUrl))
            {
                continue;
            }

            dbContext.KocSocialAccounts.Add(new KocSocialAccountEntity
            {
                Id = Guid.NewGuid(),
                KocProfileId = kocProfile.Id,
                Platform = social.Platform,
                ChannelUrl = social.ChannelUrl.Trim(),
                Followers = social.Followers ?? 0,
                IsPrimary = social.Platform.Equals("TikTok", StringComparison.OrdinalIgnoreCase),
                IsVerified = false,
                ViewCount = 0,
                IsVisible = true,
                CreatedAtUtc = DateTime.UtcNow
            });

            foreach (var code in social.SelectedCategoryCodes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                allCodes.Add(code.Trim());
            }
        }

        foreach (var code in allCodes)
        {
            if (categoryMap.TryGetValue(code, out var categoryId))
            {
                dbContext.KocProfileCategories.Add(new KocProfileCategoryEntity
                {
                    KocProfileId = kocProfile.Id,
                    CategoryId = categoryId,
                    CreatedAtUtc = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return new CompleteKocRegistrationProfileResult(true, "OK");
    }
}

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class ProfileModel(
    IProfileAccessService profileAccessService,
    TopstarDbContext dbContext) : BrandGuardedPageModel(profileAccessService)
{
    public BrandProfileView? Profile { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: true);
        if (redirect is not null)
        {
            return redirect;
        }

        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return RedirectToPage("/Public/Login");
        }

        var row = await (
            from brand in dbContext.BrandProfiles.AsNoTracking()
            join user in dbContext.Users.AsNoTracking() on brand.UserId equals user.Id
            where brand.UserId == userId
            select new
            {
                brand.Id,
                brand.CompanyName,
                brand.ContactName,
                brand.ContactPhone,
                user.Email,
                user.Phone,
                brand.StreetAddress,
                brand.ProvinceId,
                brand.WardId,
                brand.Industry,
                brand.Website,
                brand.LogoUrl,
                brand.TaxCode,
                brand.TaxCompanyName,
                brand.TaxInvoiceAddress,
                brand.BusinessLicenseFileUrl,
                brand.Status,
                brand.ReviewNote,
                brand.CreatedAtUtc,
                brand.UpdatedAtUtc
            }).FirstOrDefaultAsync(cancellationToken);

        if (row is null)
        {
            return RedirectToPage("/Brand/EditProfile");
        }

        var provinceName = row.ProvinceId.HasValue
            ? await dbContext.Provinces.AsNoTracking()
                .Where(x => x.Id == row.ProvinceId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;
        var wardName = row.WardId.HasValue
            ? await dbContext.Wards.AsNoTracking()
                .Where(x => x.Id == row.WardId.Value)
                .Select(x => x.Name)
                .FirstOrDefaultAsync(cancellationToken)
            : null;

        var campaignCount = await dbContext.Campaigns.AsNoTracking()
            .CountAsync(x => x.BrandProfileId == row.Id, cancellationToken);
        var activeCampaignCount = await dbContext.Campaigns.AsNoTracking()
            .CountAsync(x => x.BrandProfileId == row.Id &&
                             x.Status != "Completed" && x.Status != "Cancelled", cancellationToken);

        Profile = new BrandProfileView(
            row.CompanyName, row.ContactName, row.ContactPhone, row.Email, row.Phone,
            row.StreetAddress, wardName, provinceName, row.Industry, row.Website,
            row.LogoUrl,
            row.TaxCode, row.TaxCompanyName, row.TaxInvoiceAddress,
            row.BusinessLicenseFileUrl, LocalizeStatus(row.Status), StatusClass(row.Status),
            ShouldShowReviewNote(row.Status) ? row.ReviewNote : null, campaignCount, activeCampaignCount,
            row.CreatedAtUtc, row.UpdatedAtUtc);
        return Page();
    }

    private static string LocalizeStatus(string? status) => status?.ToLowerInvariant() switch
    {
        "approved" => "Đã duyệt",
        "pendingreview" => "Đang chờ duyệt",
        "needupdate" => "Cần bổ sung",
        "rejected" => "Đã từ chối",
        "locked" => "Đã khóa",
        "draft" => "Bản nháp",
        _ => "Đang cập nhật"
    };

    private static string StatusClass(string? status) => status?.ToLowerInvariant() switch
    {
        "approved" => "is-approved",
        "pendingreview" => "is-pending",
        "needupdate" or "rejected" or "locked" => "is-warning",
        _ => "is-draft"
    };

    private static bool ShouldShowReviewNote(string? status) => status?.ToLowerInvariant() is
        "needupdate" or "rejected" or "locked";

    public sealed record BrandProfileView(
        string CompanyName,
        string ContactName,
        string ContactPhone,
        string Email,
        string? Phone,
        string? StreetAddress,
        string? WardName,
        string? ProvinceName,
        string? Industry,
        string? Website,
        string? LogoUrl,
        string? TaxCode,
        string? TaxCompanyName,
        string? TaxInvoiceAddress,
        string? BusinessLicenseFileUrl,
        string StatusLabel,
        string StatusClass,
        string? ReviewNote,
        int CampaignCount,
        int ActiveCampaignCount,
        DateTime CreatedAtUtc,
        DateTime? UpdatedAtUtc);
}

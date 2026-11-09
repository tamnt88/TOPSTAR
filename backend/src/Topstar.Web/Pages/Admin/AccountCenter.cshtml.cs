using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class AccountCenterModel(IUserAccountAdminService accountService, IProfileReviewService profileReviewService, TopstarDbContext db) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Type { get; set; } = "KOC";
    [BindProperty(SupportsGet = true)] public Guid UserId { get; set; }
    [BindProperty] public Guid ReviewProfileId { get; set; }
    public UserAccountAdminDetail Detail { get; private set; } = null!;
    public Guid ProfileId { get; private set; }
    public string? Location { get; private set; }
    public int PerformanceCount { get; private set; }
    public decimal EffectiveScore { get; private set; }
    public int Followers { get; private set; }
    public string? AvatarUrl { get; private set; }
    public string? ProfileDocumentUrl { get; private set; }
    public string? Address { get; private set; }
    public string? ReviewNote { get; private set; }
    public string? Gender { get; private set; }
    public BodyInfo? Body { get; private set; }
    public ExperienceInfo? Experience { get; private set; }
    public CapabilityInfo? Capability { get; private set; }
    public PreferenceInfo? Preference { get; private set; }
    public IReadOnlyList<string> Categories { get; private set; } = [];
    public IReadOnlyList<PortfolioItem> Portfolio { get; private set; } = [];
    public IReadOnlyList<MediaItem> MediaAssets { get; private set; } = [];
    public IReadOnlyList<SocialItem> SocialAccounts { get; private set; } = [];
    public IReadOnlyList<CollaborationItem> Collaborations { get; private set; } = [];
    public IReadOnlyList<ViolationItem> Violations { get; private set; } = [];
    public BrandInfo? Brand { get; private set; }
    public IReadOnlyList<BrandCampaignItem> BrandCampaigns { get; private set; } = [];
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Type = string.Equals(Type, "KOC", StringComparison.OrdinalIgnoreCase) ? "KOC" : "Brand";
        var detail = await accountService.GetDetailAsync(UserId, cancellationToken);
        if (detail is null || !string.Equals(detail.ProfileType, Type, StringComparison.OrdinalIgnoreCase)) return NotFound();
        Detail = detail;

        if (Type == "KOC")
        {
            var profile = await db.KocProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == UserId, cancellationToken);
            if (profile is null) return NotFound();
            ProfileId = profile.Id;
            Location = profile.Region;
            Followers = profile.Followers;
            EffectiveScore = profile.Score;
            AvatarUrl = profile.AvatarUrl;
            ProfileDocumentUrl = await db.KocMediaAssets.AsNoTracking()
                .Where(x => x.KocProfileId == profile.Id && x.MediaType == "ProfileDocument" && x.IsVisible)
                .OrderByDescending(x => x.CreatedAtUtc).Select(x => x.FileUrl).FirstOrDefaultAsync(cancellationToken);
            Address = profile.AddressV2 ?? profile.StreetAddress;
            ReviewNote = profile.ReviewNote;
            Gender = profile.Gender;
            Body = await db.KocBodyProfiles.AsNoTracking().Where(x => x.KocProfileId == profile.Id)
                .Select(x => new BodyInfo(x.HeightCm, x.WeightKg, x.BustCm, x.WaistCm, x.HipCm, x.MaritalStatus, x.HasChildren, x.ChildrenCount)).FirstOrDefaultAsync(cancellationToken);
            Experience = await db.KocExperiences.AsNoTracking().Where(x => x.KocProfileId == profile.Id)
                .Select(x => new ExperienceInfo(x.YearsOfExperience, x.WorkedBrands, x.CaseStudyUrl)).FirstOrDefaultAsync(cancellationToken);
            Capability = await db.KocProductionCapabilities.AsNoTracking().Where(x => x.KocProfileId == profile.Id)
                .Select(x => new CapabilityInfo(x.CanScript, x.CanShoot, x.CanEdit, x.CanLivestream, x.Devices)).FirstOrDefaultAsync(cancellationToken);
            Preference = await db.KocJobPreferences.AsNoTracking().Where(x => x.KocProfileId == profile.Id)
                .Select(x => new PreferenceInfo(x.MinBudgetAmount, x.PreferredRegions, x.AvailabilityNote)).FirstOrDefaultAsync(cancellationToken);
            Categories = await (from selected in db.KocProfileCategories.AsNoTracking()
                                join category in db.KocCategories.AsNoTracking() on selected.CategoryId equals category.Id
                                where selected.KocProfileId == profile.Id orderby category.SortOrder select category.Name).ToListAsync(cancellationToken);
            Portfolio = await db.KocPortfolios.AsNoTracking().Where(x => x.KocProfileId == profile.Id && x.IsVisible)
                .OrderByDescending(x => x.CreatedAtUtc).Select(x => new PortfolioItem(x.Title, x.BrandName, x.CampaignName, x.PostUrl, x.Platform, x.Views, x.Likes, x.Comments, x.Shares)).Take(12).ToListAsync(cancellationToken);
            MediaAssets = await db.KocMediaAssets.AsNoTracking().Where(x => x.KocProfileId == profile.Id && x.IsVisible && x.MediaType != "ProfileDocument")
                .OrderBy(x => x.SortOrder).Select(x => new MediaItem(x.MediaType, x.FileUrl)).Take(12).ToListAsync(cancellationToken);
            SocialAccounts = await db.KocSocialAccounts.AsNoTracking().Where(x => x.KocProfileId == profile.Id)
                .OrderByDescending(x => x.IsPrimary).ThenByDescending(x => x.Followers)
                .Select(x => new SocialItem(x.Platform, x.Handle, x.ChannelUrl, x.Followers, x.AvgViews, x.AvgEngagementRate, x.IsVerified, x.IsPrimary))
                .ToListAsync(cancellationToken);
            Collaborations = await (from participation in db.Participations.AsNoTracking()
                                    join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                                    join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                                    where participation.KocProfileId == profile.Id
                                    orderby participation.CreatedAtUtc descending
                                    select new CollaborationItem(campaign.Title, brand.CompanyName, participation.Status, participation.CreatedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            PerformanceCount = Collaborations.Count(x => x.Status == "Completed");
        }
        else
        {
            var profile = await db.BrandProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.UserId == UserId, cancellationToken);
            if (profile is null) return NotFound();
            ProfileId = profile.Id;
            Location = profile.Industry;
            Address = profile.AddressV2 ?? profile.StreetAddress;
            ReviewNote = profile.ReviewNote;
            Brand = new(profile.CompanyName, profile.ContactName, profile.ContactPhone, profile.Industry, profile.Website,
                profile.TaxCompanyName, profile.TaxCode, profile.TaxInvoiceAddress, profile.BusinessLicenseFileUrl, profile.ViewCount, profile.IsVisible);
            BrandCampaigns = await db.Campaigns.AsNoTracking().Where(x => x.BrandProfileId == profile.Id)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new BrandCampaignItem(x.Id, x.Title, x.Status, x.BudgetAmount, x.DesiredKocCount, x.StartDate, x.EndDate, x.CreatedAtUtc))
                .Take(30).ToListAsync(cancellationToken);
            PerformanceCount = BrandCampaigns.Count(x => x.Status == "Completed");
            EffectiveScore = BrandCampaigns.Sum(x => x.Budget);
        }

        Violations = await db.UserViolations.AsNoTracking().Where(x => x.UserId == UserId)
            .OrderByDescending(x => x.CreatedAtUtc).Take(30)
            .Select(x => new ViolationItem(x.Category, x.Severity, x.PenaltyPoints, x.Status, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostVerifyEmailAsync(string reason, CancellationToken cancellationToken)
    {
        var result = await accountService.SetVerificationAsync(AdminId(), UserId, true, null, reason, cancellationToken);
        return Done(result);
    }

    public async Task<IActionResult> OnPostToggleLockAsync(bool locked, string reason, CancellationToken cancellationToken)
    {
        var result = await accountService.ChangeLockAsync(AdminId(), UserId, locked, reason, cancellationToken);
        return Done(result);
    }

    public async Task<IActionResult> OnPostRevokeSessionsAsync(string reason, CancellationToken cancellationToken)
    {
        var result = await accountService.RevokeSessionAsync(AdminId(), UserId, null, reason, cancellationToken);
        return Done(result);
    }

    public async Task<IActionResult> OnPostAddNoteAsync(string body, CancellationToken cancellationToken)
    {
        var result = await accountService.AddNoteAsync(AdminId(), UserId, body, cancellationToken);
        return Done(result);
    }

    public async Task<IActionResult> OnPostReviewProfileAsync(string decision, string reviewNote, CancellationToken cancellationToken)
    {
        var result = await profileReviewService.ReviewAsync(AdminId(), Type, ReviewProfileId, decision, reviewNote, cancellationToken);
        Message = result.Message;
        MessageSuccess = result.Success;
        return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { Type, UserId }, fragment: "profile");
    }

    private Guid AdminId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IActionResult Done(UserAccountAdminResult result)
    {
        Message = result.Message;
        MessageSuccess = result.Success;
        return RedirectToPage(pageName: null, pageHandler: null, routeValues: new { Type, UserId }, fragment: "security");
    }

    public sealed record ViolationItem(string Category, string Severity, int PenaltyPoints, string Status, DateTime CreatedAtUtc);
    public sealed record SocialItem(string Platform, string? Handle, string ChannelUrl, int Followers, int? AvgViews, decimal? EngagementRate, bool IsVerified, bool IsPrimary);
    public sealed record CollaborationItem(string CampaignName, string BrandName, string Status, DateTime CreatedAtUtc);
    public sealed record BodyInfo(decimal? HeightCm, decimal? WeightKg, decimal? BustCm, decimal? WaistCm, decimal? HipCm, string? MaritalStatus, bool HasChildren, int ChildrenCount);
    public sealed record ExperienceInfo(decimal? Years, string? WorkedBrands, string? CaseStudyUrl);
    public sealed record CapabilityInfo(bool CanScript, bool CanShoot, bool CanEdit, bool CanLivestream, string? Devices);
    public sealed record PreferenceInfo(decimal? MinBudget, string? Regions, string? Availability);
    public sealed record PortfolioItem(string Title, string? Brand, string? Campaign, string? Url, string? Platform, long Views, long Likes, long Comments, long Shares);
    public sealed record MediaItem(string Type, string Url);
    public sealed record BrandInfo(string CompanyName, string ContactName, string ContactPhone, string? Industry, string? Website,
        string? TaxCompanyName, string? TaxCode, string? TaxInvoiceAddress, string? BusinessLicenseUrl, long ViewCount, bool IsVisible);
    public sealed record BrandCampaignItem(Guid Id, string Title, string Status, decimal Budget, int DesiredKocCount, DateOnly? StartDate, DateOnly? EndDate, DateTime CreatedAtUtc);
}

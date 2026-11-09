using Topstar.Application.Campaigns;

namespace Topstar.Application.Matching;

public static class MatchingTransitions
{
    public static bool CanShortlist(string applicationStatus, string campaignStatus)
        => applicationStatus == ApplicationStatuses.Applied && campaignStatus is CampaignStatuses.Published or CampaignStatuses.InProgress;

    public static bool CanConfirm(string applicationStatus, string campaignStatus)
        => applicationStatus == "Shortlisted" && campaignStatus is CampaignStatuses.Published or CampaignStatuses.InProgress;

    public static bool CanBrandSelect(bool selectionEnabled, bool isFinalized, int confirmedCount, int? quota)
        => selectionEnabled && !isFinalized && (!quota.HasValue || confirmedCount < quota.Value);

    public static bool CanFinalize(bool selectionEnabled, bool isFinalized, int shortlistCount, int confirmedCount, int? quota)
        => !isFinalized && shortlistCount > 0
           && (!selectionEnabled || (confirmedCount > 0 && (!quota.HasValue || confirmedCount <= quota.Value)));
}

public static class ParticipationStatuses
{
    public const string Invited = "Invited";
    public const string Confirmed = "Confirmed";
    public const string Declined = "Declined";
}

public static class PackagePolicyRules
{
    private const decimal CurrencyStep = 0.01m;

    public static string? ValidateCoverage(IEnumerable<PackagePolicyRange> policies)
    {
        var active = policies.Where(x => x.IsActive).OrderBy(x => x.MinBudgetAmount).ToArray();
        if (active.Length == 0 || active[0].MinBudgetAmount != 0)
            return "Phải có policy hoạt động bắt đầu từ ngân sách 0.";

        for (var index = 0; index < active.Length - 1; index++)
        {
            var current = active[index];
            var next = active[index + 1];
            if (!current.MaxBudgetAmount.HasValue)
                return "Chỉ policy cuối cùng được phép không có ngân sách tối đa.";

            var expectedNextMin = current.MaxBudgetAmount.Value + CurrencyStep;
            if (next.MinBudgetAmount != expectedNextMin)
                return next.MinBudgetAmount < expectedNextMin
                    ? "Các khoảng ngân sách đang bị chồng lấn."
                    : "Các khoảng ngân sách đang có khoảng trống.";
        }

        return active[^1].MaxBudgetAmount.HasValue
            ? "Policy cuối cùng phải bao phủ ngân sách không giới hạn."
            : null;
    }
}

public sealed record MatchingCriterion(string Code,bool IsConfigured,bool IsMatched);
public sealed record TargetingMatchScore(decimal Score,IReadOnlyList<string> Matched,IReadOnlyList<string> Unmatched);
public static class TargetingMatchRules
{
    public static TargetingMatchScore Calculate(IEnumerable<MatchingCriterion> criteria)
    {
        var active=criteria.Where(x=>x.IsConfigured).ToArray();
        if(active.Length==0)return new(100,[],[]);
        var matched=active.Where(x=>x.IsMatched).Select(x=>x.Code).ToArray();
        var unmatched=active.Where(x=>!x.IsMatched).Select(x=>x.Code).ToArray();
        return new(Math.Round(100m*matched.Length/active.Length,2),matched,unmatched);
    }
}

public interface IMatchingService
{
    Task<IReadOnlyList<MatchingCampaignItem>> GetAdminCampaignsAsync(CancellationToken cancellationToken = default);
    Task<AdminMatchingDetail?> GetAdminDetailAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<MatchingResult> SendShortlistAsync(Guid adminUserId, Guid campaignId, IReadOnlyCollection<Guid> applicationIds, string? note, CancellationToken cancellationToken = default);
    Task<MatchingResult> AssignDirectAsync(Guid adminUserId, Guid campaignId, IReadOnlyCollection<Guid> kocProfileIds, string? note, CancellationToken cancellationToken = default);
    Task<MatchingResult> RemoveAssignedAsync(Guid adminUserId, Guid campaignId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<BrandShortlistDetail?> GetBrandShortlistAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default);
    Task<MatchingResult> ConfirmAsync(Guid brandUserId, Guid applicationId, CancellationToken cancellationToken = default);
    Task<MatchingResult> AcceptInvitationAsync(Guid kocUserId, Guid participationId, CancellationToken cancellationToken = default);
    Task<MatchingResult> DeclineInvitationAsync(Guid kocUserId, Guid participationId, string? reason, CancellationToken cancellationToken = default);
    Task<MatchingResult> FinalizeAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default);
    Task<MatchingResult> RemoveConfirmedAsync(Guid brandUserId, Guid applicationId, string reason, CancellationToken cancellationToken = default);
    Task<MatchingResult> RejectProposedAsync(Guid brandUserId, Guid applicationId, string reason, CancellationToken cancellationToken = default);
}

public interface IPackagePolicyAdminService
{
    Task<IReadOnlyList<PackagePolicyItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<PackagePolicyItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<PackagePolicyItem>> GetPublicAsync(CancellationToken cancellationToken = default);
    Task<MatchingResult> CreateAsync(Guid adminUserId, PackagePolicyCreateRequest request, CancellationToken cancellationToken = default);
    Task<MatchingResult> UpdateAsync(Guid adminUserId, PackagePolicyUpdateRequest request, CancellationToken cancellationToken = default);
    Task<MatchingResult> DeleteAsync(Guid adminUserId, Guid id, string reason, CancellationToken cancellationToken = default);
}

public sealed record MatchingCampaignItem(Guid CampaignId, string CampaignTitle, string BrandName, string Status, int AppliedCount, int ShortlistedCount, int ConfirmedCount);
public sealed record AdminMatchingDetail(Guid CampaignId, string CampaignTitle, string BrandName, string Status, IReadOnlyList<AdminApplicationItem> Applications)
{
    public int DesiredKocCount { get; init; } = 1;
    public int ShortlistedCount { get; init; }
    public bool BrandSelectionEnabled { get; init; }
    public IReadOnlyList<DirectKocCandidate> DirectCandidates { get; init; } = [];
}
public sealed record DirectKocCandidate(Guid KocProfileId, string KocName, int Followers, decimal Score, decimal TargetingMatchScore, IReadOnlyList<string> UnmatchedCriteria)
{
    public string? AvatarUrl { get; init; }
}
public sealed record AdminApplicationItem(Guid ApplicationId, string KocName, int Followers, decimal Score, string Status, string? PitchNote, DateTime AppliedAtUtc)
{
    public Guid KocProfileId { get; init; }
    public string? AvatarUrl { get; init; }
    public decimal TargetingMatchScore { get; init; }
    public decimal? ProposedAmount { get; init; }
    public IReadOnlyList<string> UnmatchedCriteria { get; init; } = [];
}
public sealed record BrandShortlistDetail(Guid CampaignId, string CampaignTitle, string Status, string PolicyName, bool BrandSelectionEnabled, int? SelectionQuota, bool IsFinalized, int ConfirmedCount, IReadOnlyList<BrandShortlistItem> Items)
{
    public int DesiredKocCount { get; init; } = 1;
}
public sealed record BrandShortlistItem(Guid ApplicationId, string AnonymousCode, int Followers, decimal Score, string Status, string? PitchNote, bool IsConfirmed){public decimal? TargetingMatchScore{get;init;}public decimal? ProposedAmount{get;init;}public IReadOnlyList<string> UnmatchedCriteria{get;init;}=[];}
public sealed record MatchingResult(bool Success, string Message, Guid? Id = null)
{
    public IReadOnlyList<Guid> RelatedIds { get; init; } = [];
}
public sealed record PackagePolicyHistoryItem(decimal MinBudgetAmount,decimal? MaxBudgetAmount,bool BrandSelectionEnabled,int? SelectionQuota,bool IsActive,string Reason,DateTime CreatedAtUtc)
{
    public decimal? ReferenceBudget {get;init;}
    public decimal SalePrice { get; init; }
    public string? ImageUrl { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Benefits { get; init; } = string.Empty;
    public int? ValidityDays { get; init; }
    public bool IsFeatured { get; init; }
    public int DisplayOrder { get; init; }
}
public sealed record PackagePolicyItem(Guid Id, string Code, string Name, decimal MinBudgetAmount, decimal? MaxBudgetAmount, bool BrandSelectionEnabled, int? SelectionQuota, bool IsActive)
{
    public bool ReferenceBudgetIsSuggested {get;init;}
    public decimal? ReferenceBudget {get;init;}
    public decimal SalePrice { get; init; }
    public string? ImageUrl { get; init; }
    public string Description { get; init; } = string.Empty;
    public string Benefits { get; init; } = string.Empty;
    public int? ValidityDays { get; init; }
    public bool IsFeatured { get; init; }
    public int DisplayOrder { get; init; }
    public IReadOnlyList<PackagePolicyHistoryItem> History{get;init;}=[];
}
public sealed record PackagePolicyRange(decimal MinBudgetAmount, decimal? MaxBudgetAmount, bool IsActive);
public sealed record PackagePolicyCreateRequest(string Code,string Name,decimal SalePrice,string? ImageUrl,string Description,string Benefits,int? ValidityDays,bool IsFeatured,int DisplayOrder,decimal MinBudgetAmount,decimal? MaxBudgetAmount,bool BrandSelectionEnabled,int? SelectionQuota,bool IsActive,string Reason){public decimal? ReferenceBudget{get;init;}}
public sealed record PackagePolicyUpdateRequest(Guid Id,string Name,decimal SalePrice,string? ImageUrl,string Description,string Benefits,int? ValidityDays,bool IsFeatured,int DisplayOrder,decimal MinBudgetAmount,decimal? MaxBudgetAmount,bool BrandSelectionEnabled,int? SelectionQuota,bool IsActive,string Reason){public decimal? ReferenceBudget{get;init;}}

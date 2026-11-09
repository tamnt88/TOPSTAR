namespace Topstar.Application.Profiles;

public interface IProfileAccessService
{
    Task<ProfileAccessResult> CheckBrandAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<ProfileAccessResult> CheckKocAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record ProfileAccessResult(
    bool Exists,
    bool IsComplete,
    bool IsApproved,
    string Status,
    IReadOnlyList<string> MissingFields)
{
    public bool CanUseFeatures => Exists && IsComplete && IsApproved;
    public bool CanBrowseFeatures => Exists && IsComplete && Status is ProfileStatuses.PendingReview or ProfileStatuses.Approved;
}

public static class ProfileStatuses
{
    public const string Draft = "Draft";
    public const string PendingReview = "PendingReview";
    public const string NeedUpdate = "NeedUpdate";
    public const string Approved = "Approved";
    public const string Rejected = "Rejected";
    public const string Locked = "Locked";
}

public static class ProfileTransitions
{
    public static bool CanReview(string status) => status is
        ProfileStatuses.PendingReview or
        ProfileStatuses.NeedUpdate or
        ProfileStatuses.Approved or
        ProfileStatuses.Rejected or
        ProfileStatuses.Locked;
    public static bool IsValidDecision(string decision) => decision is ProfileStatuses.Approved or ProfileStatuses.NeedUpdate or ProfileStatuses.Rejected;
}

public interface IProfileReviewService
{
    Task<IReadOnlyList<ProfileReviewListItem>> GetQueueAsync(string? profileType, CancellationToken cancellationToken = default);
    Task<ProfileReviewDetail?> GetDetailAsync(string profileType, Guid profileId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProfileStatusEvent>> GetHistoryAsync(string profileType, Guid profileId, CancellationToken cancellationToken = default);
    Task<ProfileReviewResult> ReviewAsync(Guid adminUserId, string profileType, Guid profileId, string decision, string? reviewNote, CancellationToken cancellationToken = default);
}

public sealed record ProfileReviewListItem(Guid Id, string ProfileType, string DisplayName, string Email, string Phone, string Status, DateTime CreatedAtUtc);

public sealed record ProfileReviewDetail(
    Guid Id,
    string ProfileType,
    string DisplayName,
    string Email,
    string Phone,
    string Status,
    string? ReviewNote,
    string? Address,
    string? ProvinceName,
    string? WardName,
    string? ExtraInformation,
    string? DocumentUrl,
    IReadOnlyList<string> Highlights,
    DateTime CreatedAtUtc,
    KocAdminProfileDetail? KocDetail = null);

public sealed record KocAdminProfileDetail(
    string? AvatarUrl,
    string? Gender,
    string? Region,
    int Followers,
    decimal Score,
    long ViewCount,
    bool IsVisible,
    KocBodyAdminDetail? Body,
    IReadOnlyList<string> Categories,
    IReadOnlyList<KocSocialAdminDetail> SocialAccounts,
    KocExperienceAdminDetail? Experience,
    KocProductionAdminDetail? Production,
    KocPreferenceAdminDetail? Preference,
    IReadOnlyList<KocPortfolioAdminDetail> Portfolio,
    IReadOnlyList<KocMediaAdminDetail> Media,
    KocHistoryAdminDetail History);

public sealed record KocBodyAdminDetail(
    decimal? HeightCm, decimal? WeightKg, decimal? BustCm, decimal? WaistCm,
    decimal? HipCm, string? MaritalStatus, bool HasChildren, int ChildrenCount,
    IReadOnlyList<KocChildAdminDetail> Children);
public sealed record KocChildAdminDetail(int? AgeYears, string? Gender, int? BirthYear);

public sealed record KocSocialAdminDetail(
    string Platform, string ChannelUrl, string? Handle, int Followers, int? AvgViews,
    decimal? AvgEngagementRate, bool IsPrimary, bool IsVerified, bool IsVisible);

public sealed record KocExperienceAdminDetail(decimal? Years, string? WorkedBrands, string? CaseStudyUrl);
public sealed record KocProductionAdminDetail(bool CanScript, bool CanShoot, bool CanEdit, bool CanLivestream, string? Devices);
public sealed record KocPreferenceAdminDetail(decimal? MinBudgetAmount, string? PreferredRegions, string? AvailabilityNote);
public sealed record KocPortfolioAdminDetail(string Title, string? BrandName, string? CampaignName, string? PostUrl, string? Platform, long Views, long Likes, long Comments, long Shares);
public sealed record KocMediaAdminDetail(string MediaType, string FileUrl, int SortOrder);
public sealed record KocHistoryAdminDetail(
    IReadOnlyList<KocHistoryItem> Collaborations,
    IReadOnlyList<KocHistoryItem> Applications,
    IReadOnlyList<KocHistoryItem> Reviews,
    IReadOnlyList<KocHistoryItem> Finance,
    IReadOnlyList<KocHistoryItem> Violations,
    IReadOnlyList<KocHistoryItem> Account,
    IReadOnlyList<KocHistoryItem> SocialSync);
public sealed record KocHistoryItem(string Title, string? Detail, string Status, decimal? Amount, DateTime OccurredAtUtc);

public sealed record ProfileStatusEvent(string? FromStatus, string ToStatus, string? ReviewNote, DateTime CreatedAtUtc);

public sealed record ProfileReviewResult(bool Success, string Message);

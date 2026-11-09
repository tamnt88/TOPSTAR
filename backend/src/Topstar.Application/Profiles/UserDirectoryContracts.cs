namespace Topstar.Application.Profiles;
public sealed record UserAccountStatusEvent(string FromStatus,string ToStatus,string Reason,string Actor,DateTime CreatedAtUtc);

public static class UserDirectorySorts
{
    public const string Newest = "newest";
    public const string Name = "name";
    public const string Score = "score";
    public static string Normalize(string? value) => value is Name or Score ? value : Newest;
}

public sealed record BrandDirectoryItem(
    Guid ProfileId,
    Guid UserId,
    string CompanyName,
    string Email,
    string? Industry,
    string ProfileStatus,
    string UserStatus,
    int CampaignCount,
    int ActiveCampaignCount,
    int CompletedCampaignCount,
    decimal TotalCampaignBudget,
    int ActiveViolationCount,
    DateTime CreatedAtUtc){public IReadOnlyList<UserAccountStatusEvent> AccountHistory{get;init;}=[];}

public sealed record KocRankingItem(
    Guid ProfileId,
    Guid UserId,
    string DisplayName,
    string Email,
    string? Region,
    string ProfileStatus,
    string UserStatus,
    int Followers,
    decimal BaseScore,
    int ActivePenalty,
    decimal EffectiveScore,
    int CompletedCollaborationCount,
    int ActiveViolationCount,
    DateTime CreatedAtUtc){public IReadOnlyList<UserAccountStatusEvent> AccountHistory{get;init;}=[];}

public sealed record UserDirectoryResult(
    IReadOnlyList<BrandDirectoryItem> Brands,
    IReadOnlyList<KocRankingItem> Kocs,
    int TotalBrands,
    int ApprovedBrands,
    int TotalKocs,
    int ApprovedKocs);

public sealed record UserDirectoryPageRequest(
    string Type,
    string? Search,
    string? ProfileStatus,
    string? UserStatus,
    string? Location,
    bool? HasViolation,
    string Sort,
    int Skip,
    int Take);

public sealed record UserDirectoryPageResult(
    IReadOnlyList<BrandDirectoryItem> Brands,
    IReadOnlyList<KocRankingItem> Kocs,
    int TotalCount,
    int FilteredCount);

public interface IUserDirectoryService
{
    Task<UserDirectoryResult> GetAsync(
        string? search,
        string? status,
        string? sort,
        CancellationToken cancellationToken = default);

    Task<UserDirectoryPageResult> GetPageAsync(
        UserDirectoryPageRequest request,
        CancellationToken cancellationToken = default);
}

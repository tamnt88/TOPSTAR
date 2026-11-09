namespace Topstar.Web.Services.Profiles;

public interface IKocProfileSectionService
{
    Task<KocProfileEditSnapshot?> GetEditSnapshotAsync(Guid userId, IReadOnlyList<string> platforms, CancellationToken cancellationToken = default);
    Task<Guid?> GetKocProfileIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task SaveBasicAsync(Guid userId, KocBasicSectionInput input, CancellationToken cancellationToken = default);
    Task SaveSocialAsync(Guid kocProfileId, IReadOnlyList<KocSocialSectionItem> items, CancellationToken cancellationToken = default);
    Task SavePersonalAsync(Guid kocProfileId, KocPersonalSectionInput input, CancellationToken cancellationToken = default);
    Task SaveMediaAsync(Guid kocProfileId, string mediaType, IReadOnlyList<Guid> orderIds, IReadOnlyList<Guid> deleteIds, IReadOnlyList<string> newFileUrls, CancellationToken cancellationToken = default);
}

public sealed record KocProfileEditSnapshot(
    string FullName, string Email, string Phone, string Gender, DateTime? DateOfBirth,
    string? AvatarUrl, string StreetAddress, Guid? ProvinceId, Guid? WardId,
    decimal? WeightKg, decimal? HeightCm, decimal? BustCm, decimal? WaistCm, decimal? HipCm,
    string? MaritalStatus, bool HasChildren, decimal? ServicePriceVnd,
    IReadOnlyList<KocChildSectionItem> Children, IReadOnlyList<KocSocialEditItem> Socials,
    IReadOnlyList<KocMediaEditItem> Gallery, IReadOnlyList<KocMediaEditItem> Videos, string? ProfileDocumentUrl,
    DateTime? ProfileUpdatedAtUtc);
public sealed record KocSocialEditItem(string Platform, string? ChannelUrl, int? Followers, IReadOnlyList<string> CategoryCodes);
public sealed record KocMediaEditItem(Guid Id, string FileUrl, int SortOrder);

public sealed record KocBasicSectionInput(string FullName, string Email, string Phone, string Gender, DateTime? DateOfBirth, string StreetAddress, Guid? ProvinceId, Guid? WardId, string? AvatarUrl, string? ProfileDocumentUrl);

public sealed record KocSocialSectionItem(string Platform, string ChannelUrl, int Followers, IReadOnlyList<string> CategoryCodes);
public sealed record KocPersonalSectionInput(
    decimal? WeightKg,
    decimal? HeightCm,
    decimal? BustCm,
    decimal? WaistCm,
    decimal? HipCm,
    string? MaritalStatus,
    bool HasChildren,
    decimal? ServicePriceVnd,
    IReadOnlyList<KocChildSectionItem> Children);
public sealed record KocChildSectionItem(int? Age, string? Gender);

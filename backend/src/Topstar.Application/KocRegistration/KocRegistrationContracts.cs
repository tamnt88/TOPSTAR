namespace Topstar.Application.KocRegistration;

public sealed record ProvinceOption(Guid Id, string Name);
public sealed record WardOption(Guid Id, string Name);
public sealed record KocCategoryOption(string Code, string Name);

public sealed record KocChildInput(int? Age, string? Gender);
public sealed record KocSocialInput(
    string Platform,
    string? ChannelUrl,
    int? Followers,
    IReadOnlyList<string> SelectedCategoryCodes);

public sealed record CompleteKocRegistrationProfileRequest(
    Guid UserId,
    string StreetAddress,
    Guid? ProvinceId,
    Guid? WardId,
    decimal? WeightKg,
    decimal? HeightCm,
    string? MaritalStatus,
    bool HasChildren,
    IReadOnlyList<KocChildInput> Children,
    IReadOnlyList<KocSocialInput> SocialAccounts);

public sealed record CompleteKocRegistrationProfileResult(bool Success, string Message);

public interface IKocRegistrationService
{
    Task<IReadOnlyList<ProvinceOption>> GetActiveProvincesAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WardOption>> GetActiveWardsByProvinceAsync(Guid provinceId, CancellationToken cancellationToken = default);
    Task<bool> IsValidProvinceAsync(Guid provinceId, CancellationToken cancellationToken = default);
    Task<bool> IsValidWardAsync(Guid wardId, Guid? provinceId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KocCategoryOption>> GetCategoriesAsync(CancellationToken cancellationToken = default);
    Task<CompleteKocRegistrationProfileResult> CompleteProfileAsync(CompleteKocRegistrationProfileRequest request, CancellationToken cancellationToken = default);
}

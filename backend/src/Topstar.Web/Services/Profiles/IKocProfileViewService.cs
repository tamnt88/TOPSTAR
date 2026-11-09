namespace Topstar.Web.Services.Profiles;

public interface IKocProfileViewService
{
    Task<KocProfileViewData?> GetAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class KocProfileViewData
{
    public string DisplayName { get; set; } = "KOC";
    public string? AvatarUrl { get; set; }
    public string Email { get; set; } = "";
    public string Phone { get; set; } = "";
    public string GenderDisplay { get; set; } = "";
    public DateTime? DateOfBirth { get; set; }
    public string ProvinceName { get; set; } = "";
    public string WardName { get; set; } = "";
    public string StreetAddress { get; set; } = "";
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? BustCm { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipCm { get; set; }
    public string MaritalStatus { get; set; } = "Chưa cập nhật";
    public bool? HasChildren { get; set; }
    public IReadOnlyList<KocProfileChildItem> Children { get; set; } = [];
    public IReadOnlyList<string> WorkModes { get; set; } = [];
    public IReadOnlyList<string> JobTypes { get; set; } = [];
    public IReadOnlyList<string> PreferredRegions { get; set; } = [];
    public int TotalFollowers { get; set; }
    public string TierLabel { get; set; } = "Standard";
    public IReadOnlyList<KocProfileSocialItem> SocialChannels { get; set; } = [];
    public IReadOnlyList<KocProfilePortfolioItem> Portfolios { get; set; } = [];
    public IReadOnlyList<KocProfileMediaItem> GalleryImages { get; set; } = [];
    public IReadOnlyList<KocProfileMediaItem> Videos { get; set; } = [];
    public string? ProfileDocumentUrl { get; set; }
    public IReadOnlyList<KocPlatformConnectionItem> PlatformConnections { get; set; } = [];
}

public sealed record KocProfileSocialItem(string Platform, string? Handle, string? ChannelUrl, int Followers, decimal? AvgEngagementRate, int? AvgViews, IReadOnlyList<string> Categories);
public sealed record KocProfilePortfolioItem(string CampaignTitle, string Status);
public sealed record KocProfileMediaItem(string FileUrl);
public sealed record KocProfileChildItem(int? Age, string GenderDisplay);
public sealed record KocPlatformConnectionItem(string Platform, string Status, DateTime? LastSyncedAtUtc, string? LastError);

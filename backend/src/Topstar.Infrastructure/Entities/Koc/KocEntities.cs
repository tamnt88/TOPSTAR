namespace Topstar.Infrastructure.Entities;

public sealed class BrandProfileEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string ContactPhone { get; set; } = string.Empty;
    public string? Industry { get; set; }
    public string? Website { get; set; }
    public string? LogoUrl { get; set; }
    public string? StreetAddress { get; set; }
    public Guid? WardId { get; set; }
    public Guid? ProvinceId { get; set; }
    public string? TaxCompanyName { get; set; }
    public string? TaxCode { get; set; }
    public string? TaxInvoiceAddress { get; set; }
    public string? RefundBankName { get; set; }
    public string? RefundAccountNumber { get; set; }
    public string? RefundAccountHolder { get; set; }
    public string? BusinessLicenseFileUrl { get; set; }
    public string? AddressV2 { get; set; }
    public string? WardCodeV2 { get; set; }
    public bool IsNewAddress { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public UserEntity User { get; set; } = null!;
}

public sealed class BrandProfileStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid BrandProfileId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class KocProfileEntity
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public string? Gender { get; set; }
    public string? Region { get; set; }
    public string? StreetAddress { get; set; }
    public Guid? WardId { get; set; }
    public Guid? ProvinceId { get; set; }
    public string? AddressV2 { get; set; }
    public string? WardCodeV2 { get; set; }
    public bool IsNewAddress { get; set; }
    public int Followers { get; set; }
    public decimal Score { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }

    public UserEntity User { get; set; } = null!;
}

public sealed class KocProfileStatusHistoryEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string? ReviewNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class KocBodyProfileEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public decimal? HeightCm { get; set; }
    public decimal? WeightKg { get; set; }
    public decimal? BustCm { get; set; }
    public decimal? WaistCm { get; set; }
    public decimal? HipCm { get; set; }
    public string? MaritalStatus { get; set; }
    public bool HasChildren { get; set; }
    public int ChildrenCount { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocChildEntity
{
    public Guid Id { get; set; }
    public Guid KocBodyProfileId { get; set; }
    public int? AgeYears { get; set; }
    public string? Gender { get; set; }
    public int? BirthYear { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocSocialAccountEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public string Platform { get; set; } = string.Empty;
    public string ChannelUrl { get; set; } = string.Empty;
    public string? Handle { get; set; }
    public int Followers { get; set; }
    public int? AvgViews { get; set; }
    public decimal? AvgEngagementRate { get; set; }
    public bool IsPrimary { get; set; }
    public bool IsVerified { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocCategoryEntity
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocCategoryHistoryEntity
{
    public long Id { get; set; }
    public Guid? KocCategoryId { get; set; }
    public string ActionName { get; set; } = string.Empty;
    public string PreviousJson { get; set; } = string.Empty;
    public string CurrentJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class KocProfileCategoryEntity
{
    public Guid KocProfileId { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocSocialAccountCategoryEntity
{
    public Guid SocialAccountId { get; set; }
    public Guid CategoryId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocExperienceEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public decimal? YearsOfExperience { get; set; }
    public string? WorkedBrands { get; set; }
    public string? CaseStudyUrl { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocProductionCapabilityEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public bool CanScript { get; set; }
    public bool CanShoot { get; set; }
    public bool CanEdit { get; set; }
    public bool CanLivestream { get; set; }
    public string? Devices { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocJobPreferenceEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public decimal? MinBudgetAmount { get; set; }
    public string? PreferredRegions { get; set; }
    public string? AvailabilityNote { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocPortfolioEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? BrandName { get; set; }
    public string? CampaignName { get; set; }
    public string? PostUrl { get; set; }
    public string? Platform { get; set; }
    public long Views { get; set; }
    public long Likes { get; set; }
    public long Comments { get; set; }
    public long Shares { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class KocMediaAssetEntity
{
    public Guid Id { get; set; }
    public Guid KocProfileId { get; set; }
    public string MediaType { get; set; } = "GalleryImage";
    public string FileUrl { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsVisible { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

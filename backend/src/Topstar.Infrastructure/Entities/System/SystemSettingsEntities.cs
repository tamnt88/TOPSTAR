namespace Topstar.Infrastructure.Entities;

public sealed class SocialLoginConfigurationEntity
{
    public Guid Id { get; set; }
    public string Provider { get; set; } = string.Empty;
    public string? RedirectUri { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public bool LoginEnabled { get; set; }
    public bool RegistrationEnabled { get; set; }
    public bool BrandEnabled { get; set; }
    public bool KocEnabled { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SystemEmailAccountEntity
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EmailAddress { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? SmtpHost { get; set; }
    public int? SmtpPort { get; set; }
    public string? SmtpUsername { get; set; }
    public string? SmtpPassword { get; set; }
    public bool SmtpEnableSsl { get; set; }
    public string? ImapHost { get; set; }
    public int? ImapPort { get; set; }
    public string? ImapUsername { get; set; }
    public string? ImapPassword { get; set; }
    public bool ImapEnableSsl { get; set; }
    public bool IsDefaultSender { get; set; }
    public bool IsDefaultReceiver { get; set; }
    public bool IsActive { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SystemShippingConfigEntity
{
    public Guid Id { get; set; }
    public string ProviderCode { get; set; } = string.Empty;
    public string ProviderName { get; set; } = string.Empty;
    public string ApiBaseUrl { get; set; } = string.Empty;
    public string? ApiToken { get; set; }
    public string? ShopId { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? WebhookUrl { get; set; }
    public string? WebhookSecret { get; set; }
    public bool IsSandbox { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class SystemWebsiteConfigEntity
{
    public Guid Id { get; set; }
    public string WebsiteName { get; set; } = string.Empty;
    public string? ShortName { get; set; }
    public string? Domain { get; set; }
    public string? Address { get; set; }
    public string? ContactEmail { get; set; }
    public string? Hotline { get; set; }
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string? DefaultShareImageUrl { get; set; }
    public string? CopyrightText { get; set; }
    public string? HomepageContentJson { get; set; }
    public string? AcademyContentJson { get; set; }
    public bool IsActive { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PublicPageSeoConfigEntity
{
    [System.ComponentModel.DataAnnotations.MaxLength(200)] public string? PageTitle { get; set; }
    [System.ComponentModel.DataAnnotations.MaxLength(1000)] public string? ShortDescription { get; set; }
    public string? PageContent { get; set; }
    public Guid Id { get; set; }
    public string PageKey { get; set; } = string.Empty;
    public string PagePath { get; set; } = string.Empty;
    public string? SeoTitle { get; set; }
    public string? SeoDescription { get; set; }
    public string? SeoKeywords { get; set; }
    public string? CanonicalUrl { get; set; }
    public string? OgTitle { get; set; }
    public string? OgDescription { get; set; }
    public string? OgImageUrl { get; set; }
    public string? TwitterTitle { get; set; }
    public string? TwitterDescription { get; set; }
    public string? TwitterImageUrl { get; set; }
    public string? JsonLd { get; set; }
    public bool IsActive { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class CampaignPackagePolicyEntity
{
    public decimal? ReferenceBudget {get;set;}
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public int? ValidityDays { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public decimal MinBudgetAmount { get; set; }
    public decimal? MaxBudgetAmount { get; set; }
    public bool BrandSelectionEnabled { get; set; }
    public int? SelectionQuota { get; set; }
    public bool IsActive { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class HomepageContentHistoryEntity
{
    public Guid Id { get; set; }
    public string ContentJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AcademyContentHistoryEntity
{
    public Guid Id { get; set; }
    public string ContentJson { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class CampaignPackagePolicyHistoryEntity
{
    public decimal? ReferenceBudget {get;set;}
    public Guid Id { get; set; }
    public Guid PolicyId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal SalePrice { get; set; }
    public string? ImageUrl { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Benefits { get; set; } = string.Empty;
    public int? ValidityDays { get; set; }
    public bool IsFeatured { get; set; }
    public int DisplayOrder { get; set; }
    public decimal MinBudgetAmount { get; set; }
    public decimal? MaxBudgetAmount { get; set; }
    public bool BrandSelectionEnabled { get; set; }
    public int? SelectionQuota { get; set; }
    public bool IsActive { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class BrandPackagePurchaseEntity
{ public Guid? VatPolicyIdSnapshot{get;set;}public decimal VatRateSnapshot{get;set;}public decimal VatAmountSnapshot{get;set;}public decimal VatAddedSnapshot{get;set;}public string VatModeSnapshot{get;set;}="None";

    public Guid Id { get; set; }
    public Guid BrandProfileId { get; set; }
    public Guid PackagePolicyId { get; set; }
    public Guid? CampaignId { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public string PackageCodeSnapshot { get; set; } = string.Empty;
    public string PackageNameSnapshot { get; set; } = string.Empty;
    public decimal AmountSnapshot { get; set; }
    public int? ValidityDaysSnapshot { get; set; }
    public decimal MinBudgetAmountSnapshot { get; set; }
    public decimal? MaxBudgetAmountSnapshot { get; set; }
    public bool BrandSelectionEnabledSnapshot { get; set; }
    public int? SelectionQuotaSnapshot { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? PaymentReference { get; set; }
    public string PaymentMethod { get; set; } = "BankTransfer";
    public string? PaymentProofUrl { get; set; }
    public string? PaymentProofNote { get; set; }
    public DateTime? PaymentProofSubmittedAtUtc { get; set; }
    public DateTime PurchasedAtUtc { get; set; }
    public DateTime? PaidAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public DateTime? ConsumedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class BrandPackagePurchaseHistoryEntity
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public Guid? ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class PaymentMethodConfigurationEntity
{
    public bool VnPayEnabled { get; set; }
    public string VnPayEnvironment { get; set; } = "Test";
    public string? VnPayTmnCode { get; set; }
    public string? VnPayHashSecretCiphertext { get; set; }
    public string VnPayBaseUrl { get; set; } = "https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
    public string VnPayReturnUrl { get; set; } = "https://topstar.vn/Brand/VnPayReturn";
    public string VnPayIpnUrl { get; set; } = "https://topstar.vn/api/payments/vnpay/ipn";
    public Guid Id { get; set; }
    public bool BankTransferEnabled { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
    public string? BankAccountHolder { get; set; }
    public bool OnePayEnabled { get; set; }
    public bool OnePayDomesticEnabled { get; set; }
    public bool OnePayInternationalEnabled { get; set; }
    public bool OnePayEWalletEnabled { get; set; }
    public string OnePayEnvironment { get; set; } = "Test";
    public string? OnePayMerchantId { get; set; }
    public string? OnePayAccessCode { get; set; }
    public string? OnePaySecureHashCiphertext { get; set; }
    public string? OnePayBaseUrl { get; set; }
    public string OnePayReturnUrl { get; set; } = string.Empty;
    public string OnePayCallbackUrl { get; set; } = string.Empty;
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

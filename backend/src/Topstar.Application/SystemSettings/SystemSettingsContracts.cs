namespace Topstar.Application.SystemSettings;

public interface ISystemSettingsAdminService
{
    Task<IReadOnlyList<EmailAccountItem>> GetEmailAccountsAsync(CancellationToken cancellationToken = default);
    Task<EmailAccountItem?> GetEmailAccountByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateEmailAccountAsync(EmailAccountUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateEmailAccountAsync(EmailAccountUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteEmailAccountAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ShippingConfigItem>> GetShippingConfigsAsync(CancellationToken cancellationToken = default);
    Task<ShippingConfigItem?> GetShippingConfigByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateShippingConfigAsync(ShippingConfigUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateShippingConfigAsync(ShippingConfigUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteShippingConfigAsync(Guid id, CancellationToken cancellationToken = default);

    Task<WebsiteConfigItem?> GetWebsiteConfigAsync(CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveWebsiteConfigAsync(WebsiteConfigUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> SaveBrandingAsync(BrandingUpsertRequest request, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<SeoConfigItem>> GetSeoConfigsAsync(CancellationToken cancellationToken = default);
    Task<SeoConfigItem?> GetSeoConfigByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<ServiceResult> CreateSeoConfigAsync(SeoConfigUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> UpdateSeoConfigAsync(SeoConfigUpsertRequest request, CancellationToken cancellationToken = default);
    Task<ServiceResult> DeleteSeoConfigAsync(Guid id, CancellationToken cancellationToken = default);
}

public sealed record EmailAccountItem(
    Guid Id,
    string Name,
    string EmailAddress,
    string DisplayName,
    string? SmtpHost,
    int? SmtpPort,
    bool SmtpEnableSsl,
    bool IsDefaultSender,
    bool IsDefaultReceiver,
    bool IsActive);

public sealed record EmailAccountUpsertRequest(
    Guid? Id,
    string Name,
    string EmailAddress,
    string DisplayName,
    string? SmtpHost,
    int? SmtpPort,
    string? SmtpUsername,
    string? SmtpPassword,
    bool SmtpEnableSsl,
    string? ImapHost,
    int? ImapPort,
    string? ImapUsername,
    string? ImapPassword,
    bool ImapEnableSsl,
    bool IsDefaultSender,
    bool IsDefaultReceiver,
    bool IsActive,
    bool IsVisible);

public sealed record ShippingConfigItem(
    Guid Id,
    string ProviderCode,
    string ProviderName,
    string ApiBaseUrl,
    bool IsDefault,
    bool IsSandbox,
    bool IsActive);

public sealed record ShippingConfigUpsertRequest(
    Guid? Id,
    string ProviderCode,
    string ProviderName,
    string ApiBaseUrl,
    string? ApiToken,
    string? ShopId,
    string? ClientId,
    string? ClientSecret,
    string? WebhookUrl,
    string? WebhookSecret,
    bool IsSandbox,
    bool IsDefault,
    bool IsActive,
    bool IsVisible);

public sealed record WebsiteConfigItem(
    Guid Id,
    string WebsiteName,
    string? ShortName,
    string? Domain,
    string? Address,
    string? ContactEmail,
    string? Hotline,
    string? LogoUrl,
    string? FaviconUrl,
    string? DefaultShareImageUrl,
    string? CopyrightText,
    bool IsActive,
    bool IsVisible);

public sealed record WebsiteConfigUpsertRequest(
    string WebsiteName,
    string? ShortName,
    string? Domain,
    string? Address,
    string? ContactEmail,
    string? Hotline,
    string? CopyrightText,
    bool IsActive,
    bool IsVisible);

public sealed record BrandingUpsertRequest(
    string? LogoUrl,
    string? FaviconUrl,
    string? DefaultShareImageUrl);

public sealed record SeoConfigItem(
    Guid Id,
    string PageKey,
    string PagePath,
    string? SeoTitle,
    string? SeoDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? TwitterTitle,
    string? TwitterDescription,
    string? TwitterImageUrl,
    string? JsonLd,
    bool IsActive,
    bool IsVisible);

public sealed record SeoConfigUpsertRequest(
    Guid? Id,
    string PageKey,
    string PagePath,
    string? SeoTitle,
    string? SeoDescription,
    string? SeoKeywords,
    string? CanonicalUrl,
    string? OgTitle,
    string? OgDescription,
    string? OgImageUrl,
    string? TwitterTitle,
    string? TwitterDescription,
    string? TwitterImageUrl,
    string? JsonLd,
    bool IsActive,
    bool IsVisible);

public sealed record ServiceResult(bool Success, string Message);

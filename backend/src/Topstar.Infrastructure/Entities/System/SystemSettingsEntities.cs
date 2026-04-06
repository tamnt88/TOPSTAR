namespace Topstar.Infrastructure.Entities;

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
    public bool IsActive { get; set; }
    public long ViewCount { get; set; }
    public bool IsVisible { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class PublicPageSeoConfigEntity
{
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

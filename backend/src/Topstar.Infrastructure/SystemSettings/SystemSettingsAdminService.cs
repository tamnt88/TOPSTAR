using Microsoft.EntityFrameworkCore;
using Topstar.Application.SystemSettings;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.SystemSettings;

public sealed class SystemSettingsAdminService(TopstarDbContext db) : ISystemSettingsAdminService
{
    public async Task<IReadOnlyList<EmailAccountItem>> GetEmailAccountsAsync(CancellationToken cancellationToken = default)
        => await db.SystemEmailAccounts.AsNoTracking()
            .OrderByDescending(x => x.IsDefaultSender)
            .ThenBy(x => x.Name)
            .Select(x => new EmailAccountItem(x.Id, x.Name, x.EmailAddress, x.DisplayName, x.SmtpHost, x.SmtpPort, x.SmtpEnableSsl, x.IsDefaultSender, x.IsDefaultReceiver, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<EmailAccountItem?> GetEmailAccountByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.SystemEmailAccounts.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new EmailAccountItem(x.Id, x.Name, x.EmailAddress, x.DisplayName, x.SmtpHost, x.SmtpPort, x.SmtpEnableSsl, x.IsDefaultSender, x.IsDefaultReceiver, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateEmailAccountAsync(EmailAccountUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.EmailAddress))
            return new ServiceResult(false, "Vui lòng nhập Tên và Email.");

        if (await db.SystemEmailAccounts.AnyAsync(x => x.EmailAddress == request.EmailAddress.Trim(), cancellationToken))
            return new ServiceResult(false, "Email đã tồn tại.");

        var now = DateTime.UtcNow;
        var entity = new SystemEmailAccountEntity
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            EmailAddress = request.EmailAddress.Trim(),
            DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name.Trim() : request.DisplayName.Trim(),
            SmtpHost = Normalize(request.SmtpHost),
            SmtpPort = request.SmtpPort,
            SmtpUsername = Normalize(request.SmtpUsername),
            SmtpPassword = Normalize(request.SmtpPassword),
            SmtpEnableSsl = request.SmtpEnableSsl,
            ImapHost = Normalize(request.ImapHost),
            ImapPort = request.ImapPort,
            ImapUsername = Normalize(request.ImapUsername),
            ImapPassword = Normalize(request.ImapPassword),
            ImapEnableSsl = request.ImapEnableSsl,
            IsDefaultSender = request.IsDefaultSender,
            IsDefaultReceiver = request.IsDefaultReceiver,
            IsActive = request.IsActive,
            IsVisible = request.IsVisible,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        db.SystemEmailAccounts.Add(entity);
        await HandleDefaultFlagsForEmailAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã tạo tài khoản email.");
    }

    public async Task<ServiceResult> UpdateEmailAccountAsync(EmailAccountUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Id.HasValue || request.Id.Value == Guid.Empty)
            return new ServiceResult(false, "Không tìm thấy bản ghi.");

        var entity = await db.SystemEmailAccounts.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Không tìm thấy bản ghi.");

        if (await db.SystemEmailAccounts.AnyAsync(x => x.EmailAddress == request.EmailAddress.Trim() && x.Id != request.Id.Value, cancellationToken))
            return new ServiceResult(false, "Email đã tồn tại.");

        entity.Name = request.Name.Trim();
        entity.EmailAddress = request.EmailAddress.Trim();
        entity.DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? request.Name.Trim() : request.DisplayName.Trim();
        entity.SmtpHost = Normalize(request.SmtpHost);
        entity.SmtpPort = request.SmtpPort;
        entity.SmtpUsername = Normalize(request.SmtpUsername);
        entity.SmtpPassword = Normalize(request.SmtpPassword) ?? entity.SmtpPassword;
        entity.SmtpEnableSsl = request.SmtpEnableSsl;
        entity.ImapHost = Normalize(request.ImapHost);
        entity.ImapPort = request.ImapPort;
        entity.ImapUsername = Normalize(request.ImapUsername);
        entity.ImapPassword = Normalize(request.ImapPassword) ?? entity.ImapPassword;
        entity.ImapEnableSsl = request.ImapEnableSsl;
        entity.IsDefaultSender = request.IsDefaultSender;
        entity.IsDefaultReceiver = request.IsDefaultReceiver;
        entity.IsActive = request.IsActive;
        entity.IsVisible = request.IsVisible;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await HandleDefaultFlagsForEmailAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã cập nhật tài khoản email.");
    }

    public async Task<ServiceResult> DeleteEmailAccountAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemEmailAccounts.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Không tìm thấy bản ghi.");
        db.SystemEmailAccounts.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã xóa tài khoản email.");
    }

    public async Task<IReadOnlyList<ShippingConfigItem>> GetShippingConfigsAsync(CancellationToken cancellationToken = default)
        => await db.SystemShippingConfigs.AsNoTracking()
            .OrderByDescending(x => x.IsDefault)
            .ThenBy(x => x.ProviderName)
            .Select(x => new ShippingConfigItem(x.Id, x.ProviderCode, x.ProviderName, x.ApiBaseUrl, x.IsDefault, x.IsSandbox, x.IsActive))
            .ToListAsync(cancellationToken);

    public async Task<ShippingConfigItem?> GetShippingConfigByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.SystemShippingConfigs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new ShippingConfigItem(x.Id, x.ProviderCode, x.ProviderName, x.ApiBaseUrl, x.IsDefault, x.IsSandbox, x.IsActive))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateShippingConfigAsync(ShippingConfigUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.ProviderCode) || string.IsNullOrWhiteSpace(request.ProviderName))
            return new ServiceResult(false, "Vui lòng nhập mã và tên nhà vận chuyển.");

        var code = request.ProviderCode.Trim().ToUpperInvariant();
        if (await db.SystemShippingConfigs.AnyAsync(x => x.ProviderCode == code, cancellationToken))
            return new ServiceResult(false, "Mã nhà vận chuyển đã tồn tại.");

        var entity = new SystemShippingConfigEntity
        {
            Id = Guid.NewGuid(),
            ProviderCode = code,
            ProviderName = request.ProviderName.Trim(),
            ApiBaseUrl = request.ApiBaseUrl.Trim(),
            ApiToken = Normalize(request.ApiToken),
            ShopId = Normalize(request.ShopId),
            ClientId = Normalize(request.ClientId),
            ClientSecret = Normalize(request.ClientSecret),
            WebhookUrl = Normalize(request.WebhookUrl),
            WebhookSecret = Normalize(request.WebhookSecret),
            IsSandbox = request.IsSandbox,
            IsDefault = request.IsDefault,
            IsActive = request.IsActive,
            IsVisible = request.IsVisible,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        db.SystemShippingConfigs.Add(entity);
        await HandleDefaultFlagsForShippingAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã tạo cấu hình giao nhận.");
    }

    public async Task<ServiceResult> UpdateShippingConfigAsync(ShippingConfigUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Id.HasValue || request.Id.Value == Guid.Empty)
            return new ServiceResult(false, "Không tìm thấy bản ghi.");

        var entity = await db.SystemShippingConfigs.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Không tìm thấy bản ghi.");

        var code = request.ProviderCode.Trim().ToUpperInvariant();
        if (await db.SystemShippingConfigs.AnyAsync(x => x.ProviderCode == code && x.Id != request.Id.Value, cancellationToken))
            return new ServiceResult(false, "Mã nhà vận chuyển đã tồn tại.");

        entity.ProviderCode = code;
        entity.ProviderName = request.ProviderName.Trim();
        entity.ApiBaseUrl = request.ApiBaseUrl.Trim();
        entity.ApiToken = Normalize(request.ApiToken);
        entity.ShopId = Normalize(request.ShopId);
        entity.ClientId = Normalize(request.ClientId);
        entity.ClientSecret = Normalize(request.ClientSecret);
        entity.WebhookUrl = Normalize(request.WebhookUrl);
        entity.WebhookSecret = Normalize(request.WebhookSecret);
        entity.IsSandbox = request.IsSandbox;
        entity.IsDefault = request.IsDefault;
        entity.IsActive = request.IsActive;
        entity.IsVisible = request.IsVisible;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await HandleDefaultFlagsForShippingAsync(entity, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã cập nhật cấu hình giao nhận.");
    }

    public async Task<ServiceResult> DeleteShippingConfigAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemShippingConfigs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Không tìm thấy bản ghi.");
        db.SystemShippingConfigs.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã xóa cấu hình giao nhận.");
    }

    public async Task<WebsiteConfigItem?> GetWebsiteConfigAsync(CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemWebsiteConfigs.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAtUtc ?? x.CreatedAtUtc)
            .FirstOrDefaultAsync(cancellationToken);
        if (entity is null) return null;
        return new WebsiteConfigItem(entity.Id, entity.WebsiteName, entity.ShortName, entity.Domain, entity.Address, entity.ContactEmail, entity.Hotline, entity.LogoUrl, entity.FaviconUrl, entity.DefaultShareImageUrl, entity.CopyrightText, entity.IsActive, entity.IsVisible);
    }

    public async Task<ServiceResult> SaveWebsiteConfigAsync(WebsiteConfigUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemWebsiteConfigs.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new SystemWebsiteConfigEntity { Id = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
            db.SystemWebsiteConfigs.Add(entity);
        }

        entity.WebsiteName = request.WebsiteName.Trim();
        entity.ShortName = Normalize(request.ShortName);
        entity.Domain = Normalize(request.Domain);
        entity.Address = Normalize(request.Address);
        entity.ContactEmail = Normalize(request.ContactEmail);
        entity.Hotline = Normalize(request.Hotline);
        entity.CopyrightText = Normalize(request.CopyrightText);
        entity.IsActive = request.IsActive;
        entity.IsVisible = request.IsVisible;
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã lưu cấu hình website.");
    }

    public async Task<ServiceResult> SaveBrandingAsync(BrandingUpsertRequest request, CancellationToken cancellationToken = default)
    {
        var entity = await db.SystemWebsiteConfigs.FirstOrDefaultAsync(cancellationToken);
        if (entity is null)
        {
            entity = new SystemWebsiteConfigEntity { Id = Guid.NewGuid(), WebsiteName = "TopStar", CreatedAtUtc = DateTime.UtcNow, IsActive = true, IsVisible = true };
            db.SystemWebsiteConfigs.Add(entity);
        }

        entity.LogoUrl = Normalize(request.LogoUrl);
        entity.FaviconUrl = Normalize(request.FaviconUrl);
        entity.DefaultShareImageUrl = Normalize(request.DefaultShareImageUrl);
        entity.UpdatedAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã lưu cấu hình branding.");
    }

    public async Task<IReadOnlyList<SeoConfigItem>> GetSeoConfigsAsync(CancellationToken cancellationToken = default)
        => await db.PublicPageSeoConfigs.AsNoTracking()
            .OrderBy(x => x.PageKey)
            .Select(x => new SeoConfigItem(
                x.Id,
                x.PageKey,
                x.PagePath,
                x.SeoTitle,
                x.SeoDescription,
                x.SeoKeywords,
                x.CanonicalUrl,
                x.OgTitle,
                x.OgDescription,
                x.OgImageUrl,
                x.TwitterTitle,
                x.TwitterDescription,
                x.TwitterImageUrl,
                x.JsonLd,
                x.IsActive,
                x.IsVisible))
            .ToListAsync(cancellationToken);

    public async Task<SeoConfigItem?> GetSeoConfigByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => await db.PublicPageSeoConfigs.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new SeoConfigItem(
                x.Id,
                x.PageKey,
                x.PagePath,
                x.SeoTitle,
                x.SeoDescription,
                x.SeoKeywords,
                x.CanonicalUrl,
                x.OgTitle,
                x.OgDescription,
                x.OgImageUrl,
                x.TwitterTitle,
                x.TwitterDescription,
                x.TwitterImageUrl,
                x.JsonLd,
                x.IsActive,
                x.IsVisible))
            .FirstOrDefaultAsync(cancellationToken);

    public async Task<ServiceResult> CreateSeoConfigAsync(SeoConfigUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.PageKey) || string.IsNullOrWhiteSpace(request.PagePath))
            return new ServiceResult(false, "Vui lòng nhập PageKey và PagePath.");

        if (await db.PublicPageSeoConfigs.AnyAsync(x => x.PageKey == request.PageKey.Trim(), cancellationToken))
            return new ServiceResult(false, "PageKey đã tồn tại.");
        if (await db.PublicPageSeoConfigs.AnyAsync(x => x.PagePath == request.PagePath.Trim(), cancellationToken))
            return new ServiceResult(false, "PagePath đã tồn tại.");

        db.PublicPageSeoConfigs.Add(new PublicPageSeoConfigEntity
        {
            Id = Guid.NewGuid(),
            PageKey = request.PageKey.Trim(),
            PagePath = request.PagePath.Trim(),
            SeoTitle = Normalize(request.SeoTitle),
            SeoDescription = Normalize(request.SeoDescription),
            SeoKeywords = Normalize(request.SeoKeywords),
            CanonicalUrl = Normalize(request.CanonicalUrl),
            OgTitle = Normalize(request.OgTitle),
            OgDescription = Normalize(request.OgDescription),
            OgImageUrl = Normalize(request.OgImageUrl),
            TwitterTitle = Normalize(request.TwitterTitle),
            TwitterDescription = Normalize(request.TwitterDescription),
            TwitterImageUrl = Normalize(request.TwitterImageUrl),
            JsonLd = Normalize(request.JsonLd),
            IsActive = request.IsActive,
            IsVisible = request.IsVisible,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã tạo cấu hình SEO.");
    }

    public async Task<ServiceResult> UpdateSeoConfigAsync(SeoConfigUpsertRequest request, CancellationToken cancellationToken = default)
    {
        if (!request.Id.HasValue || request.Id.Value == Guid.Empty)
            return new ServiceResult(false, "Không tìm thấy bản ghi.");

        var entity = await db.PublicPageSeoConfigs.FirstOrDefaultAsync(x => x.Id == request.Id.Value, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Không tìm thấy bản ghi.");

        if (await db.PublicPageSeoConfigs.AnyAsync(x => x.PageKey == request.PageKey.Trim() && x.Id != request.Id.Value, cancellationToken))
            return new ServiceResult(false, "PageKey đã tồn tại.");
        if (await db.PublicPageSeoConfigs.AnyAsync(x => x.PagePath == request.PagePath.Trim() && x.Id != request.Id.Value, cancellationToken))
            return new ServiceResult(false, "PagePath đã tồn tại.");

        entity.PageKey = request.PageKey.Trim();
        entity.PagePath = request.PagePath.Trim();
        entity.SeoTitle = Normalize(request.SeoTitle);
        entity.SeoDescription = Normalize(request.SeoDescription);
        entity.SeoKeywords = Normalize(request.SeoKeywords);
        entity.CanonicalUrl = Normalize(request.CanonicalUrl);
        entity.OgTitle = Normalize(request.OgTitle);
        entity.OgDescription = Normalize(request.OgDescription);
        entity.OgImageUrl = Normalize(request.OgImageUrl);
        entity.TwitterTitle = Normalize(request.TwitterTitle);
        entity.TwitterDescription = Normalize(request.TwitterDescription);
        entity.TwitterImageUrl = Normalize(request.TwitterImageUrl);
        entity.JsonLd = Normalize(request.JsonLd);
        entity.IsActive = request.IsActive;
        entity.IsVisible = request.IsVisible;
        entity.UpdatedAtUtc = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã cập nhật cấu hình SEO.");
    }

    public async Task<ServiceResult> DeleteSeoConfigAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var entity = await db.PublicPageSeoConfigs.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (entity is null) return new ServiceResult(false, "Không tìm thấy bản ghi.");
        db.PublicPageSeoConfigs.Remove(entity);
        await db.SaveChangesAsync(cancellationToken);
        return new ServiceResult(true, "Đã xóa cấu hình SEO.");
    }

    private async Task HandleDefaultFlagsForEmailAsync(SystemEmailAccountEntity current, CancellationToken cancellationToken)
    {
        if (current.IsDefaultSender)
        {
            var others = await db.SystemEmailAccounts.Where(x => x.Id != current.Id && x.IsDefaultSender).ToListAsync(cancellationToken);
            foreach (var item in others) item.IsDefaultSender = false;
        }

        if (current.IsDefaultReceiver)
        {
            var others = await db.SystemEmailAccounts.Where(x => x.Id != current.Id && x.IsDefaultReceiver).ToListAsync(cancellationToken);
            foreach (var item in others) item.IsDefaultReceiver = false;
        }
    }

    private async Task HandleDefaultFlagsForShippingAsync(SystemShippingConfigEntity current, CancellationToken cancellationToken)
    {
        if (!current.IsDefault) return;
        var others = await db.SystemShippingConfigs.Where(x => x.Id != current.Id && x.IsDefault).ToListAsync(cancellationToken);
        foreach (var item in others) item.IsDefault = false;
    }

    private static string? Normalize(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}

using System.Text.Json;
using Ganss.Xss;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Matching;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Matching;

public sealed class PackagePolicyAdminService(TopstarDbContext db) : IPackagePolicyAdminService
{
    public async Task<IReadOnlyList<PackagePolicyItem>> GetAllAsync(CancellationToken ct = default)
    {
        var items = await db.CampaignPackagePolicies.AsNoTracking().Where(x => !x.IsDeleted)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.SalePrice).Select(ToItem()).ToListAsync(ct);
        var ids = items.Select(x => x.Id).ToArray();
        var history = await db.CampaignPackagePolicyHistories.AsNoTracking()
            .Where(x => ids.Contains(x.PolicyId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        return items.Select(item => item with
        {
            History = history.Where(h => h.PolicyId == item.Id).Select(MapHistory).ToList()
        }).ToList();
    }

    public async Task<PackagePolicyItem?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await db.CampaignPackagePolicies.AsNoTracking().Where(x => !x.IsDeleted && x.Id == id)
            .Select(ToItem()).FirstOrDefaultAsync(ct);
        if (item is null) return null;
        var history = await db.CampaignPackagePolicyHistories.AsNoTracking()
            .Where(x => x.PolicyId == id).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
        return item with { History = history.Select(MapHistory).ToList() };
    }

    public async Task<IReadOnlyList<PackagePolicyItem>> GetPublicAsync(CancellationToken ct = default)
    {
        var items = await db.CampaignPackagePolicies.AsNoTracking().Where(x => !x.IsDeleted && x.IsActive)
            .OrderBy(x => x.DisplayOrder).ThenBy(x => x.SalePrice).Select(ToItem()).ToListAsync(ct);
        var creationRules=await Topstar.Infrastructure.Finance.CampaignCreationRules.ReadAsync(db,ct);
        if(creationRules is null)return [];
        return items.Select(x=>Topstar.Infrastructure.Finance.CampaignCreationRules.ReferenceTemplate(creationRules,x))
            .Where(x=>x is not null).Select(x=>x! with{Benefits=SanitizeBenefits(x!.Benefits)})
            .OrderBy(x=>x.DisplayOrder).ThenBy(x=>x.ReferenceBudget).ToList();
    }

    public async Task<MatchingResult> CreateAsync(Guid admin, PackagePolicyCreateRequest request, CancellationToken ct = default)
    {
        var code = (request.Code ?? "").Trim().ToUpperInvariant();
        if(request.ReferenceBudget.HasValue){
            var rules=await Topstar.Infrastructure.Finance.CampaignCreationRules.ReadAsync(db,ct);
            if(rules is not null){var error=Topstar.Infrastructure.Finance.CampaignCreationRules.Validate(rules,request.ReferenceBudget.Value,request.SelectionQuota??1,request.BrandSelectionEnabled);if(error is not null)return Fail(error);}
        }
        var reason = request.Reason?.Trim() ?? "";
        var validation = Validate(request.Name, request.SalePrice, request.Description, request.Benefits, request.ValidityDays,
            request.MinBudgetAmount, request.MaxBudgetAmount, request.BrandSelectionEnabled, request.SelectionQuota, reason);
        if (validation is not null) return Fail(validation);
        if (code.Length is < 2 or > 80 || code.Any(ch => !(char.IsLetterOrDigit(ch) || ch == '_')))
            return Fail("Mã gói chỉ gồm chữ in hoa, số và dấu gạch dưới, dài 2–80 ký tự.");
        if (await db.CampaignPackagePolicies.AnyAsync(x => x.Code == code, ct)) return Fail("Mã gói đã tồn tại.");

        var now = DateTime.UtcNow;
        var entity = new CampaignPackagePolicyEntity
        {
            Id = Guid.NewGuid(), Code = code, CreatedAtUtc = now
        };
        Apply(entity, request.Name, request.SalePrice, request.ImageUrl, request.Description, request.Benefits, request.ValidityDays,
            request.IsFeatured, request.DisplayOrder, request.MinBudgetAmount, request.MaxBudgetAmount,
            request.BrandSelectionEnabled, request.SelectionQuota, request.IsActive, now);
        entity.ReferenceBudget=request.ReferenceBudget;
        db.CampaignPackagePolicies.Add(entity);
        db.CampaignPackagePolicyHistories.Add(CreateHistory(entity, admin, reason, now));
        db.AuditLogs.Add(new() { ActorUserId = admin, EntityName = "CampaignPackagePolicy", EntityId = entity.Id.ToString(), ActionName = "Create", PayloadJson = JsonSerializer.Serialize(request), CreatedAtUtc = now });
        await db.SaveChangesAsync(ct);
        return new(true, "Đã thêm gói KOC và lưu lịch sử.", entity.Id);
    }

    public async Task<MatchingResult> UpdateAsync(Guid admin, PackagePolicyUpdateRequest request, CancellationToken ct = default)
    {
        var entity = await db.CampaignPackagePolicies.FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, ct);
        if (entity is null) return Fail("Không tìm thấy gói KOC.");
        if(request.ReferenceBudget.HasValue){
            var rules=await Topstar.Infrastructure.Finance.CampaignCreationRules.ReadAsync(db,ct);
            if(rules is not null){var error=Topstar.Infrastructure.Finance.CampaignCreationRules.Validate(rules,request.ReferenceBudget.Value,request.SelectionQuota??1,request.BrandSelectionEnabled);if(error is not null)return Fail(error);}
        }
        var reason = request.Reason?.Trim() ?? "";
        var validation = Validate(request.Name, request.SalePrice, request.Description, request.Benefits, request.ValidityDays,
            request.MinBudgetAmount, request.MaxBudgetAmount, request.BrandSelectionEnabled, request.SelectionQuota, reason);
        if (validation is not null) return Fail(validation);

        var before = new { entity.Name, entity.SalePrice, entity.ImageUrl, entity.Description, entity.Benefits, entity.ValidityDays, entity.IsFeatured, entity.DisplayOrder, entity.MinBudgetAmount, entity.MaxBudgetAmount, entity.BrandSelectionEnabled, entity.SelectionQuota, entity.IsActive };
        var now = DateTime.UtcNow;
        Apply(entity, request.Name, request.SalePrice, request.ImageUrl, request.Description, request.Benefits, request.ValidityDays,
            request.IsFeatured, request.DisplayOrder, request.MinBudgetAmount, request.MaxBudgetAmount,
            request.BrandSelectionEnabled, request.SelectionQuota, request.IsActive, now);
        entity.ReferenceBudget=request.ReferenceBudget;
        db.CampaignPackagePolicyHistories.Add(CreateHistory(entity, admin, reason, now));
        db.AuditLogs.Add(new() { ActorUserId = admin, EntityName = "CampaignPackagePolicy", EntityId = entity.Id.ToString(), ActionName = "Update", PayloadJson = JsonSerializer.Serialize(new { Before = before, After = request }), CreatedAtUtc = now });
        await db.SaveChangesAsync(ct);
        return new(true, "Đã cập nhật gói KOC và lưu lịch sử.", entity.Id);
    }

    public async Task<MatchingResult> DeleteAsync(Guid admin, Guid id, string reason, CancellationToken ct = default)
    {
        reason = (reason ?? "").Trim();
        if (reason.Length is < 10 or > 1000) return Fail("Lý do xóa phải dài 10–1.000 ký tự.");
        var entity = await db.CampaignPackagePolicies.FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, ct);
        if (entity is null) return Fail("Không tìm thấy gói KOC.");
        var now = DateTime.UtcNow;
        entity.IsActive = false; entity.IsDeleted = true; entity.UpdatedAtUtc = now;
        db.CampaignPackagePolicyHistories.Add(CreateHistory(entity, admin, "Xóa mềm: " + reason, now));
        db.AuditLogs.Add(new() { ActorUserId = admin, EntityName = "CampaignPackagePolicy", EntityId = entity.Id.ToString(), ActionName = "SoftDelete", PayloadJson = JsonSerializer.Serialize(new { entity.Code, Reason = reason }), CreatedAtUtc = now });
        await db.SaveChangesAsync(ct);
        return new(true, "Đã xóa gói KOC. Dữ liệu lịch sử vẫn được bảo toàn.", id);
    }

    private static System.Linq.Expressions.Expression<Func<CampaignPackagePolicyEntity, PackagePolicyItem>> ToItem() =>
        x => new PackagePolicyItem(x.Id, x.Code, x.Name, x.MinBudgetAmount, x.MaxBudgetAmount, x.BrandSelectionEnabled, x.SelectionQuota, x.IsActive)
        {
            ReferenceBudget=x.ReferenceBudget,SalePrice = x.SalePrice, ImageUrl = x.ImageUrl, Description = x.Description, Benefits = x.Benefits,
            ValidityDays = x.ValidityDays, IsFeatured = x.IsFeatured, DisplayOrder = x.DisplayOrder
        };

    private static PackagePolicyHistoryItem MapHistory(CampaignPackagePolicyHistoryEntity h) => new(h.MinBudgetAmount, h.MaxBudgetAmount, h.BrandSelectionEnabled, h.SelectionQuota, h.IsActive, h.Reason, h.CreatedAtUtc)
    {
        ReferenceBudget=h.ReferenceBudget,SalePrice = h.SalePrice, ImageUrl = h.ImageUrl, Description = h.Description, Benefits = h.Benefits,
        ValidityDays = h.ValidityDays, IsFeatured = h.IsFeatured, DisplayOrder = h.DisplayOrder
    };

    private static CampaignPackagePolicyHistoryEntity CreateHistory(CampaignPackagePolicyEntity e, Guid actor, string reason, DateTime now) => new()
    {
        Id = Guid.NewGuid(), PolicyId = e.Id, Name = e.Name, ReferenceBudget=e.ReferenceBudget,SalePrice = e.SalePrice, ImageUrl = e.ImageUrl, Description = e.Description,
        Benefits = e.Benefits, ValidityDays = e.ValidityDays, IsFeatured = e.IsFeatured, DisplayOrder = e.DisplayOrder,
        MinBudgetAmount = e.MinBudgetAmount, MaxBudgetAmount = e.MaxBudgetAmount, BrandSelectionEnabled = e.BrandSelectionEnabled,
        SelectionQuota = e.SelectionQuota, IsActive = e.IsActive, Reason = reason, ActorUserId = actor, CreatedAtUtc = now
    };

    private static void Apply(CampaignPackagePolicyEntity e, string name, decimal price, string? imageUrl, string description, string benefits,
        int? validityDays, bool featured, int displayOrder, decimal min, decimal? max, bool brandSelection, int? quota, bool active, DateTime now)
    {
        e.Name = name.Trim(); e.SalePrice = price; e.ImageUrl = string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim();
        e.Description = description.Trim(); e.Benefits = SanitizeBenefits(benefits);
        e.ValidityDays = validityDays; e.IsFeatured = featured; e.DisplayOrder = displayOrder;
        e.MinBudgetAmount = min; e.MaxBudgetAmount = max; e.BrandSelectionEnabled = brandSelection;
        e.SelectionQuota = quota; e.IsActive = active; e.UpdatedAtUtc = now;
    }

    private static string? Validate(string name, decimal price, string description, string benefits, int? validityDays,
        decimal min, decimal? max, bool brandSelection, int? quota, string reason)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > 150) return "Tên gói là bắt buộc và không vượt quá 150 ký tự.";
        if (price <= 0) return "Giá bán phải lớn hơn 0.";
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length is < 10 or > 1000) return "Mô tả gói phải dài 10–1.000 ký tự.";
        if (string.IsNullOrWhiteSpace(benefits) || benefits.Trim().Length > 2000) return "Vui lòng nhập quyền lợi của gói.";
        if (validityDays is < 1 or > 3650) return "Thời hạn gói phải từ 1 đến 3.650 ngày.";
        if (reason.Length is < 5 or > 1000) return "Lý do thay đổi phải dài 5–1.000 ký tự.";
        if (min < 0 || max.HasValue && max < min) return "Khoảng ngân sách không hợp lệ.";
        if (!quota.HasValue || quota is < 1 or > 100) return "Mỗi gói phải có số KOC tối đa từ 1 đến 100.";
        return null;
    }

    private static string SanitizeBenefits(string benefits)
    {
        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedTags.Clear();
        foreach (var tag in new[] { "p", "br", "ul", "ol", "li", "strong", "b", "em", "i", "u", "s", "sub", "sup", "span", "h2", "h3", "h4", "h5", "blockquote", "pre", "code", "a", "figure", "figcaption", "img", "table", "thead", "tbody", "tr", "th", "td", "oembed" }) sanitizer.AllowedTags.Add(tag);
        sanitizer.AllowedAttributes.Clear();
        foreach (var attribute in new[] { "class", "style", "href", "target", "rel", "src", "alt", "width", "height", "url" }) sanitizer.AllowedAttributes.Add(attribute);
        return sanitizer.Sanitize(benefits ?? string.Empty).Trim();
    }

    private static MatchingResult Fail(string message) => new(false, message);
}

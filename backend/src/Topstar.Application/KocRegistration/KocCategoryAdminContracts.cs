namespace Topstar.Application.KocRegistration;

public sealed record KocCategoryAdminItem(
    Guid Id,
    string Code,
    string Name,
    int SortOrder,
    bool IsActive,
    int UsageCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record SaveKocCategoryRequest(Guid? Id, string Code, string Name, int SortOrder, string Reason);
public sealed record KocCategoryAdminResult(bool Success, string Message, Guid? Id = null, string? Field = null);
public sealed record KocCategoryHistoryItem(long Id, Guid? CategoryId, string CategoryName, string CategoryCode, string Action, string ActionLabel, string Reason, string Actor, DateTime CreatedAtUtc, string PreviousSummary, string CurrentSummary);

public interface IKocCategoryAdminService
{
    Task<IReadOnlyList<KocCategoryAdminItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<KocCategoryAdminItem?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KocCategoryHistoryItem>> GetHistoryAsync(CancellationToken cancellationToken = default);
    Task<KocCategoryAdminResult> SaveAsync(Guid actorUserId, SaveKocCategoryRequest request, CancellationToken cancellationToken = default);
    Task<KocCategoryAdminResult> SetActiveAsync(Guid actorUserId, Guid id, bool isActive, string reason, CancellationToken cancellationToken = default);
    Task<KocCategoryAdminResult> DeleteAsync(Guid actorUserId, Guid id, string reason, CancellationToken cancellationToken = default);
}

public static class KocCategoryAdminRules
{
    public static string NormalizeCode(string? value)
        => string.IsNullOrWhiteSpace(value)
            ? string.Empty
            : string.Join('-', value.Trim().ToLowerInvariant().Split([' ', '_', '-'], StringSplitOptions.RemoveEmptyEntries));

    public static string? Validate(string code, string name, int sortOrder, string reason)
    {
        if (code.Length is < 2 or > 50 || code.Any(x => !(char.IsAsciiLetterOrDigit(x) || x == '-')))
            return "Mã danh mục phải từ 2 đến 50 ký tự, chỉ gồm chữ không dấu, số và dấu gạch ngang.";
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length is < 2 or > 100)
            return "Tên lĩnh vực phải từ 2 đến 100 ký tự.";
        if (sortOrder is < 0 or > 9999)
            return "Thứ tự hiển thị phải từ 0 đến 9.999.";
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 10 or > 500)
            return "Vui lòng nhập lý do thay đổi từ 10 đến 500 ký tự.";
        return null;
    }

    public static string? ValidateDeleteReason(string? reason)
        => string.IsNullOrWhiteSpace(reason) || reason.Trim().Length is < 10 or > 500
            ? "Vui lòng nhập lý do xóa từ 10 đến 500 ký tự."
            : null;
}

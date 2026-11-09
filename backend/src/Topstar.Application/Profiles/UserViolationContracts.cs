namespace Topstar.Application.Profiles;
public static class UserViolationRules
{
    public static readonly string[] Severities = ["Low", "Medium", "High"];
    public static bool IsValidSeverity(string? value) => value is not null && Severities.Contains(value, StringComparer.Ordinal);
    public static bool IsValidPenalty(int value) => value is >= 0 and <= 100;
    public static decimal EffectiveScore(decimal baseScore, int activePenalty) => Math.Max(0m, baseScore - activePenalty);
}
public sealed record UserViolationEvent(string? FromStatus,string ToStatus,string Note,DateTime CreatedAtUtc);
public sealed record UserViolationItem(Guid Id,Guid UserId,string DisplayName,string Email,string Role,string Category,string Severity,int PenaltyPoints,string Reason,string Status,string? ResolutionNote,DateTime CreatedAtUtc){public IReadOnlyList<UserViolationEvent> History{get;init;}=[];}
public sealed record UserViolationResult(bool Success,string Message);
public interface IUserViolationService
{
    Task<IReadOnlyList<UserViolationItem>> GetAsync(string? status,Guid? userId=null,CancellationToken cancellationToken=default);
    Task<UserViolationResult> CreateAsync(Guid adminUserId,string email,string category,string severity,int penaltyPoints,string reason,CancellationToken cancellationToken=default);
    Task<UserViolationResult> RevokeAsync(Guid adminUserId,Guid violationId,string reason,CancellationToken cancellationToken=default);
}

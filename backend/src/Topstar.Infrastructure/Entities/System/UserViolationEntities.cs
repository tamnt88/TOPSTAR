namespace Topstar.Infrastructure.Entities;
public sealed class UserViolationEntity
{
    public Guid Id{get;set;}public Guid UserId{get;set;}public string Category{get;set;}=string.Empty;public string Severity{get;set;}=string.Empty;public int PenaltyPoints{get;set;}public string Reason{get;set;}=string.Empty;public string Status{get;set;}="Active";public string? ResolutionNote{get;set;}public Guid CreatedByUserId{get;set;}public Guid? RevokedByUserId{get;set;}public DateTime? RevokedAtUtc{get;set;}public DateTime CreatedAtUtc{get;set;}public DateTime? UpdatedAtUtc{get;set;}
}
public sealed class UserViolationHistoryEntity
{
    public Guid Id{get;set;}public Guid ViolationId{get;set;}public string? FromStatus{get;set;}public string ToStatus{get;set;}=string.Empty;public string Note{get;set;}=string.Empty;public Guid ActorUserId{get;set;}public DateTime CreatedAtUtc{get;set;}
}

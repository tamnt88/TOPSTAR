namespace Topstar.Infrastructure.Entities;
public sealed class DisputeCaseEntity
{
 public Guid Id{get;set;} public Guid ParticipationId{get;set;} public Guid CreatedByUserId{get;set;} public Guid? AssignedAdminUserId{get;set;}
 public string Category{get;set;}=string.Empty; public string Subject{get;set;}=string.Empty; public string Description{get;set;}=string.Empty; public string Status{get;set;}=string.Empty; public string Priority{get;set;}="Normal"; public bool IsActive{get;set;}=true; public string? ResolutionNote{get;set;} public DateTime CreatedAtUtc{get;set;} public DateTime? UpdatedAtUtc{get;set;} public DateTime? ResolvedAtUtc{get;set;}
}
public sealed class DisputeCaseStatusHistoryEntity
{
 public Guid Id{get;set;} public Guid DisputeCaseId{get;set;} public string? FromStatus{get;set;} public string ToStatus{get;set;}=string.Empty; public string? Note{get;set;} public Guid? ActorUserId{get;set;} public DateTime CreatedAtUtc{get;set;}
}
public sealed class DisputeMessageEntity
{
 public Guid Id{get;set;} public Guid DisputeCaseId{get;set;} public Guid AuthorUserId{get;set;} public string AuthorRole{get;set;}=string.Empty; public string Body{get;set;}=string.Empty; public string? EvidenceUrl{get;set;} public string Visibility{get;set;}="Parties"; public DateTime CreatedAtUtc{get;set;}
}

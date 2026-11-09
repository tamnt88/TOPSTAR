namespace Topstar.Application.Collaborations;

public interface ICollaborationHistoryService
{
    Task<IReadOnlyList<CollaborationItem>> GetForBrandAsync(Guid brandUserId,Guid? campaignId=null,CancellationToken cancellationToken=default, Topstar.Application.Common.ListWindow? window = null);
    Task<IReadOnlyList<CollaborationItem>> GetForKocAsync(Guid kocUserId,CancellationToken cancellationToken=default);
    Task<CollaborationPage> GetKocPageAsync(Guid kocUserId,string? search,string? status,int skip,int take,CancellationToken cancellationToken=default);
    Task<IReadOnlyList<CollaborationItem>> GetForAdminAsync(CancellationToken cancellationToken=default, Topstar.Application.Common.ListWindow? window = null);
}
public sealed record CollaborationPage(int TotalCount,int FilteredCount,IReadOnlyList<CollaborationItem> Items);
public sealed record CollaborationItem(Guid ParticipationId,string CampaignTitle,string BrandName,string KocName,string Status,DateTime ConfirmedAtUtc,IReadOnlyList<CollaborationEvent> Events)
{
    public string CurrentStep { get; init; } = "Được chọn";
    public string WaitingFor { get; init; } = "Brand/Admin";
    public string NextAction { get; init; } = "Chờ hệ thống cập nhật bước tiếp theo.";
}
public sealed record CollaborationEvent(string Domain,string Status,string? Note,string? ActorRole,DateTime OccurredAtUtc);

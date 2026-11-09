namespace Topstar.Application.Settings;

public sealed record SampleDataCleanupPreview(
    int MemberAccounts,
    int BrandAccounts,
    int KocProfiles,
    int BrandProfiles,
    int Campaigns,
    int RelatedRecords)
{
    public int TotalAccounts => MemberAccounts + BrandAccounts;
    public bool HasSampleData => TotalAccounts > 0;
}

public sealed record SampleDataCleanupResult(bool Success, string Message, SampleDataCleanupPreview Preview);

public interface ISampleDataCleanupService
{
    Task<SampleDataCleanupPreview> PreviewAsync(Guid actorUserId, CancellationToken ct = default);
    Task<SampleDataCleanupResult> DeleteAsync(Guid actorUserId, CancellationToken ct = default);
}

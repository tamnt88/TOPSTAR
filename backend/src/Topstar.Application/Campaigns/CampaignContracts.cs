namespace Topstar.Application.Campaigns;

public static class CampaignStatuses
{
    public const string Draft = "Draft";
    public const string Submitted = "Submitted";
    public const string NeedEdit = "NeedEdit";
    public const string Published = "Published";
    public const string Rejected = "Rejected";
    public const string InProgress = "InProgress";
    public const string Completed = "Completed";
    public const string Cancelled = "Cancelled";
    public const string Paused = "Paused";
}

public static class CampaignLifecycleRules
{
    public static bool CanPause(string status, bool obligationsStarted)
        => !obligationsStarted && status is CampaignStatuses.Published or CampaignStatuses.InProgress;
    public static bool CanCancel(string status, bool obligationsStarted, bool paymentUncertain)
        => !obligationsStarted && !paymentUncertain && status is CampaignStatuses.Draft or CampaignStatuses.Submitted or CampaignStatuses.NeedEdit or CampaignStatuses.Rejected or CampaignStatuses.Published or CampaignStatuses.InProgress or CampaignStatuses.Paused;
    public static bool CanResume(string status, string? previous)
        => status == CampaignStatuses.Paused && previous is CampaignStatuses.Published or CampaignStatuses.InProgress;
}

public static class ApplicationStatuses
{
    public const string Applied = "Applied";
    public const string Shortlisted = "Shortlisted";
    public const string Confirmed = "Confirmed";
    public const string NotSelected = "NotSelected";
    public const string Withdrawn = "Withdrawn";
    public const string CancelledByAdmin = "CancelledByAdmin";

    public static bool IsActive(string status) => status is Applied or Shortlisted or Confirmed;
}

public static class CampaignTransitions
{
    public static bool CanEdit(string status) => status is CampaignStatuses.Draft or CampaignStatuses.NeedEdit;
    public static bool CanEditImages(string status, bool workStarted) => !workStarted &&
        status is CampaignStatuses.Draft or CampaignStatuses.NeedEdit or CampaignStatuses.Submitted or CampaignStatuses.Published or CampaignStatuses.InProgress;
    public static bool CanSubmit(string status) => status is CampaignStatuses.Draft or CampaignStatuses.NeedEdit;
    public static bool CanReview(string status) => status == CampaignStatuses.Submitted;
    public static bool CanApply(string status) => status == CampaignStatuses.Published;
}

public static class CampaignTargetingRules
{
    public static string? Validate(CampaignTargetingInput target)
    {
        if (target.Gender is not ("Any" or "Female" or "Male")) return "Gender";
        if (target.MinimumFollowers is < 0 || target.MaximumFollowers is < 0 || target.MinimumFollowers > target.MaximumFollowers) return "Followers";
        if (target.MinimumEngagementRate is < 0 or > 100 || target.MaximumEngagementRate is < 0 or > 100 || target.MinimumEngagementRate > target.MaximumEngagementRate) return "Engagement";
        if (target.ProvinceIds.Distinct().Count() > 20 || target.CategoryIds.Distinct().Count() > 20) return "SelectionLimit";
        return null;
    }
}

public static class CampaignExecutionModes
{
    public const string RemoteProductReview = "RemoteProductReview";
    public const string FoodReviewOnsite = "FoodReviewOnsite";
    public const string StoreReviewOnsite = "StoreReviewOnsite";
    public const string ServiceReviewOnsite = "ServiceReviewOnsite";
    public const string EventOnsite = "EventOnsite";
    public const string LivestreamOnline = "LivestreamOnline";
    public static bool IsOnsite(string mode) => mode is FoodReviewOnsite or StoreReviewOnsite or ServiceReviewOnsite or EventOnsite;
    public static bool IsValid(string mode) => mode is RemoteProductReview or FoodReviewOnsite or StoreReviewOnsite or ServiceReviewOnsite or EventOnsite or LivestreamOnline;
}

public interface ICampaignService
{
    Task<IReadOnlyList<CampaignListItem>> GetBrandCampaignsAsync(Guid brandUserId, CancellationToken cancellationToken = default);
    Task<BrandCampaignPage> GetBrandCampaignPageAsync(Guid brandUserId, BrandCampaignPageRequest request, CancellationToken cancellationToken = default);
    Task<AdminCampaignPage> GetAdminCampaignPageAsync(BrandCampaignPageRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CampaignListItem>> GetReviewQueueAsync(CancellationToken cancellationToken = default);
    Task<CampaignDiscoveryPage> GetPublishedAsync(Guid? kocUserId, CampaignDiscoveryFilter? filter = null, CancellationToken cancellationToken = default);
    Task<CampaignResult> ToggleFavoriteAsync(Guid campaignId, Guid kocUserId, CancellationToken cancellationToken = default);
    Task<CampaignDetail?> GetForBrandAsync(Guid campaignId, Guid brandUserId, CancellationToken cancellationToken = default);
    Task<CampaignDetail?> GetForAdminAsync(Guid campaignId, CancellationToken cancellationToken = default);
    Task<CampaignDetail?> GetPublishedDetailAsync(Guid campaignId, Guid? kocUserId, CancellationToken cancellationToken = default);
    Task<CampaignTargetingOptions> GetTargetingOptionsAsync(CancellationToken cancellationToken = default);
    Task<CampaignResult> CreateDraftAsync(Guid brandUserId, CampaignUpsertRequest request, CancellationToken cancellationToken = default);
    Task<CampaignResult> UpdateDraftAsync(Guid campaignId, Guid brandUserId, CampaignUpsertRequest request, CancellationToken cancellationToken = default);
    Task<CampaignResult> UpdateImagesAsync(Guid campaignId, Guid brandUserId, string? avatarUrl, string? bannerUrl, CancellationToken cancellationToken = default);
    Task<CampaignResult> SubmitAsync(Guid campaignId, Guid brandUserId, CancellationToken cancellationToken = default);
    Task<CampaignResult> ReviewAsync(Guid campaignId, Guid adminUserId, CampaignReviewRequest request, CancellationToken cancellationToken = default);
    Task<CampaignResult> DeleteByAdminAsync(Guid campaignId, Guid adminUserId, string reason, CancellationToken cancellationToken = default);
}

public sealed record BrandCampaignPageRequest(string? Keyword, string? Status, int Start, int Length, int SortColumn, bool SortDescending);
public sealed record BrandCampaignSummary(int Draft, int Review, int Running, int Completed);
public sealed record BrandCampaignPage(IReadOnlyList<CampaignListItem> Items, int TotalCount, int FilteredCount, BrandCampaignSummary Summary);
public sealed record AdminCampaignSummary(int Total, int Draft, int PendingReview, int Running, int Completed, int NeedsAttention);
public sealed record AdminCampaignPage(IReadOnlyList<CampaignListItem> Items, int TotalCount, int FilteredCount, AdminCampaignSummary Summary);

public interface ICampaignApplicationService
{
    Task<CampaignResult> ApplyAsync(Guid campaignId, Guid kocUserId, string? pitchNote, decimal? proposedAmount, CancellationToken cancellationToken = default);
    Task<CampaignResult> WithdrawAsync(Guid applicationId, Guid kocUserId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<KocApplicationItem>> GetMineAsync(Guid kocUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
}

public sealed record CampaignUpsertRequest(
    string Title,
    string? Objective,
    decimal BudgetAmount,
    string PackageCode,
    bool RequiresBooking,
    bool RequiresShipment,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string DeliverableType,
    int DeliverableQuantity,
    DateTime? DeliverableDueDateUtc)
{
    public bool CheckoutAtCreation {get;init;}
    public bool BrandSelectionRequested {get;init;}

    public string? AvatarUrl { get; init; }
    public string? BannerUrl { get; init; }
    public IReadOnlyList<CampaignDeliverableInput> Deliverables { get; init; } = [];
    public CampaignTargetingInput Targeting { get; init; } = new();
    public CampaignExecutionInput Execution { get; init; } = new();
    public Guid? PackagePurchaseId { get; init; }
    public string AllocationMode { get; init; } = "Equal";
    public int DesiredKocCount { get; init; } = 1;
}
public sealed record CampaignExecutionInput
{
    public string Mode { get; init; } = CampaignExecutionModes.RemoteProductReview;
    public string? VenueName { get; init; }
    public string? VenueAddress { get; init; }
    public string? ContactName { get; init; }
    public string? ContactPhone { get; init; }
    public string? Schedule { get; init; }
    public string? BookingInstructions { get; init; }
    public string? ShippingInstructions { get; init; }
    public string? SupportDetails { get; init; }
    public string? Requirements { get; init; }
}
public sealed record CampaignExecutionItem(string Mode,string? VenueName,string? VenueAddress,string? ContactName,string? ContactPhone,string? Schedule,string? BookingInstructions,string? ShippingInstructions,string? SupportDetails,string? Requirements);
public sealed record CampaignTargetingInput
{
    public string Gender { get; init; } = "Any";
    public int? MinimumFollowers { get; init; }
    public int? MaximumFollowers { get; init; }
    public decimal? MinimumEngagementRate { get; init; }
    public decimal? MaximumEngagementRate { get; init; }
    public IReadOnlyList<Guid> ProvinceIds { get; init; } = [];
    public IReadOnlyList<Guid> CategoryIds { get; init; } = [];
}
public sealed record CampaignTargetingItem(string Gender,int? MinimumFollowers,int? MaximumFollowers,decimal? MinimumEngagementRate,decimal? MaximumEngagementRate,IReadOnlyList<TargetingOption> Provinces,IReadOnlyList<TargetingOption> Categories);
public sealed record TargetingOption(Guid Id,string Name);
public sealed record CampaignTargetingOptions(IReadOnlyList<TargetingOption> Provinces,IReadOnlyList<TargetingOption> Categories);
public sealed record CampaignTargetingMatch(bool IsMatch,IReadOnlyList<string> UnmatchedCriteria);
public sealed record CampaignDeliverableInput(string DeliverableType,int Quantity,DateTime? DueDateUtc);
public sealed record CampaignDeliverableItem(Guid Id,string DeliverableType,int Quantity,DateTime? DueDateUtc);

public sealed record CampaignReviewRequest(string Decision, string? ReviewNote)
{
    public IReadOnlyList<CampaignReviewFeedbackInput> FeedbackItems { get; init; } = [];
}
public sealed record CampaignReviewFeedbackInput(string FieldCode, string Message);
public sealed record CampaignReviewFeedbackItem(string FieldCode, string Message, int SortOrder);
public sealed record CampaignResult(bool Success, string Message, Guid? Id = null);
public sealed record CampaignDiscoveryFilter(string? Keyword,decimal? MinimumBudget,decimal? MaximumBudget,bool? RequiresBooking,bool? RequiresShipment,string? DeliverableType,string? Sort,int Page = 1,int PageSize = 12);
public sealed record CampaignDiscoveryPage(IReadOnlyList<CampaignListItem> Items,int TotalCount,int Page,int PageSize)
{
    public int TotalPages => Math.Max(1, (int)Math.Ceiling(TotalCount / (double)PageSize));
}

public sealed record LifecycleEvent(string? FromStatus, string ToStatus, string? Note, DateTime CreatedAtUtc);

public sealed record CampaignListItem(
    Guid Id,
    string Title,
    string BrandName,
    decimal BudgetAmount,
    string PackageCode,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int ApplicationCount,
    string? CurrentApplicationStatus)
{
    public int SelectedKocCount { get; init; }
    public decimal FundedBudget { get; init; }
    public bool HasWorkStarted { get; init; }
    public bool CanEditImages => CampaignTransitions.CanEditImages(Status, HasWorkStarted);
    public string? AvatarUrl { get; init; }
    public string? BannerUrl { get; init; }
    public IReadOnlyList<LifecycleEvent> History { get; init; } = [];
    public bool IsShortlistFinalized { get; init; }
    public bool RequiresBooking { get; init; }
    public bool RequiresShipment { get; init; }
    public string? DeliverableType { get; init; }
    public int DesiredKocCount { get; init; }
    public int DeliverableQuantity { get; init; }
    public DateTime? DeliverableDueDateUtc { get; init; }
    public CampaignTargetingMatch? CurrentKocTargetingMatch { get; init; }
    public bool IsFavorite { get; init; }
}

public sealed record CampaignDetail(
    Guid Id,
    string Title,
    string BrandName,
    string? Objective,
    decimal BudgetAmount,
    string PackageCode,
    bool RequiresBooking,
    bool RequiresShipment,
    string Status,
    DateOnly? StartDate,
    DateOnly? EndDate,
    string? ReviewNote,
    string DeliverableType,
    int DeliverableQuantity,
    DateTime? DeliverableDueDateUtc,
    int ApplicationCount,
    string? CurrentApplicationStatus)
{
    public string? AvatarUrl { get; init; }
    public string? BannerUrl { get; init; }
    public IReadOnlyList<CampaignDeliverableItem> Deliverables { get; init; } = [];
    public IReadOnlyList<CampaignReviewFeedbackItem> ReviewFeedbackItems { get; init; } = [];
    public CampaignTargetingItem? Targeting { get; init; }
    public CampaignExecutionItem? Execution { get; init; }
    public IReadOnlyList<LifecycleEvent> History { get; init; } = [];
    public CampaignTargetingMatch? CurrentKocTargetingMatch { get; init; }
    public string AllocationMode { get; init; } = "Equal";
    public int DesiredKocCount { get; init; } = 1;
    public bool IsFavorite { get; init; }
}

public sealed record KocApplicationItem(Guid Id, Guid CampaignId, string CampaignTitle, string BrandName, decimal BudgetAmount, decimal? ProposedAmount, string Status, DateTime AppliedAtUtc)
{
    public IReadOnlyList<LifecycleEvent> History { get; init; } = [];
}

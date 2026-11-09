namespace Topstar.Application.Campaigns;

public interface ICampaignWorkspaceService
{
    Task<CampaignWorkspace?> GetForBrandAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default);
    Task<CampaignWorkLaunchResult> OpenWorkAsync(Guid brandUserId, Guid campaignId, Guid? participationId, bool requireFullTeam, CancellationToken cancellationToken = default);
}

public sealed record CampaignWorkLaunchResult(bool Success, string Message, IReadOnlyList<Guid> ParticipationIds);

public sealed record CampaignWorkspace(
    Guid Id,
    string Title,
    string BrandName,
    string PackageCode,
    string TechnicalStatus,
    decimal Budget,
    DateOnly? StartDate,
    DateOnly? EndDate,
    int DesiredKocCount,
    int ActiveApplications,
    int ConfirmedKocs,
    bool IsShortlistFinalized,
    string? FundingStatus,
    decimal TotalPayable,
    string? OrderNumber,
    DateTime? FundingDueAtUtc,
    DateTime? PaidAtUtc,
    bool RequiresBooking,
    int ScheduleTotal,
    int SchedulePending,
    bool RequiresShipment,
    int ShipmentTotal,
    int ShipmentIssues,
    int SubmissionTotal,
    int SubmissionPending,
    int SubmissionNotSubmitted,
    int SubmissionRevisionPending,
    int OpenDisputes,
    string PhaseCode,
    string PhaseLabel,
    string PhaseDescription,
    string NextAction,
    string NextPage,
    string? NextSubPage,
    CampaignFinancialSummary FinancialSummary,
    IReadOnlyList<KocJourneyItem> KocJourneys)
{
    public int RecruitmentPercent => DesiredKocCount <= 0 ? 0 : Math.Min(100, ConfirmedKocs * 100 / DesiredKocCount);
    public bool IsFunded => FundingStatus is "Paid" or "PartiallyReleased" or "Released";
    public bool HasCompletedFundingStep => FundingStatus is "Paid" or "PartiallyReleased" or "Released" or "Refunded";
    public bool IsWaitingStart => PhaseCode == "waiting-start";
    public bool CanOperate => CampaignWorkspaceRules.CanOperate(TechnicalStatus, IsShortlistFinalized, FundingStatus, PhaseCode);
    public string ExecutionStepLabel => CampaignWorkspaceRules.ExecutionStepLabel(TechnicalStatus, IsShortlistFinalized, FundingStatus, PhaseCode);
    public bool IsExecutionComplete => TechnicalStatus == CampaignStatuses.Completed;
    public bool IsSettlementComplete => IsExecutionComplete && FundingStatus is Billing.CampaignFundingStatuses.Released or Billing.CampaignFundingStatuses.Refunded;
}

public sealed record CampaignFinancialSummary(
    decimal PackageCost,
    decimal KocBudget,
    decimal PlatformFee,
    decimal GatewayFee,
    decimal PaidAmount,
    decimal EscrowRemaining,
    decimal ReleasedAmount,
    decimal RefundAmount,
    decimal ActualCost);

public static class CampaignFinancialSummaryRules
{
    public static decimal EscrowRemaining(decimal kocBudget,decimal releasedAmount,decimal approvedRefundAmount)
        => Math.Max(0m,kocBudget-releasedAmount-approvedRefundAmount);

    public static decimal ActualCost(decimal packageCost,decimal paidCampaignAmount,decimal refundedAmount)
        => Math.Max(0m,packageCost+paidCampaignAmount-refundedAmount);
}

public sealed record KocJourneyItem(
    Guid ParticipationId,
    string KocName,
    string ParticipationStatus,
    string BookingStatus,
    string ShipmentStatus,
    string SubmissionStatus,
    DateTime? DueAtUtc,
    string WaitingFor,
    string NextAction)
{
    public DateTime? WorkOpenedAtUtc { get; init; }
    public bool IsWorkOpened => WorkOpenedAtUtc.HasValue;
    public bool IsFunded { get; init; }
    public bool CanOpenWork => ParticipationStatus == Topstar.Application.Matching.ParticipationStatuses.Confirmed && IsFunded && !IsWorkOpened;
}

public sealed record CampaignWorkspacePhaseInput(
    string Status,
    bool IsShortlistFinalized,
    string? FundingStatus,
    DateOnly? StartDate,
    DateOnly Today,
    int ConfirmedKocs,
    bool RequiresBooking,
    int ScheduleTotal,
    int SchedulePending,
    bool RequiresShipment,
    int ShipmentTotal,
    int ShipmentIssues,
    int SubmissionNotSubmitted,
    int SubmissionReviewPending,
    int SubmissionRevisionPending,
    int OpenDisputes);

public sealed record CampaignWorkspacePhase(
    string Code,
    string Label,
    string Description,
    string NextAction,
    string NextPage,
    string? NextSubPage = null);

public static class CampaignWorkspaceRules
{
    public static bool CanOperate(string status, bool shortlistFinalized, string? fundingStatus, string phaseCode)
        => status == CampaignStatuses.InProgress && shortlistFinalized
            && fundingStatus is (Billing.CampaignFundingStatuses.Paid or Billing.CampaignFundingStatuses.PartiallyReleased or Billing.CampaignFundingStatuses.Released)
            && phaseCode != "waiting-start";

    public static string ExecutionStepLabel(string status, bool shortlistFinalized, string? fundingStatus, string phaseCode)
    {
        if (status == CampaignStatuses.Completed) return "Đã hoàn tất";
        if (status == CampaignStatuses.Cancelled) return "Đã hủy";
        if (status == CampaignStatuses.Paused) return "Tạm ngừng";
        if (!shortlistFinalized || fundingStatus is not (Billing.CampaignFundingStatuses.Paid or Billing.CampaignFundingStatuses.PartiallyReleased or Billing.CampaignFundingStatuses.Released))
            return "Chưa đủ điều kiện triển khai";
        if (phaseCode == "waiting-start") return "Chờ ngày bắt đầu";
        if (phaseCode == "preparing") return "Đang chuẩn bị";
        return CanOperate(status, shortlistFinalized, fundingStatus, phaseCode) ? "Đang triển khai" : "Chưa bắt đầu";
    }

    public static CampaignWorkspacePhase Resolve(CampaignWorkspacePhaseInput input)
    {
        if(input.Status==CampaignStatuses.Paused) return new("warning","Tạm ngừng","Chiến dịch đang tạm ngừng. Tiền ký quỹ và xác nhận KOC được giữ nguyên; công việc chưa được mở.","Xem thông tin ký quỹ","CampaignFunding");
        if (input.Status == CampaignStatuses.Draft) return new("draft", "Bản nháp", "Hoàn thiện thông tin chiến dịch và gửi TopStar phê duyệt.", "Tiếp tục chỉnh sửa", "CampaignEdit", "profile");
        if (input.Status == CampaignStatuses.Submitted) return new("review", "Chờ TopStar duyệt", "Chiến dịch đang được kiểm tra trước khi tuyển KOC.", "Xem hồ sơ chiến dịch", "CampaignEdit", "profile");
        if (input.Status == CampaignStatuses.NeedEdit) return new("warning", "Cần cập nhật", "TopStar đã gửi phản hồi cần xử lý.", "Cập nhật chiến dịch", "CampaignEdit", "profile");
        if (input.Status == CampaignStatuses.Rejected) return new("danger", "Bị từ chối", "Xem phản hồi để biết lý do chiến dịch chưa được chấp thuận.", "Xem phản hồi", "CampaignEdit", "profile");
        if (input.Status == CampaignStatuses.Cancelled) return new("muted", "Đã hủy", "Chiến dịch đã kết thúc theo quyết định hủy.", "Xem hồ sơ", "CampaignEdit", "profile");
        if (input.Status == CampaignStatuses.Completed && input.OpenDisputes > 0)
            return new("warning", "Có yêu cầu cần xử lý", $"Có {input.OpenDisputes} hồ sơ hỗ trợ hoặc tranh chấp đang mở trước khi quyết toán.", "Xử lý hỗ trợ", "Disputes", "disputes");
        if (input.Status == CampaignStatuses.Completed && input.FundingStatus is Billing.CampaignFundingStatuses.Paid or Billing.CampaignFundingStatuses.PartiallyReleased)
            return new("settlement", "Chờ nghiệm thu và giải ngân", "Công việc đã hoàn thành; theo dõi nghiệm thu, giải ngân thù lao và hoàn tiền nếu có.", "Theo dõi nghiệm thu", "CampaignFunding");
        if (input.Status == CampaignStatuses.Completed)
            return new("completed", "Đã hoàn thành", "Đã hoàn tất thực hiện và quyết toán; có thể xem báo cáo và đánh giá KOC.", "Xem báo cáo", "Reports", "reports");

        if (!input.IsShortlistFinalized)
            return new("recruiting", "Đang tuyển chọn KOC", "Danh sách KOC tham gia chiến dịch chưa được xác nhận. Vui lòng xem tiến độ tại tab KOC.", "Tuyển chọn KOC", "Shortlist");

        if (input.FundingStatus == Billing.CampaignFundingStatuses.Pending)
            return new("payment", "Chờ xác nhận thanh toán", "Yêu cầu ký quỹ đã gửi và đang chờ đối soát.", "Xem thanh toán", "CampaignFunding");

        var funded = input.FundingStatus is Billing.CampaignFundingStatuses.Paid
            or Billing.CampaignFundingStatuses.PartiallyReleased
            or Billing.CampaignFundingStatuses.Released;
        if (!funded)
            return new("payment", "Chờ ký quỹ", "Danh sách KOC đã được xác nhận. Vui lòng phân bổ thù lao và hoàn tất ký quỹ để triển khai chiến dịch.", "Phân bổ thù lao và ký quỹ", "CampaignFunding");

        if (input.OpenDisputes > 0)
            return new("warning", "Có yêu cầu cần xử lý", $"Có {input.OpenDisputes} hồ sơ hỗ trợ hoặc tranh chấp đang mở.", "Xử lý hỗ trợ", "Disputes", "disputes");
        if (input.ShipmentIssues > 0)
            return new("warning", "Có sự cố giao nhận", $"Có {input.ShipmentIssues} vận đơn cần xử lý.", "Xử lý giao nhận", "Shipments", "shipments");
        if (input.RequiresShipment && input.ShipmentTotal < input.ConfirmedKocs)
            return new("preparing", "Chuẩn bị sản phẩm", $"Cần tạo hoặc hoàn thiện giao nhận cho {input.ConfirmedKocs - input.ShipmentTotal} KOC.", "Tạo vận đơn", "Shipments", "shipments");
        if (input.RequiresBooking && (input.ScheduleTotal < input.ConfirmedKocs || input.SchedulePending > 0))
            return new("preparing", "Sắp xếp lịch thực hiện", "Lịch làm việc với KOC chưa được xác nhận đầy đủ.", "Quản lý lịch", "Bookings", "bookings");
        if (input.StartDate.HasValue && input.Today < input.StartDate.Value)
            return new("waiting-start", "Đã ký quỹ – Chờ bắt đầu", $"Công tác chuẩn bị đã hoàn tất; chiến dịch bắt đầu ngày {input.StartDate:dd/MM/yyyy}.", "Xem kế hoạch thực hiện", "Deliverables", "deliverables");
        if (input.SubmissionReviewPending > 0)
            return new("running", "Đang thực hiện", $"Có {input.SubmissionReviewPending} nội dung đang chờ Brand duyệt.", "Duyệt nội dung", "Deliverables", "deliverables");
        if (input.SubmissionRevisionPending > 0)
            return new("running", "Đang thực hiện", $"Có {input.SubmissionRevisionPending} nội dung KOC đang chỉnh sửa.", "Theo dõi nội dung", "Deliverables", "deliverables");
        if (input.SubmissionNotSubmitted > 0)
            return new("running", "Đang thực hiện", $"Đang chờ {input.SubmissionNotSubmitted} nội dung từ KOC.", "Theo dõi nội dung", "Deliverables", "deliverables");

        return new("running", "Đang thực hiện", "Quỹ đã được bảo đảm; các công việc đang được triển khai.", "Theo dõi thực hiện", "Deliverables", "deliverables");
    }
}

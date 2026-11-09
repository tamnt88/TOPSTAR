using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using Topstar.Application.Campaigns;
using Topstar.Application.Matching;
using Topstar.Application.Operations;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class CampaignReviewModel(
    ICampaignService campaignService,
    IMatchingService matchingService,
    ICampaignFundingService fundingService,
    IOperationsAdminService operationsService,
    TopstarDbContext db,
    IFileStorageService storage) : PageModel
{
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty(SupportsGet = true)] public string Tab { get; set; } = "overview";
    [BindProperty] public ReviewInput Input { get; set; } = new();
    [BindProperty] public List<Guid> SelectedApplicationIds { get; set; } = [];
    [BindProperty] public List<Guid> SelectedKocProfileIds { get; set; } = [];
    [BindProperty, StringLength(500)] public string? MatchingNote { get; set; }
    public CampaignDetail? Campaign { get; private set; }
    public AdminMatchingDetail? Matching { get; private set; }
    public CampaignFundingItem? Funding { get; private set; }
    public OperationsAdminDashboard Operations { get; private set; } = new([], [], [], [], [], [], [], [], []);
    public IReadOnlyList<CampaignFundingEvidenceEntity> FundingEvidences { get; private set; } = [];
    public IReadOnlyDictionary<Guid, CampaignFundingEvidenceReviewEntity> LatestEvidenceReviews { get; private set; }
        = new Dictionary<Guid, CampaignFundingEvidenceReviewEntity>();
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
        => await LoadAsync(cancellationToken) ? Page() : NotFound();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        Tab = "approval";
        if (!ModelState.IsValid) return await ReloadPageAsync(cancellationToken);
        var result = await campaignService.ReviewAsync(Id, UserId(), new(Input.Decision, Input.ReviewNote)
        {
            FeedbackItems = Input.Feedback.Where(x => x.Selected)
                .Select(x => new CampaignReviewFeedbackInput(x.FieldCode, x.Message ?? string.Empty)).ToList()
        }, cancellationToken);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return await ReloadPageAsync(cancellationToken);
        }
        Message = result.Message;
        MessageSuccess = true;
        return RedirectToPage(new { id = Id, tab = "approval" });
    }

    public async Task<IActionResult> OnPostShortlistAsync(CancellationToken cancellationToken)
    {
        var campaign = await campaignService.GetForAdminAsync(Id, cancellationToken);
        if (campaign?.Status != CampaignStatuses.Published)
            return Finish(false, "Chỉ có thể gửi danh sách KOC khi chiến dịch đang tuyển chọn.", "matching");
        var result = await matchingService.SendShortlistAsync(UserId(), Id, SelectedApplicationIds, MatchingNote, cancellationToken);
        return Finish(result.Success, result.Message, "matching");
    }

    public async Task<IActionResult> OnPostAssignDirectAsync(CancellationToken cancellationToken)
    {
        var campaign = await campaignService.GetForAdminAsync(Id, cancellationToken);
        if (campaign?.Status != CampaignStatuses.Published)
            return Finish(false, "Chỉ có thể bổ sung KOC khi chiến dịch đang tuyển chọn.", "matching");
        var result = await matchingService.AssignDirectAsync(UserId(), Id, SelectedKocProfileIds, MatchingNote, cancellationToken);
        return Finish(result.Success, result.Message, "matching");
    }

    public async Task<IActionResult> OnPostRemoveAssignedAsync(Guid applicationId, CancellationToken cancellationToken)
    {
        var result = await matchingService.RemoveAssignedAsync(UserId(), Id, applicationId, cancellationToken);
        return Finish(result.Success, result.Message, "matching");
    }

    public async Task<IActionResult> OnPostConfirmPaidAsync(Guid fundingId, string reference, string? manualReason, CancellationToken cancellationToken)
    {
        var funding = (await fundingService.GetForAdminAsync(cancellationToken)).FirstOrDefault(x => x.Id == fundingId && x.CampaignId == Id);
        if (funding?.Status != CampaignFundingStatuses.Pending)
            return Finish(false, "Yêu cầu ký quỹ không còn ở trạng thái chờ đối soát.", "finance");
        reference = (reference ?? string.Empty).Trim();
        if (reference.Length is < 3 or > 200)
        {
            Message = "Vui lòng nhập mã giao dịch từ 3 đến 200 ký tự.";
            MessageSuccess = false;
            return RedirectToPage(new { id = Id, tab = "finance" });
        }
        var result = await fundingService.ConfirmPaidAsync(UserId(), fundingId, reference, manualReason, cancellationToken);
        return Finish(result.Success, result.Message, "finance");
    }

    public async Task<IActionResult> OnPostReviewEvidenceAsync(Guid fundingId, Guid evidenceId, string reviewStatus, string reviewReason, CancellationToken ct)
    {
        var belongsToCampaign = await (from evidence in db.CampaignFundingEvidences.AsNoTracking()
                                       join funding in db.CampaignFundings.AsNoTracking() on evidence.FundingId equals funding.Id
                                       where evidence.Id == evidenceId && evidence.FundingId == fundingId && funding.CampaignId == Id
                                       select evidence.Id).AnyAsync(ct);
        if (!belongsToCampaign) return Finish(false, "Chứng từ không thuộc chiến dịch đang mở.", "finance");
        reviewReason = (reviewReason ?? string.Empty).Trim();
        if (reviewReason.Length is < 10 or > 500) return Finish(false, "Vui lòng nhập phản hồi chứng từ từ 10 đến 500 ký tự.", "finance");
        var result = await fundingService.ReviewEvidenceAsync(UserId(), evidenceId, reviewStatus, reviewReason, ct);
        return Finish(result.Success, result.Message, "finance");
    }

    public async Task<IActionResult> OnPostReconciliationEvidenceAsync(Guid fundingId, string reference, decimal? amount, string note, IFormFile? evidenceFile, CancellationToken ct)
    {
        var funding = (await fundingService.GetForAdminAsync(ct)).FirstOrDefault(x => x.Id == fundingId && x.CampaignId == Id);
        if (funding is null || funding.Status != CampaignFundingStatuses.Pending || funding.PaymentMethod != CampaignPaymentMethods.BankTransfer)
            return Finish(false, "Yêu cầu ký quỹ không thuộc chiến dịch hoặc không còn chờ đối soát.", "finance");
        reference = (reference ?? string.Empty).Trim();
        note = (note ?? string.Empty).Trim();
        if (reference.Length is < 3 or > 200) return Finish(false, "Vui lòng nhập mã giao dịch từ 3 đến 200 ký tự.", "finance");
        if (!amount.HasValue || amount <= 0) return Finish(false, "Vui lòng nhập số tiền thực nhận lớn hơn 0.", "finance");
        if (note.Length is < 10 or > 500) return Finish(false, "Vui lòng nhập ghi chú đối soát từ 10 đến 500 ký tự.", "finance");
        var url = await storage.SaveCampaignFundingEvidenceAsync(evidenceFile, ct);
        if (url is null) return Finish(false, "Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB.", "finance");
        CampaignFundingResult result;
        try
        {
            result = await fundingService.AddAdminEvidenceAsync(UserId(), fundingId, CampaignFundingEvidenceTypes.Reconciliation, url, reference, amount, note, ct);
        }
        catch
        {
            await storage.DeleteCampaignFundingEvidenceAsync(url, ct);
            return Finish(false, "Không thể lưu chứng từ đối soát vào lúc này.", "finance");
        }
        if (!result.Success) await storage.DeleteCampaignFundingEvidenceAsync(url, ct);
        return Finish(result.Success, result.Message, "finance");
    }

    public async Task<IActionResult> OnPostResolveShipmentAsync(Guid shipmentId, string decision, string note, CancellationToken ct)
    {
        var operations = await operationsService.GetCampaignDashboardAsync(Id, ct);
        if (!operations.ShipmentIssues.Any(x => x.ShipmentId == shipmentId))
            return Finish(false, "Vận đơn không thuộc chiến dịch đang mở hoặc không còn cần xử lý.", "operations");
        note = (note ?? string.Empty).Trim();
        if (note.Length is < 10 or > 500) return Finish(false, "Vui lòng nhập ghi chú xử lý từ 10 đến 500 ký tự.", "operations");
        var result = await operationsService.ResolveShipmentAsync(UserId(), shipmentId, decision, note, ct);
        return Finish(result.Success, result.Message, "operations");
    }

    public async Task<IActionResult> OnPostCancelScheduleAsync(Guid scheduleId, string reason, CancellationToken ct)
    {
        var operations = await operationsService.GetCampaignDashboardAsync(Id, ct);
        if (!operations.ScheduleAttention.Any(x => x.ScheduleId == scheduleId))
            return Finish(false, "Lịch không thuộc chiến dịch đang mở hoặc không còn cần xử lý.", "operations");
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length is < 10 or > 500) return Finish(false, "Vui lòng nhập lý do hủy lịch từ 10 đến 500 ký tự.", "operations");
        var result = await operationsService.CancelScheduleAsync(UserId(), scheduleId, reason, ct);
        return Finish(result.Success, result.Message, "operations");
    }

    public async Task<IActionResult> OnPostAcknowledgeEscalationAsync(Guid escalationId, string note, CancellationToken ct)
    {
        var operations = await operationsService.GetCampaignDashboardAsync(Id, ct);
        var belongsToCampaign = operations.SubmissionEscalations.Any(x => x.EscalationId == escalationId)
            || operations.BrandReviewEscalations.Any(x => x.EscalationId == escalationId)
            || operations.DisputeEscalations.Any(x => x.EscalationId == escalationId);
        if (!belongsToCampaign)
            return Finish(false, "Cảnh báo không thuộc chiến dịch đang mở hoặc đã được xử lý.", "operations");
        note = (note ?? string.Empty).Trim();
        if (note.Length is < 10 or > 500) return Finish(false, "Vui lòng nhập ghi chú tiếp nhận từ 10 đến 500 ký tự.", "operations");
        var result = await operationsService.AcknowledgeEscalationAsync(UserId(), escalationId, note, ct);
        return Finish(result.Success, result.Message, "operations");
    }

    private async Task<bool> LoadAsync(CancellationToken ct)
    {
        Campaign = await campaignService.GetForAdminAsync(Id, ct);
        if (Campaign is null) return false;
        Matching = await matchingService.GetAdminDetailAsync(Id, ct);
        Funding = (await fundingService.GetForAdminAsync(ct)).Where(x => x.CampaignId == Id)
            .OrderByDescending(x => x.VersionNo).FirstOrDefault();
        if (Funding?.Id is Guid fundingId)
        {
            FundingEvidences = await db.CampaignFundingEvidences.AsNoTracking()
                .Where(x => x.FundingId == fundingId).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct);
            var evidenceIds = FundingEvidences.Select(x => x.Id).ToList();
            LatestEvidenceReviews = (await db.CampaignFundingEvidenceReviews.AsNoTracking()
                    .Where(x => evidenceIds.Contains(x.EvidenceId)).OrderByDescending(x => x.CreatedAtUtc).ToListAsync(ct))
                .GroupBy(x => x.EvidenceId).ToDictionary(x => x.Key, x => x.First());
        }
        Operations = await operationsService.GetCampaignDashboardAsync(Id, ct);
        return true;
    }

    private async Task<IActionResult> ReloadPageAsync(CancellationToken ct)
        => await LoadAsync(ct) ? Page() : NotFound();

    private IActionResult Finish(bool success, string message, string tab)
    {
        Message = message;
        MessageSuccess = success;
        return RedirectToPage(new { id = Id, tab });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public sealed class ReviewInput
    {
        [Required] public string Decision { get; set; } = string.Empty;
        [StringLength(1000)] public string? ReviewNote { get; set; }
        public List<FeedbackInput> Feedback { get; set; } = FeedbackInput.Defaults();
    }

    public sealed class FeedbackInput
    {
        public bool Selected { get; set; }
        public string FieldCode { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
        [StringLength(500)] public string? Message { get; set; }
        public static List<FeedbackInput> Defaults() =>
        [
            new() { FieldCode = "Title", Label = "Tên chiến dịch" },
            new() { FieldCode = "Brief", Label = "Mục tiêu / brief" },
            new() { FieldCode = "Budget", Label = "Ngân sách / gói" },
            new() { FieldCode = "Schedule", Label = "Thời gian" },
            new() { FieldCode = "Deliverables", Label = "Nội dung bàn giao / hạn nộp" },
            new() { FieldCode = "Targeting", Label = "Đối tượng KOC" },
            new() { FieldCode = "Logistics", Label = "Booking / giao nhận" },
            new() { FieldCode = "Other", Label = "Nội dung khác" }
        ];
    }
}

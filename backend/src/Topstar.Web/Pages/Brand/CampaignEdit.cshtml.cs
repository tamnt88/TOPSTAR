using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Campaigns;
using Topstar.Application.Profiles;
using Topstar.Application.Matching;
using Topstar.Web.Services.Storage;
using Topstar.Application.Settings;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class CampaignEditModel(
    IProfileAccessService profileAccessService,
    ICampaignService campaignService,
    IPackagePolicyAdminService packagePolicyService,
    IFileStorageService fileStorageService,IOperationalSetupService setup,Topstar.Infrastructure.Persistence.TopstarDbContext db) : BrandGuardedPageModel(profileAccessService)
{
    [BindProperty(SupportsGet = true)] public Guid? Id { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? PackageId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? PurchaseId { get; set; }
    [BindProperty(SupportsGet=true)] public decimal Budget {get;set;}
    [BindProperty(SupportsGet=true)] public int KocCount {get;set;}=1;
    [BindProperty(SupportsGet=true)] public bool SelfSelect {get;set;}
    public bool NeedsCheckout {get;private set;}
    [BindProperty] public InputModel Input { get; set; } = new();
    [BindProperty] public IFormFile? AvatarFile { get; set; }
    [BindProperty] public IFormFile? BannerFile { get; set; }
    public string? CurrentAvatarUrl { get; private set; }
    public string? CurrentBannerUrl { get; private set; }
    public string Status { get; private set; } = CampaignStatuses.Draft;
    public string? ReviewNote { get; private set; }
    public IReadOnlyList<CampaignReviewFeedbackItem> ReviewFeedbackItems { get; private set; } = [];
    public CampaignTargetingOptions TargetingOptions { get; private set; } = new([], []);
    public IReadOnlyList<OperationalCatalogItem> DeliverableTypes { get; private set; }=[];
    public string? PackageName { get; private set; }
    public int? PackageKocQuota { get; private set; }
    public bool PackageAllowsBrandSelection { get; private set; }
    [TempData] public string? Message { get; set; }
    [TempData] public bool OfferSubmission { get; set; }
    [TempData] public bool SubmissionFailed { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        TargetingOptions = await campaignService.GetTargetingOptionsAsync(cancellationToken);
        DeliverableTypes=await setup.GetCatalogAsync(OperationalCatalogTypes.Deliverable,true,cancellationToken);
        if (!Id.HasValue)
        {
            if(!PurchaseId.HasValue){
                var policy=await Topstar.Infrastructure.Finance.CampaignCreationRules.ReadAsync(db,cancellationToken);
                if(policy is null||Topstar.Infrastructure.Finance.CampaignCreationRules.Validate(policy,Budget,KocCount,SelfSelect)is not null)return RedirectToPage("/Brand/CampaignNeeds");
                Input.CheckoutAtCreation=true;Input.BrandSelectionRequested=SelfSelect;Input.BudgetAmount=Budget;Input.DesiredKocCount=KocCount;Input.PackageCode=SelfSelect?"CUSTOM_SELF":"CUSTOM_ASSIGNED";NeedsCheckout=true;PackageKocQuota=KocCount;PackageAllowsBrandSelection=SelfSelect;return Page();
            }

            var purchaseService = HttpContext.RequestServices.GetRequiredService<IBrandPackagePurchaseService>();
            var purchase = await purchaseService.GetUsableAsync(UserId(), PurchaseId.Value, cancellationToken);
            if (purchase is null) return RedirectToPage("/Brand/Packages", new { required = true });
            var selectedPackage = await packagePolicyService.GetByIdAsync(purchase.PackagePolicyId, cancellationToken);
            if (selectedPackage is null) return RedirectToPage("/Brand/Packages", new { required = true });
            Input.PackagePurchaseId = purchase.Id;
            Input.PackageCode = purchase.PackageCode;
            Input.BudgetAmount = 0;
            SetPackageRule(purchase, selectedPackage);
            return Page();
        }
        var item = await campaignService.GetForBrandAsync(Id.Value, UserId(), cancellationToken);
        if (item is null) return NotFound();
        Status = item.Status; ReviewNote = item.ReviewNote; ReviewFeedbackItems = item.ReviewFeedbackItems;
        CurrentAvatarUrl = item.AvatarUrl;
        CurrentBannerUrl = item.BannerUrl;
        var deliverables = item.Deliverables
            .Select(x => new DeliverableInputModel { DeliverableType = x.DeliverableType, Quantity = x.Quantity, DueDateUtc = x.DueDateUtc })
            .ToList();
        Input = new() { Title = item.Title, Objective = item.Objective, BudgetAmount = item.BudgetAmount, AllocationMode = item.AllocationMode, DesiredKocCount = item.DesiredKocCount, PackageCode = item.PackageCode, RequiresBooking = item.RequiresBooking, RequiresShipment = item.RequiresShipment, StartDate = item.StartDate, EndDate = item.EndDate, Deliverables = deliverables.Count > 0 ? deliverables : [new()], Targeting = TargetingInputModel.From(item.Targeting), Execution = ExecutionInputModel.From(item.Execution) };
        await LoadPackageRuleAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        await LoadPackageRuleAsync(cancellationToken);
        if (PackageKocQuota.HasValue && Input.DesiredKocCount > PackageKocQuota.Value)
            ModelState.AddModelError("Input.DesiredKocCount", $"Gói {PackageName} cho phép xác nhận tối đa {PackageKocQuota.Value} KOC.");
        if (Input.StartDate.HasValue && Input.EndDate.HasValue && Input.EndDate.Value < Input.StartDate.Value)
            ModelState.AddModelError("Input.EndDate", "Ngày kết thúc phải bằng hoặc sau ngày bắt đầu.");
        for(var index=0;index<Input.Deliverables.Count;index++)
        {
            var error=!Input.Deliverables[index].DueDateUtc.HasValue ? "Vui lòng chọn ngày và giờ hạn nộp." : CampaignScheduleRules.ValidateDeadline(Input.StartDate,Input.EndDate,Input.Deliverables[index].DueDateUtc);
            if(error is not null)ModelState.AddModelError($"Input.Deliverables[{index}].DueDateUtc",error);
        }
        if(Input.StartDate.HasValue&&Input.StartDate.Value<CampaignScheduleRules.TodayVietnam)
            ModelState.AddModelError("Input.StartDate","Ngày bắt đầu dự kiến phải từ hôm nay trở đi.");
        for(var index=0;index<Input.Deliverables.Count;index++)
            if(Input.Deliverables[index].DueDateUtc is {} due&&DateOnly.FromDateTime(due)<CampaignScheduleRules.TodayVietnam)
                ModelState.AddModelError($"Input.Deliverables[{index}].DueDateUtc","Hạn nộp phải từ hôm nay trở đi.");
        ValidateExecutionInput();
        if (!ModelState.IsValid)
        {
            await LoadDisplayStateAsync(cancellationToken);
            return Page();
        }
        var request = Input.ToRequest();
        if (Id.HasValue)
        {
            var existing = await campaignService.GetForBrandAsync(Id.Value, UserId(), cancellationToken);
            if (existing is null) return NotFound();
            if (!CampaignTransitions.CanEdit(existing.Status)) return Forbid();
        }
        string? avatarUrl = null, bannerUrl = null;
        var imagesCommitted = false;
        try
        {
        if (AvatarFile is { Length: > 0 })
        {
            avatarUrl = await fileStorageService.SaveCampaignImageAsync(AvatarFile, cancellationToken);
            if (avatarUrl is null) ModelState.AddModelError(nameof(AvatarFile), "Ảnh đại diện phải là JPG, PNG hoặc WEBP, tối đa 5 MB.");
        }
        if (BannerFile is { Length: > 0 })
        {
            bannerUrl = await fileStorageService.SaveCampaignImageAsync(BannerFile, cancellationToken);
            if (bannerUrl is null) ModelState.AddModelError(nameof(BannerFile), "Banner phải là JPG, PNG hoặc WEBP, tối đa 5 MB.");
        }
        if (!ModelState.IsValid)
        {
            await LoadDisplayStateAsync(cancellationToken);
            return Page();
        }
        request = request with { AvatarUrl = avatarUrl, BannerUrl = bannerUrl };
        var result = Id.HasValue
            ? await campaignService.UpdateDraftAsync(Id.Value, UserId(), request, cancellationToken)
            : await campaignService.CreateDraftAsync(UserId(), request, cancellationToken);
        if (!result.Success)
        {
            var key = result.Message.StartsWith("Loại nội dung", StringComparison.OrdinalIgnoreCase)
                ? "Input.Deliverables[0].DeliverableType"
                : result.Message.Contains("xác nhận tối đa", StringComparison.OrdinalIgnoreCase)
                    ? "Input.DesiredKocCount" : string.Empty;
            ModelState.AddModelError(key, result.Message);
            await LoadDisplayStateAsync(cancellationToken);
            return Page();
        }
        imagesCommitted = true;
        Message = result.Message;
        var newFlow=await db.Campaigns.FindAsync(new object[]{result.Id!.Value},cancellationToken);
        if(newFlow?.CheckoutAtCreation==true&&newFlow.Status==CampaignStatuses.NeedEdit){var submitted=await campaignService.SubmitAsync(newFlow.Id,UserId(),cancellationToken);Message=submitted.Message;SubmissionFailed=!submitted.Success;return RedirectToPage("/Brand/CampaignEdit",new{id=newFlow.Id});}
        if(newFlow?.CheckoutAtCreation==true)return RedirectToPage("/Brand/CampaignCheckout",new{campaignId=result.Id});
        OfferSubmission = true;
        return RedirectToPage("/Brand/CampaignEdit", new { id = result.Id });
        }
        finally
        {
            if (!imagesCommitted)
            {
                await fileStorageService.DeleteCampaignImageAsync(avatarUrl, CancellationToken.None);
                await fileStorageService.DeleteCampaignImageAsync(bannerUrl, CancellationToken.None);
            }
        }
    }

    public async Task<IActionResult> OnPostSubmitAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        if (!Id.HasValue) return BadRequest();
        var result = await campaignService.SubmitAsync(Id.Value, UserId(), cancellationToken);
        Message = result.Message;
        SubmissionFailed = !result.Success;
        OfferSubmission = false;
        return RedirectToPage("/Brand/CampaignEdit", new { id = Id.Value });
    }

    public async Task<JsonResult> OnPostEditorImageAsync(IFormFile? upload, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return new JsonResult(new { error = new { message = "Hồ sơ Brand chưa đủ điều kiện thao tác." } }) { StatusCode = 403 };
        var url = await fileStorageService.SaveCampaignImageAsync(upload, cancellationToken);
        return url is null
            ? new JsonResult(new { error = new { message = "Ảnh phải là JPG, PNG hoặc WEBP và không vượt quá 5 MB." } }) { StatusCode = 400 }
            : new JsonResult(new { url });
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    private void ValidateExecutionInput()
    {
        var execution = Input.Execution;
        if (!CampaignExecutionModes.IsValid(execution.Mode))
            ModelState.AddModelError("Input.Execution.Mode", "Vui lòng chọn hình thức thực hiện hợp lệ.");
        if (Input.CheckoutAtCreation && !string.IsNullOrWhiteSpace(execution.ContactPhone) && !System.Text.RegularExpressions.Regex.IsMatch(execution.ContactPhone.Trim(), @"^[0-9+\s().-]{8,20}$"))
            ModelState.AddModelError("Input.Execution.ContactPhone", "Số điện thoại liên hệ không đúng định dạng.");
        if (!Input.CheckoutAtCreation && CampaignExecutionModes.IsOnsite(execution.Mode))
        {
            if (string.IsNullOrWhiteSpace(execution.VenueName)) ModelState.AddModelError("Input.Execution.VenueName", "Vui lòng nhập tên địa điểm.");
            if (string.IsNullOrWhiteSpace(execution.VenueAddress)) ModelState.AddModelError("Input.Execution.VenueAddress", "Vui lòng nhập địa chỉ thực hiện.");
            if (string.IsNullOrWhiteSpace(execution.ContactName)) ModelState.AddModelError("Input.Execution.ContactName", "Vui lòng nhập người liên hệ.");
            if (string.IsNullOrWhiteSpace(execution.ContactPhone)) ModelState.AddModelError("Input.Execution.ContactPhone", "Vui lòng nhập số điện thoại liên hệ.");
            else if (!System.Text.RegularExpressions.Regex.IsMatch(execution.ContactPhone.Trim(), @"^[0-9+\s().-]{8,20}$")) ModelState.AddModelError("Input.Execution.ContactPhone", "Số điện thoại liên hệ không đúng định dạng.");
            if (string.IsNullOrWhiteSpace(execution.Schedule)) ModelState.AddModelError("Input.Execution.Schedule", "Vui lòng nhập khung thời gian KOC có thể đến.");
        }

        if (!Input.CheckoutAtCreation && execution.Mode == CampaignExecutionModes.LivestreamOnline && string.IsNullOrWhiteSpace(execution.Schedule))
            ModelState.AddModelError("Input.Execution.Schedule", "Vui lòng nhập lịch livestream dự kiến.");
    }

    private async Task LoadDisplayStateAsync(CancellationToken cancellationToken)
    {
        TargetingOptions = await campaignService.GetTargetingOptionsAsync(cancellationToken);
        DeliverableTypes=await setup.GetCatalogAsync(OperationalCatalogTypes.Deliverable,true,cancellationToken);
        if (!Id.HasValue) return;
        var item = await campaignService.GetForBrandAsync(Id.Value, UserId(), cancellationToken);
        if (item is null) return;
        Status = item.Status;
        CurrentAvatarUrl = item.AvatarUrl;
        CurrentBannerUrl = item.BannerUrl;
        ReviewNote = item.ReviewNote;
        ReviewFeedbackItems = item.ReviewFeedbackItems;
    }

    private async Task LoadPackageRuleAsync(CancellationToken cancellationToken)
    {
        if(Id.HasValue){
            var existing=await db.Campaigns.FindAsync(new object[]{Id.Value},cancellationToken);
            if(existing?.CheckoutAtCreation==true){NeedsCheckout=true;PackageKocQuota=existing.SelectionQuotaSnapshot;PackageAllowsBrandSelection=existing.BrandSelectionSnapshot;Input.CheckoutAtCreation=true;Input.BrandSelectionRequested=existing.BrandSelectionSnapshot;return;}
        }else if(Input.CheckoutAtCreation){NeedsCheckout=true;var creationPolicy=await Topstar.Infrastructure.Finance.CampaignCreationRules.ReadAsync(db,cancellationToken);if(creationPolicy is not null){PackageKocQuota=Topstar.Infrastructure.Finance.CampaignCreationRules.MaximumKoc(creationPolicy,Input.BudgetAmount);PackageAllowsBrandSelection=Input.BrandSelectionRequested;}return;}
        var purchaseService = HttpContext.RequestServices.GetRequiredService<IBrandPackagePurchaseService>();
        BrandPackagePurchaseItem? purchase = null;
        if (Input.PackagePurchaseId.HasValue)
            purchase = await purchaseService.GetUsableAsync(UserId(), Input.PackagePurchaseId.Value, cancellationToken);
        if (purchase is null && Id.HasValue)
            purchase = (await purchaseService.GetForBrandAsync(UserId(), cancellationToken)).FirstOrDefault(x => x.CampaignId == Id.Value);
        if (purchase is null) return;
        var policy = purchase.SelectionQuota.HasValue ? null : await packagePolicyService.GetByIdAsync(purchase.PackagePolicyId, cancellationToken);
        SetPackageRule(purchase, policy);
    }

    private void SetPackageRule(BrandPackagePurchaseItem purchase, PackagePolicyItem? policy)
    {
        PackageName = purchase.PackageName;
        PackageKocQuota = purchase.SelectionQuota ?? policy?.SelectionQuota;
        PackageAllowsBrandSelection = purchase.SelectionQuota.HasValue ? purchase.BrandSelectionEnabled : policy?.BrandSelectionEnabled ?? purchase.BrandSelectionEnabled;
    }

    public string FeedbackLabel(string code) => code switch { "Title" => "Tên chiến dịch", "Brief" => "Mục tiêu / brief", "Budget" => "Ngân sách / gói", "Schedule" => "Thời gian", "Deliverables" => "Deliverable / deadline", "Targeting" => "Đối tượng KOC", "Logistics" => "Booking / giao hàng", _ => "Khác" };

    public sealed class InputModel
    {
        public bool CheckoutAtCreation {get;set;}
        public bool BrandSelectionRequested {get;set;}

        [Required(ErrorMessage = "Vui lòng nhập tên chiến dịch."), StringLength(200, ErrorMessage = "Tên chiến dịch không được vượt quá 200 ký tự.")] public string Title { get; set; } = string.Empty;
        [StringLength(20000, ErrorMessage = "Nội dung chiến dịch không được vượt quá 20.000 ký tự.")] public string? Objective { get; set; }
        [Range(1, double.MaxValue, ErrorMessage = "Ngân sách phải lớn hơn 0.")] public decimal BudgetAmount { get; set; }
        [Required, RegularExpression("^(Equal|Custom)$", ErrorMessage = "Vui lòng chọn cách phân bổ thù lao.")] public string AllocationMode { get; set; } = "Equal";
        [Range(1, 100, ErrorMessage = "Số lượng KOC mong muốn phải từ 1 đến 100.")] public int DesiredKocCount { get; set; } = 1;
        [Required, StringLength(30)] public string PackageCode { get; set; } = "STANDARD";
        public Guid? PackagePurchaseId { get; set; }
        public bool RequiresBooking { get; set; }
        public bool RequiresShipment { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn ngày bắt đầu."), DataType(DataType.Date)] public DateOnly? StartDate { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn ngày kết thúc."), DataType(DataType.Date)] public DateOnly? EndDate { get; set; }
        [MinLength(1),MaxLength(10)] public List<DeliverableInputModel> Deliverables { get; set; }=[new()];
        public TargetingInputModel Targeting { get; set; } = new();
        public ExecutionInputModel Execution { get; set; } = new();
        public CampaignUpsertRequest ToRequest() => new(Title, Objective, BudgetAmount, PackageCode, RequiresBooking, RequiresShipment, StartDate, EndDate, Deliverables[0].DeliverableType, Deliverables[0].Quantity, Deliverables[0].DueDateUtc){CheckoutAtCreation=CheckoutAtCreation,BrandSelectionRequested=BrandSelectionRequested,Deliverables=Deliverables.Select(x=>new CampaignDeliverableInput(x.DeliverableType,x.Quantity,x.DueDateUtc)).ToList(),Targeting=Targeting.ToRequest(),Execution=Execution.ToRequest(),PackagePurchaseId=PackagePurchaseId,AllocationMode=AllocationMode,DesiredKocCount=DesiredKocCount};
    }
    public sealed class DeliverableInputModel { [Required(ErrorMessage="Vui lòng nhập loại nội dung."),StringLength(100,ErrorMessage="Loại nội dung không được vượt quá 100 ký tự.")]public string DeliverableType{get;set;}="video";[Range(1,100,ErrorMessage="Số lượng phải từ 1 đến 100.")]public int Quantity{get;set;}=1;[DataType(DataType.DateTime)]public DateTime? DueDateUtc{get;set;} }
    public sealed class TargetingInputModel
    {
        [Required] public string Gender { get; set; } = "Any";
        [Range(0,int.MaxValue)] public int? MinimumFollowers { get; set; }
        [Range(0,int.MaxValue)] public int? MaximumFollowers { get; set; }
        [Range(0,100)] public decimal? MinimumEngagementRate { get; set; }
        [Range(0,100)] public decimal? MaximumEngagementRate { get; set; }
        public List<Guid> ProvinceIds { get; set; } = [];
        public List<Guid> CategoryIds { get; set; } = [];
        public CampaignTargetingInput ToRequest()=>new(){Gender=Gender,MinimumFollowers=MinimumFollowers,MaximumFollowers=MaximumFollowers,MinimumEngagementRate=MinimumEngagementRate,MaximumEngagementRate=MaximumEngagementRate,ProvinceIds=ProvinceIds,CategoryIds=CategoryIds};
        public static TargetingInputModel From(CampaignTargetingItem? item)=>item is null?new():new(){Gender=item.Gender,MinimumFollowers=item.MinimumFollowers,MaximumFollowers=item.MaximumFollowers,MinimumEngagementRate=item.MinimumEngagementRate,MaximumEngagementRate=item.MaximumEngagementRate,ProvinceIds=item.Provinces.Select(x=>x.Id).ToList(),CategoryIds=item.Categories.Select(x=>x.Id).ToList()};
    }
    public sealed class ExecutionInputModel
    {
        [Required(ErrorMessage="Vui lòng chọn hình thức thực hiện."), StringLength(40)] public string Mode { get; set; } = CampaignExecutionModes.RemoteProductReview;
        [StringLength(200,ErrorMessage="Tên địa điểm không được vượt quá 200 ký tự.")] public string? VenueName { get; set; }
        [StringLength(500,ErrorMessage="Địa chỉ không được vượt quá 500 ký tự.")] public string? VenueAddress { get; set; }
        [StringLength(100,ErrorMessage="Tên người liên hệ không được vượt quá 100 ký tự.")] public string? ContactName { get; set; }
        [StringLength(20,ErrorMessage="Số điện thoại không được vượt quá 20 ký tự.")] public string? ContactPhone { get; set; }
        [StringLength(500,ErrorMessage="Khung thời gian không được vượt quá 500 ký tự.")] public string? Schedule { get; set; }
        [StringLength(1000,ErrorMessage="Quy trình đặt lịch không được vượt quá 1.000 ký tự.")] public string? BookingInstructions { get; set; }
        [StringLength(1000,ErrorMessage="Thông tin sản phẩm dự kiến gửi không được vượt quá 1.000 ký tự.")] public string? ShippingInstructions { get; set; }
        [StringLength(1000,ErrorMessage="Thông tin hỗ trợ không được vượt quá 1.000 ký tự.")] public string? SupportDetails { get; set; }
        [StringLength(2000,ErrorMessage="Yêu cầu nội dung không được vượt quá 2.000 ký tự.")] public string? Requirements { get; set; }
        public CampaignExecutionInput ToRequest()=>new(){Mode=Mode,VenueName=VenueName,VenueAddress=VenueAddress,ContactName=ContactName,ContactPhone=ContactPhone,Schedule=Schedule,BookingInstructions=BookingInstructions,ShippingInstructions=ShippingInstructions,SupportDetails=SupportDetails,Requirements=Requirements};
        public static ExecutionInputModel From(CampaignExecutionItem? item)=>item is null?new():new(){Mode=item.Mode,VenueName=item.VenueName,VenueAddress=item.VenueAddress,ContactName=item.ContactName,ContactPhone=item.ContactPhone,Schedule=item.Schedule,BookingInstructions=item.BookingInstructions,ShippingInstructions=item.ShippingInstructions,SupportDetails=item.SupportDetails,Requirements=item.Requirements};
    }
}

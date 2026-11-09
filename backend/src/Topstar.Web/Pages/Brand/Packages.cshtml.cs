using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Text.RegularExpressions;
using Topstar.Application.Matching;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class PackagesModel(
    IProfileAccessService profileAccessService,
    IPackagePolicyAdminService packagePolicyService,
    IBrandPackagePurchaseService purchaseService, Topstar.Infrastructure.Finance.BrandBalanceService balanceService) : BrandGuardedPageModel(profileAccessService)
{
    public IReadOnlyList<PackagePolicyItem> Items { get; private set; } = [];
    public IReadOnlyList<BrandPackagePurchaseItem> Purchases { get; private set; } = [];
    [BindProperty(SupportsGet=true)] public int PackagePage { get; set; }=1;
    public int PackagePageSize { get; }=6;
    public int PackageTotal { get; private set; }
    public int PackagePages => Math.Max(1,(int)Math.Ceiling(PackageTotal/(double)PackagePageSize));
    [TempData] public string? Message { get; set; }
    [TempData] public bool Success { get; set; }
    public string? CancelError{get;set;}public string? CancelReason{get;set;}public string? CancelPackageName{get;set;}public string? CancelPurchaseId{get;set;}

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePages: false);
        if (redirect is not null) return redirect;
        await LoadAsync(cancellationToken);
        return Page();
    }

    public async Task<IActionResult> OnPostCancelAsync(Guid id, string reason, string packageName, CancellationToken cancellationToken)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
        var owned=(await purchaseService.GetForBrandAsync(UserId(),cancellationToken)).FirstOrDefault(x=>x.Id==id);
        var result=owned?.Status=="Paid" ? await balanceService.CancelPackageAsync(UserId(),id,reason,false,false,cancellationToken) : new Topstar.Infrastructure.Finance.BrandBalanceResult(false, "");
        if(owned?.Status!="Paid"){var pending=await purchaseService.CancelPendingAsync(UserId(),id,reason,cancellationToken);result=new(pending.Success,pending.Message);}
        if(!result.Success){CancelError=result.Message;CancelReason=reason;CancelPackageName=packageName;CancelPurchaseId=id.ToString();await LoadAsync(cancellationToken);return Page();}await Topstar.Web.Services.Email.PackageCancellationMail.SendAsync(HttpContext,id,result.Message,cancellationToken);Message=result.Message;Success=true;return RedirectToPage();
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task LoadAsync(CancellationToken ct){var all=await packagePolicyService.GetPublicAsync(ct);PackageTotal=all.Count;PackagePage=Math.Clamp(PackagePage,1,Math.Max(1,(int)Math.Ceiling(PackageTotal/(double)PackagePageSize)));Items=all.Skip((PackagePage-1)*PackagePageSize).Take(PackagePageSize).ToList();Purchases=await purchaseService.GetForBrandAsync(UserId(),ct);}

    public static string StatusLabel(string status) => status switch
    {
        "CancelRequested" => "Chờ duyệt hủy",
        "Refunded" => "Đã hoàn vào số dư",
        BrandPackagePurchaseStatuses.Pending => "Chờ thanh toán",
        BrandPackagePurchaseStatuses.Paid => "Đã thanh toán",
        BrandPackagePurchaseStatuses.Consumed => "Đã sử dụng",
        BrandPackagePurchaseStatuses.Expired => "Đã hết hạn",
        BrandPackagePurchaseStatuses.Cancelled => "Đã hủy",
        _ => "Không xác định"
    };

    public static string DisplayPackageName(PackagePolicyItem item)
        => item.BrandSelectionEnabled
            ? $"Brand chủ động tuyển chọn tối đa {item.SelectionQuota ?? 1} KOC"
            : $"Topstar tuyển chọn và phân công tối đa {item.SelectionQuota ?? 1} KOC";

    public static string DisplayBenefits(string benefits)
        => Regex.Replace(benefits ?? string.Empty, @"<li\b[^>]*>.*?ngân\s*sách.*?</li>", string.Empty, RegexOptions.IgnoreCase | RegexOptions.Singleline);

    public static string DisplayPurchasedPackageName(BrandPackagePurchaseItem item)
    {
        if (item.PackageName.StartsWith("Brand chọn tối đa ", StringComparison.OrdinalIgnoreCase))
            return item.PackageName.Replace("Brand chọn tối đa ", "Brand chủ động tuyển chọn tối đa ", StringComparison.OrdinalIgnoreCase);
        if (item.PackageName.StartsWith("Admin phân công KOC", StringComparison.OrdinalIgnoreCase))
            return item.PackageName.Replace("Admin phân công KOC", "Topstar tuyển chọn KOC", StringComparison.OrdinalIgnoreCase);
        return item.PackageName;
    }
}

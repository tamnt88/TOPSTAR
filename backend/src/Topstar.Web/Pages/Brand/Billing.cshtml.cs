using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Billing;
using Topstar.Application.Matching;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles="Brand")]
public sealed class BillingModel(IProfileAccessService access,IBillingService service,IBrandPackagePurchaseService purchases):BrandGuardedPageModel(access)
{
 public Topstar.Application.Common.ListWindow InvoiceWindow { get; private set; }=new();
 public Topstar.Application.Common.ListWindow PackageWindow { get; private set; }=new();

    public IReadOnlyList<BillingInvoiceItem> Items{get;private set;}=[];
    public IReadOnlyList<BrandPackagePurchaseItem> PackagePurchases{get;private set;}=[];
    [TempData]public string? Message{get;set;}
    [TempData]public bool Success{get;set;}
    public string? CancelError{get;set;}public string? CancelReason{get;set;}public string? CancelPackageName{get;set;}public string? CancelPurchaseId{get;set;}

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
        await LoadAsync(ct);return Page();
    }
    public async Task<IActionResult> OnPostCancelAsync(Guid id,string reason,string packageName,CancellationToken ct)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
        var result=await purchases.CancelPendingAsync(UserId(),id,reason,ct);if(!result.Success){CancelError=result.Message;CancelReason=reason;CancelPackageName=packageName;CancelPurchaseId=id.ToString();await LoadAsync(ct);return Page();}Message=result.Message;Success=true;return RedirectToPage();
    }
    private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task LoadAsync(CancellationToken ct){InvoiceWindow=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"brandServiceFeeTable");PackageWindow=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"brandPackagePurchaseTable");var userId=UserId();Items=await service.GetForBrandAsync(userId,ct,InvoiceWindow);PackagePurchases=await purchases.GetForBrandAsync(userId,ct,PackageWindow);}
    public static string PackageStatusLabel(string s)=>s switch{"Refunded"=>"Đã hoàn vào số dư","CancelRequested"=>"Chờ duyệt hủy",BrandPackagePurchaseStatuses.Pending=>"Chờ thanh toán",BrandPackagePurchaseStatuses.Paid=>"Đã thanh toán",BrandPackagePurchaseStatuses.Consumed=>"Đã sử dụng",BrandPackagePurchaseStatuses.Expired=>"Đã hết hạn",BrandPackagePurchaseStatuses.Cancelled=>"Đã hủy",_=>"Không xác định"};
}

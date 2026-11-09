using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Billing;
using Topstar.Application.Matching;
using Topstar.Web.Services.Payments;
using Topstar.Infrastructure.Entities;
namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class BillingModel(IBillingService service,IBrandPackagePurchaseService packagePurchases,VnPayCallbackJournal journal,Topstar.Infrastructure.Finance.BrandBalanceService balanceService):PageModel
{
 public Topstar.Application.Common.ListWindow InvoiceWindow { get; private set; }=new();
 public Topstar.Application.Common.ListWindow PackageWindow { get; private set; }=new();

 public IReadOnlyList<BillingInvoiceItem> Items{get;private set;}=[];[TempData]public string? Message{get;set;}[TempData]public bool Success{get;set;}
 public IReadOnlyList<BrandPackagePurchaseItem> PackagePurchases{get;private set;}=[];
 public IReadOnlyDictionary<string,IReadOnlyList<VnPayCallbackLogEntity>> VnPayLogs{get;private set;}=new Dictionary<string,IReadOnlyList<VnPayCallbackLogEntity>>();
 public async Task OnGetAsync(CancellationToken ct){InvoiceWindow=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"adminInvoiceList");PackageWindow=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"adminPackageList");Items=await service.GetForAdminAsync(ct,InvoiceWindow);PackagePurchases=await packagePurchases.GetForAdminAsync(ct,PackageWindow);VnPayLogs=await journal.ReadAsync(PackagePurchases.Where(x=>x.PaymentMethod=="VnPay").Select(x=>"P"+x.Id.ToString("N")),ct);}
 public Guid? CancelReviewId{get;set;} public string? CancelReviewReason{get;set;} public string? CancelReviewError{get;set;}
 public async Task<IActionResult> OnPostCancelPackageAsync(Guid id,string reason,bool approve,CancellationToken ct){var r=await balanceService.CancelPackageAsync(UserId(),id,reason,true,approve,ct);if(r.Success)await Topstar.Web.Services.Email.PackageCancellationMail.SendAsync(HttpContext,id,r.Message,ct);if(!r.Success){CancelReviewId=id;CancelReviewReason=reason;CancelReviewError=r.Message;await OnGetAsync(ct);return Page();}Message=r.Message;Success=r.Success;return RedirectToPage();}
 public async Task<IActionResult> OnPostConfirmPackagePaidAsync(Guid id,string paymentReference,CancellationToken ct){var r=await packagePurchases.ConfirmPaidAsync(UserId(),id,paymentReference,ct);Message=r.Message;Success=r.Success;return RedirectToPage();}
 public async Task<IActionResult> OnPostGenerateAsync(Guid id,CancellationToken ct)=>Done(await service.GenerateAsync(UserId(),id,ct));
 public async Task<IActionResult> OnPostStatusAsync(Guid id,string targetStatus,string? note,CancellationToken ct)=>Done(await service.ChangeStatusAsync(UserId(),id,targetStatus,note,ct));
 private IActionResult Done(BillingResult r){Message=r.Message;Success=r.Success;return RedirectToPage();}private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

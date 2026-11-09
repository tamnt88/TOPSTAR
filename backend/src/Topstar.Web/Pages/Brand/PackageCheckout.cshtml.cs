using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Matching;
using Topstar.Application.Profiles;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Services.Storage;
using Topstar.Web.Services.Payments;

namespace Topstar.Web.Pages.Brand;

[Authorize(Roles = "Brand")]
public sealed class PackageCheckoutModel(
    IProfileAccessService profileAccess,
    IPackagePolicyAdminService packages,
    IBrandPackagePurchaseService purchases,
    TopstarDbContext db,
    IFileStorageService storage) : BrandGuardedPageModel(profileAccess)
{
    [BindProperty(SupportsGet = true)] public Guid PackageId { get; set; }
    [BindProperty(SupportsGet = true)] public Guid? PurchaseId { get; set; }
    [BindProperty] public CheckoutInput Input { get; set; } = new();
    public PackagePolicyItem? Package { get; private set; }
    public BrandPackagePurchaseItem? Purchase { get; private set; }
    public PaymentMethodConfigurationEntity PaymentConfig { get; private set; } = new();
    public decimal AvailableBalance{get;private set;}
    public Topstar.Infrastructure.Finance.BrandTaxQuote? TaxQuote{get;private set;}
    public decimal CheckoutTotal=>Purchase?.Amount??TaxQuote?.Total??Package?.SalePrice??0;

    public async Task<IActionResult> OnGetAsync(CancellationToken ct)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        if(!PurchaseId.HasValue)return RedirectToPage("/Brand/CampaignNeeds",new{packageId=PackageId});
        await LoadPaymentConfigAsync(ct);
        if (PurchaseId.HasValue)
        {
            Purchase = (await purchases.GetForBrandAsync(UserId(), ct)).FirstOrDefault(x => x.Id == PurchaseId.Value);
            if (Purchase is null) return NotFound();
            PackageId = Purchase.PackagePolicyId;
        }
        Package = await packages.GetByIdAsync(PackageId, ct);
        if(Package is not null)TaxQuote=await Topstar.Infrastructure.Finance.BrandTaxPricing.QuoteAsync(db,"Package",Package.SalePrice,Package.SalePrice,ct);
        if (Purchase is null && string.IsNullOrWhiteSpace(Input.PaymentMethod))
            Input.PaymentMethod = PaymentConfig.BankTransferEnabled ? "BankTransfer" : PaymentConfig.VnPayEnabled ? "VnPay" : PaymentConfig.OnePayEnabled ? "OnePay" : string.Empty;
        return Package is null || !Package.IsActive ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        if(!PurchaseId.HasValue)return RedirectToPage("/Brand/CampaignNeeds",new{packageId=PackageId});
        await LoadPaymentConfigAsync(ct);
        Package = await packages.GetByIdAsync(PackageId, ct);
        if(Package is not null)TaxQuote=await Topstar.Infrastructure.Finance.BrandTaxPricing.QuoteAsync(db,"Package",Package.SalePrice,Package.SalePrice,ct);
        if(Input.PaymentMethod is not ("BankTransfer" or "OnePay" or "VnPay" or "BrandBalance"))
            ModelState.AddModelError("Input.PaymentMethod","Vui lòng chọn phương thức thanh toán hợp lệ.");
        if(Input.PaymentMethod=="VnPay"&&!PaymentConfig.VnPayEnabled)
            ModelState.AddModelError("Input.PaymentMethod","Thanh toán online hiện không khả dụng.");
        if (Package is null || !Package.IsActive) return NotFound();
        if (Input.PaymentMethod == "BankTransfer" && !PaymentConfig.BankTransferEnabled)
            ModelState.AddModelError("Input.PaymentMethod", "Phương thức chuyển khoản hiện không khả dụng.");
        if (Input.PaymentMethod == "BankTransfer" && (string.IsNullOrWhiteSpace(PaymentConfig.BankName) || string.IsNullOrWhiteSpace(PaymentConfig.BankAccountNumber) || string.IsNullOrWhiteSpace(PaymentConfig.BankAccountHolder)))
            ModelState.AddModelError("Input.PaymentMethod", "Thông tin tài khoản nhận tiền chưa hoàn chỉnh. Vui lòng chọn thanh toán online hoặc liên hệ Topstar.");
        if (Input.PaymentMethod == "OnePay" && !PaymentConfig.OnePayEnabled)
            ModelState.AddModelError("Input.PaymentMethod", "Thanh toán online hiện không khả dụng.");
        if (Input.PaymentMethod == "OnePay" && !IsOnePayChannelEnabled(Input.OnePayChannel))
            ModelState.AddModelError("Input.OnePayChannel", "Vui lòng chọn một kênh OnePAY đang hoạt động.");
        if (!Input.AcceptedTerms)
            ModelState.AddModelError("Input.AcceptedTerms", "Vui lòng xác nhận thông tin và điều khoản mua gói.");
        if(Input.ExpectedTotal!=CheckoutTotal)ModelState.AddModelError(string.Empty,"Giá hoặc thuế đã thay đổi. Vui lòng kiểm tra lại tổng tiền và xác nhận.");
        if (!ModelState.IsValid){ModelState.Remove("Input.ExpectedTotal");Input.ExpectedTotal=CheckoutTotal;return Page();}
        if(Input.PaymentMethod=="BrandBalance")
        {
            var balance=HttpContext.RequestServices.GetRequiredService<Topstar.Infrastructure.Finance.BrandBalanceService>();
            var payment=await balance.PurchasePackageAsync(UserId(),PackageId,Input.Operation,ct);
            if(!payment.Success){ModelState.AddModelError(string.Empty,payment.Message);return Page();}
            TempData["PackagePaymentMessage"]=payment.Message;
            return RedirectToPage("/Brand/Packages");
        }
        var result = await purchases.CreateOrderAsync(UserId(), PackageId, Input.PaymentMethod, ct);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            return Page();
        }
        if(Input.PaymentMethod=="VnPay"&&result.Id.HasValue)
        {
            var gateway=HttpContext.RequestServices.GetRequiredService<VnPayService>();
            var payment=await gateway.CreateAsync(UserId(),result.Id.Value,false,HttpContext.Connection.RemoteIpAddress?.ToString()??"127.0.0.1",ct);
            if(payment.Success&&payment.RedirectUrl is not null)return Redirect(payment.RedirectUrl);
            TempData["PackagePaymentMessage"]=payment.Message;
        }
        return RedirectToPage(new { packageId = PackageId, purchaseId = result.Id });
    }

    public async Task<IActionResult> OnPostPaymentProofAsync(IFormFile? proofFile, string? proofNote, CancellationToken ct)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
        if(!PurchaseId.HasValue)return BadRequest();
        proofNote=(proofNote??string.Empty).Trim();
        if(proofNote.Length is <10 or >500){TempData["PackagePaymentMessage"]="Vui lòng nhập ghi chú chuyển khoản từ 10 đến 500 ký tự.";return RedirectToPage(new{packageId=PackageId,purchaseId=PurchaseId});}
        var url=await storage.SavePaymentProofAsync(proofFile,ct);
        if(url is null){TempData["PackagePaymentMessage"]="Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB.";return RedirectToPage(new{packageId=PackageId,purchaseId=PurchaseId});}
        var result=await purchases.SubmitPaymentProofAsync(UserId(),PurchaseId.Value,url,proofNote,ct);
        if(!result.Success)await storage.DeletePaymentProofAsync(url,ct);
        TempData["PackagePaymentMessage"]=result.Message;
        return RedirectToPage(new{packageId=PackageId,purchaseId=PurchaseId});
    }

    public async Task<IActionResult> OnPostVnPayAsync(CancellationToken ct)
    {
        var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;
        if(!PurchaseId.HasValue)return BadRequest();
        var gateway=HttpContext.RequestServices.GetRequiredService<VnPayService>();
        var payment=await gateway.CreateAsync(UserId(),PurchaseId.Value,false,HttpContext.Connection.RemoteIpAddress?.ToString()??"127.0.0.1",ct);
        if(payment.Success&&payment.RedirectUrl is not null)return Redirect(payment.RedirectUrl);
        TempData["PackagePaymentMessage"]=payment.Message;
        return RedirectToPage(new{packageId=PackageId,purchaseId=PurchaseId});
    }
    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private async Task LoadPaymentConfigAsync(CancellationToken ct)
    {
        PaymentConfig = await db.PaymentMethodConfigurations.AsNoTracking().FirstOrDefaultAsync(ct) ?? new();
        AvailableBalance=(await HttpContext.RequestServices.GetRequiredService<Topstar.Infrastructure.Finance.BrandBalanceService>().GetAsync(UserId(),ct)).Available;
    }
    private bool IsOnePayChannelEnabled(string? channel) => channel switch
    {
        "DOMESTIC" => PaymentConfig.OnePayDomesticEnabled,
        "INTERNATIONAL" => PaymentConfig.OnePayInternationalEnabled,
        "EWALLET" => PaymentConfig.OnePayEWalletEnabled,
        _ => false
    };

    public sealed class CheckoutInput
    {
        public decimal ExpectedTotal{get;set;}
        public Guid Operation{get;set;}=Guid.NewGuid();
        public bool AcceptedTerms { get; set; }
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán.")]
        public string PaymentMethod { get; set; } = string.Empty;
        public string? OnePayChannel { get; set; }
    }
}

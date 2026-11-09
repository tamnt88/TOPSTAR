using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles="Admin")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
public sealed class PaymentMethodsModel(TopstarDbContext db,IDataProtectionProvider protection):PageModel
{
    [BindProperty] public InputModel Input{get;set;}=new();
    [TempData]public string? Message{get;set;}
    public async Task OnGetAsync(CancellationToken ct)
    {
        var x=await db.PaymentMethodConfigurations.AsNoTracking().FirstAsync(ct);
        Input=new(){BankTransferEnabled=x.BankTransferEnabled,BankName=x.BankName,BankAccountNumber=x.BankAccountNumber,BankAccountHolder=x.BankAccountHolder,OnePayEnabled=x.OnePayEnabled,Domestic=x.OnePayDomesticEnabled,International=x.OnePayInternationalEnabled,EWallet=x.OnePayEWalletEnabled,Environment=x.OnePayEnvironment,MerchantId=x.OnePayMerchantId,AccessCode=x.OnePayAccessCode,BaseUrl=x.OnePayBaseUrl,ReturnUrl=x.OnePayReturnUrl,CallbackUrl=x.OnePayCallbackUrl,HasSecret=!string.IsNullOrWhiteSpace(x.OnePaySecureHashCiphertext)};
        Input.SecureHashSecret=ReadSecret(x.OnePaySecureHashCiphertext,"Topstar.OnePay.SecureHash.v1");
        Input.VnPayEnabled=x.VnPayEnabled;Input.VnPayEnvironment=x.VnPayEnvironment;Input.VnPayTmnCode=x.VnPayTmnCode;
        Input.VnPayHashSecret=ReadSecret(x.VnPayHashSecretCiphertext,"Topstar.VnPay.HashSecret.v1");
        Input.VnPayBaseUrl=x.VnPayBaseUrl;Input.VnPayReturnUrl=x.VnPayReturnUrl;Input.VnPayIpnUrl=x.VnPayIpnUrl;
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        var saved=await db.PaymentMethodConfigurations.AsNoTracking().FirstAsync(ct);
        Input.HasSecret=!string.IsNullOrWhiteSpace(saved.OnePaySecureHashCiphertext);
        if(Input.VnPayEnabled && (string.IsNullOrWhiteSpace(Input.VnPayTmnCode)
            || string.IsNullOrWhiteSpace(Input.VnPayHashSecret) && string.IsNullOrWhiteSpace(saved.VnPayHashSecretCiphertext)))
            ModelState.AddModelError("","Vui lòng nhập TmnCode và Hash Secret của VNPay.");
        if(Input.VnPayEnvironment is not ("Test" or "Production")) ModelState.AddModelError("","Môi trường VNPay không hợp lệ.");
        if(Input.VnPayEnabled && (!HttpsUrl(Input.VnPayBaseUrl) || !ValidReturnUrl(Input.VnPayReturnUrl,Input.VnPayEnvironment) || !HttpsUrl(Input.VnPayIpnUrl)))
            ModelState.AddModelError("","URL thanh toán và IPN phải là HTTPS. Return URL chỉ được dùng HTTP localhost trong môi trường thử nghiệm.");
        if(Input.VnPayEnabled && Uri.TryCreate(Input.VnPayBaseUrl,UriKind.Absolute,out var vnPayUri)
            && (Input.VnPayEnvironment=="Production"&&vnPayUri.Host=="sandbox.vnpayment.vn"
                || Input.VnPayEnvironment=="Test"&&vnPayUri.Host=="pay.vnpay.vn"))
            ModelState.AddModelError("","URL thanh toán VNPay không khớp môi trường đã chọn.");
        if(Input.BankTransferEnabled&&(string.IsNullOrWhiteSpace(Input.BankName)||string.IsNullOrWhiteSpace(Input.BankAccountNumber)||string.IsNullOrWhiteSpace(Input.BankAccountHolder)))ModelState.AddModelError("", "Vui lòng nhập đủ ngân hàng, số tài khoản và chủ tài khoản.");
        if(Input.OnePayEnabled&&!(Input.Domestic||Input.International||Input.EWallet))ModelState.AddModelError("", "Hãy bật ít nhất một kênh OnePAY.");
        if(Input.OnePayEnabled&&(string.IsNullOrWhiteSpace(Input.MerchantId)||string.IsNullOrWhiteSpace(Input.AccessCode)||!Uri.TryCreate(Input.BaseUrl,UriKind.Absolute,out var onePayBase)||onePayBase.Scheme!=Uri.UriSchemeHttps))ModelState.AddModelError("", "Vui lòng nhập đầy đủ Merchant ID, Access Code và Base URL HTTPS của OnePAY.");
        if(Input.OnePayEnabled&&!Input.HasSecret&&string.IsNullOrWhiteSpace(Input.SecureHashSecret))ModelState.AddModelError("", "Vui lòng nhập Hash Code do OnePAY cung cấp.");
        if(!string.IsNullOrWhiteSpace(Input.SecureHashSecret)&&!IsValidHexSecret(Input.SecureHashSecret))ModelState.AddModelError("", "Hash Code OnePAY phải là chuỗi HEX hợp lệ gồm 64 ký tự.");
        if(!ModelState.IsValid)return Page();
        var x=await db.PaymentMethodConfigurations.FirstAsync(ct);var now=DateTime.UtcNow;
        x.VnPayEnabled=Input.VnPayEnabled;x.VnPayEnvironment=Input.VnPayEnvironment;
        x.VnPayTmnCode=Input.VnPayTmnCode?.Trim();x.VnPayBaseUrl=Input.VnPayBaseUrl.Trim();
        x.VnPayReturnUrl=Input.VnPayReturnUrl.Trim();x.VnPayIpnUrl=Input.VnPayIpnUrl.Trim();
        if(!string.IsNullOrWhiteSpace(Input.VnPayHashSecret))
            x.VnPayHashSecretCiphertext=protection.CreateProtector("Topstar.VnPay.HashSecret.v1").Protect(Input.VnPayHashSecret.Trim());
        x.BankTransferEnabled=Input.BankTransferEnabled;x.BankName=Input.BankName?.Trim();x.BankAccountNumber=Input.BankAccountNumber?.Trim();x.BankAccountHolder=Input.BankAccountHolder?.Trim();
        x.OnePayEnabled=Input.OnePayEnabled;x.OnePayDomesticEnabled=Input.Domestic;x.OnePayInternationalEnabled=Input.International;x.OnePayEWalletEnabled=Input.EWallet;x.OnePayEnvironment=Input.Environment;
        x.OnePayMerchantId=Input.MerchantId?.Trim();x.OnePayAccessCode=Input.AccessCode?.Trim();x.OnePayBaseUrl=Input.BaseUrl?.Trim();x.OnePayReturnUrl=Input.ReturnUrl.Trim();x.OnePayCallbackUrl=Input.CallbackUrl.Trim();
        if(!string.IsNullOrWhiteSpace(Input.SecureHashSecret))x.OnePaySecureHashCiphertext=protection.CreateProtector("Topstar.OnePay.SecureHash.v1").Protect(Input.SecureHashSecret.Trim());
        x.UpdatedByUserId=Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);x.UpdatedAtUtc=now;
        db.AuditLogs.Add(new(){ActorUserId=x.UpdatedByUserId,EntityName="PaymentMethodConfiguration",EntityId=x.Id.ToString(),ActionName="UpdateVnPay",PayloadJson=JsonSerializer.Serialize(new{x.VnPayEnabled,x.VnPayEnvironment,x.VnPayTmnCode,x.VnPayBaseUrl,x.VnPayReturnUrl,x.VnPayIpnUrl}),CreatedAtUtc=now});
        db.AuditLogs.Add(new(){ActorUserId=x.UpdatedByUserId,EntityName="PaymentMethodConfiguration",EntityId=x.Id.ToString(),ActionName="Update",PayloadJson=JsonSerializer.Serialize(new{x.BankTransferEnabled,x.OnePayEnabled,x.OnePayEnvironment,x.OnePayDomesticEnabled,x.OnePayInternationalEnabled,x.OnePayEWalletEnabled}),CreatedAtUtc=now});
        await db.SaveChangesAsync(ct);Message="Đã lưu cấu hình phương thức thanh toán.";return RedirectToPage();
    }
    private static bool IsValidHexSecret(string value){value=value.Trim();return value.Length==64&&value.All(Uri.IsHexDigit);}
    private static bool HttpsUrl(string? value)=>Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&string.IsNullOrEmpty(uri.UserInfo);
    private static bool ValidReturnUrl(string? value,string environment)=>HttpsUrl(value)
        || environment=="Test"&&Uri.TryCreate(value,UriKind.Absolute,out var uri)&&uri.Scheme=="http"&&uri.IsLoopback&&string.IsNullOrEmpty(uri.UserInfo);
    private string? ReadSecret(string? value,string purpose)
    {
        if(string.IsNullOrWhiteSpace(value))return null;
        try{return protection.CreateProtector(purpose).Unprotect(value);}
        catch(System.Security.Cryptography.CryptographicException){ModelState.AddModelError("","Không đọc được secret đã lưu. Vui lòng nhập lại secret của cổng thanh toán.");return null;}
    }
    public sealed class InputModel
    {
        public bool VnPayEnabled{get;set;}
        public string VnPayEnvironment{get;set;}="Test";
        [StringLength(100)]public string? VnPayTmnCode{get;set;}
        [StringLength(500)]public string? VnPayHashSecret{get;set;}
        [Required,StringLength(500)]public string VnPayBaseUrl{get;set;}="https://sandbox.vnpayment.vn/paymentv2/vpcpay.html";
        [Required,StringLength(500)]public string VnPayReturnUrl{get;set;}="https://topstar.vn/Brand/VnPayReturn";
        [Required,StringLength(500)]public string VnPayIpnUrl{get;set;}="https://topstar.vn/api/payments/vnpay/ipn";
        public bool BankTransferEnabled{get;set;} [StringLength(150)]public string? BankName{get;set;} [StringLength(50)]public string? BankAccountNumber{get;set;} [StringLength(150)]public string? BankAccountHolder{get;set;}
        public bool OnePayEnabled{get;set;} public bool Domestic{get;set;}=true;public bool International{get;set;}=true;public bool EWallet{get;set;}=true;
        [Required]public string Environment{get;set;}="Test";[StringLength(100)]public string? MerchantId{get;set;}[StringLength(200)]public string? AccessCode{get;set;}[StringLength(500)]public string? BaseUrl{get;set;}[StringLength(500)]public string? SecureHashSecret{get;set;}
        [Required,Url]public string ReturnUrl{get;set;}="https://topstar.vn/Brand/OnePayReturn";[Required,Url]public string CallbackUrl{get;set;}="https://topstar.vn/api/payments/onepay/ipn";public bool HasSecret{get;set;}
    }
}

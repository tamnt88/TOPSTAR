using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application.Dashboard;
using Topstar.Application.Profiles;
using Topstar.Application.Finance;
using Topstar.Web.Services.Email;
using System.Globalization;

namespace Topstar.Web.Pages.Koc;

[Authorize(Roles = "Koc")]
public sealed class WalletModel(
    IProfileAccessService profileAccessService,
    IWalletService walletService,
    IEmailSender emailSender,
    IEmailTemplateRenderer emailTemplateRenderer) : KocGuardedPageModel(profileAccessService)
{
    public Topstar.Application.Common.ListWindow TransactionsWindow { get; private set; } = new();
    public Topstar.Application.Common.ListWindow PayoutsWindow { get; private set; } = new();
    public KocWalletOverview? Wallet { get; private set; }
    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(allowProfilePage: false);
        if (redirect is not null) return redirect;
        TransactionsWindow=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"walletTransactionTable");
        PayoutsWindow=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"walletPayoutTable");
        Wallet = await walletService.GetKocWalletAsync(Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value), cancellationToken, TransactionsWindow, PayoutsWindow);
        return Page();
    }

    public async Task<IActionResult> OnPostAccountAsync(Guid? accountId, string? bankCode, string? accountHolderName, string? accountNumber, bool makeDefault, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        AccountBankCode = bankCode?.Trim();
        AccountHolderName = accountHolderName?.Trim();
        var result = await walletService.SavePayoutAccountAsync(UserId(), accountId, bankCode ?? string.Empty, accountHolderName ?? string.Empty, accountNumber ?? string.Empty, makeDefault, cancellationToken);
        if (!result.Success)
        {
            AccountErrorMessage = result.Message;
            AccountErrorField = result.Message.StartsWith("Mã ngân hàng", StringComparison.OrdinalIgnoreCase)
                ? "bankCode"
                : result.Message.StartsWith("Tên chủ", StringComparison.OrdinalIgnoreCase)
                    ? "accountHolderName"
                    : result.Message.StartsWith("Số tài khoản", StringComparison.OrdinalIgnoreCase)
                        ? "accountNumber"
                        : "form";
            return RedirectToListState();
        }
        Message = result.Message;
        MessageSuccess = true;
        AccountBankCode = null;
        AccountHolderName = null;
        return RedirectToListState();
    }

    public async Task<IActionResult> OnPostDeleteAccountAsync(Guid accountId, string reason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await walletService.DeletePayoutAccountAsync(UserId(), accountId, reason, cancellationToken);
        if (!result.Success)
        {
            DeleteAccountError = result.Message;
            DeleteAccountReason = reason;
            OpenDeleteAccountModal = true;
            return RedirectToListState();
        }
        Message = result.Message;
        MessageSuccess = true;
        return RedirectToListState();
    }

    public async Task<IActionResult> OnPostDefaultAccountAsync(Guid accountId, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await walletService.SetDefaultPayoutAccountAsync(UserId(), accountId, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToListState();
    }

    public async Task<IActionResult> OnPostPayoutAsync(Guid payoutAccountId, decimal amount, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await walletService.CreatePayoutOtpChallengeAsync(UserId(), payoutAccountId, amount, cancellationToken);
        if (!result.Success || result.ChallengeId is null || result.RawCode is null || result.Email is null)
        {
            PayoutErrorMessage = result.Message;
            PayoutAmount = amount.ToString(CultureInfo.InvariantCulture);
            PayoutAccountId = payoutAccountId;
            OpenPayoutModal = true;
            return RedirectToListState();
        }
        var otpExpiryMinutes = Math.Max(1, (int)Math.Ceiling(((result.ExpiresAtUtc ?? DateTime.UtcNow.AddMinutes(1)) - DateTime.UtcNow).TotalMinutes));
        var emailBody = emailTemplateRenderer.Render(new EmailTemplateModel
        {
            ProjectName = "TopStar KOC",
            MainTitle = "Xác nhận yêu cầu rút tiền",
            Preheader = $"Mã OTP xác nhận yêu cầu rút {amount:N0} ₫.",
            BodyHtml = $"""
                <p style="margin:0 0 14px;">Xin chào,</p>
                <p style="margin:0 0 18px;">Bạn vừa tạo yêu cầu rút <strong>{amount:N0} ₫</strong> từ Ví thu nhập TopStar.</p>
                <div style="padding:18px;text-align:center;border:1px solid #ead5df;border-radius:12px;background:#fff7fb;">
                    <div style="margin-bottom:8px;color:#75626c;font-size:13px;">Mã xác nhận của bạn</div>
                    <div style="color:#bd2f79;font-size:30px;font-weight:800;letter-spacing:8px;">{result.RawCode}</div>
                </div>
                <p style="margin:18px 0 0;">Mã có hiệu lực trong <strong>{otpExpiryMinutes} phút</strong> và chỉ dùng một lần.</p>
                <p style="margin:10px 0 0;padding:12px;border-radius:10px;background:#fff3cd;color:#6b5100;">
                    TopStar không bao giờ yêu cầu bạn cung cấp OTP qua điện thoại, tin nhắn hoặc mạng xã hội.
                </p>
                """
        });
        var sent = await emailSender.SendAsync(
            result.Email,
            "Mã OTP xác nhận rút tiền TopStar",
            emailBody,
            cancellationToken);
        if (!sent)
        {
            PayoutErrorMessage = "Không gửi được OTP. Vui lòng kiểm tra cấu hình email hoặc thử lại sau.";
            PayoutAmount = amount.ToString(CultureInfo.InvariantCulture);
            PayoutAccountId = payoutAccountId;
            OpenPayoutModal = true;
            return RedirectToListState();
        }
        PayoutChallengeId = result.ChallengeId.Value;
        PayoutOtpExpiresAtUtc = result.ExpiresAtUtc?.ToString("O", CultureInfo.InvariantCulture);
        Message = $"Đã gửi OTP tới email {MaskEmail(result.Email)}. Mã có hiệu lực {otpExpiryMinutes} phút.";
        MessageSuccess = true;
        return RedirectToListState();
    }

    public async Task<IActionResult> OnPostConfirmPayoutAsync(Guid challengeId, string code, string? expiresAtUtc, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false); if (redirect is not null) return redirect;
        var result = await walletService.ConfirmPayoutOtpAsync(UserId(), challengeId, code, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        if (!result.Success)
        {
            PayoutChallengeId = challengeId;
            PayoutOtpExpiresAtUtc = expiresAtUtc;
            PayoutOtpErrorMessage = result.Message;
        }
        return RedirectToListState();
    }

    public async Task<IActionResult> OnPostCancelPayoutAsync(Guid payoutRequestId, string reason, CancellationToken cancellationToken)
    {
        var redirect = await RedirectIfProfileIncompleteAsync(false);
        if (redirect is not null) return redirect;
        var result = await walletService.CancelPayoutAsync(UserId(), payoutRequestId, reason, cancellationToken);
        Message = result.Message;
        MessageSuccess = result.Success;
        if (!result.Success)
        {
            CancelPayoutId = payoutRequestId;
            CancelPayoutReason = reason;
            CancelPayoutError = result.Message;
            OpenCancelPayoutModal = true;
        }
        return RedirectToListState();
    }

    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }
    [TempData(Key = "WalletPayoutChallengeIdV2")] public Guid? PayoutChallengeId { get; set; }
    [TempData] public string? AccountErrorField { get; set; }
    [TempData] public string? AccountErrorMessage { get; set; }
    [TempData] public string? AccountBankCode { get; set; }
    [TempData] public string? AccountHolderName { get; set; }
    [TempData] public string? DeleteAccountError { get; set; }
    [TempData] public string? DeleteAccountReason { get; set; }
    [TempData] public bool OpenDeleteAccountModal { get; set; }
    [TempData] public string? PayoutErrorMessage { get; set; }
    [TempData] public string? PayoutAmount { get; set; }
    [TempData] public bool OpenPayoutModal { get; set; }
    [TempData] public string? PayoutOtpErrorMessage { get; set; }
    [TempData] public string? PayoutOtpExpiresAtUtc { get; set; }
    [TempData(Key = "WalletPayoutAccountIdV2")] public Guid? PayoutAccountId { get; set; }
    [TempData(Key = "WalletCancelPayoutIdV1")] public Guid? CancelPayoutId { get; set; }
    [TempData] public string? CancelPayoutReason { get; set; }
    [TempData] public string? CancelPayoutError { get; set; }
    [TempData] public bool OpenCancelPayoutModal { get; set; }
    private Guid UserId() => Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
    public int PayoutOtpExpiryMinutes
    {
        get
        {
            if (!DateTime.TryParse(PayoutOtpExpiresAtUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var expiresAtUtc)) return 1;
            return Math.Max(1, (int)Math.Ceiling((expiresAtUtc - DateTime.UtcNow).TotalMinutes));
        }
    }
    private static string MaskEmail(string email)
    {
        var at = email.IndexOf('@');
        if (at <= 1) return "***" + (at >= 0 ? email[at..] : string.Empty);
        return email[0] + new string('*', Math.Min(5, at - 1)) + email[at..];
    }
}

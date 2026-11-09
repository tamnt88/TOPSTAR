using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Finance;

namespace Topstar.Web.Pages.Admin.Finance;

[Authorize(Roles = "Admin")]
public sealed class PayoutPolicyModel(IWalletService walletService) : PageModel
{
    public PayoutPolicyItem Policy { get; private set; } = null!;
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
        {if(Request.Path.Value?.Equals("/Admin/Finance/PayoutPolicy",StringComparison.OrdinalIgnoreCase)==true){Response.Redirect("/Admin/Finance/TaxFees?tab=Koc");return;} Policy = await walletService.GetPayoutPolicyAsync(cancellationToken);}

    public async Task<IActionResult> OnPostAsync(decimal minimumAmount, int dailyRequestLimit, decimal fixedFee, decimal feePercent, string reason, CancellationToken cancellationToken)
    {
        if(!ModelState.IsValid){Message="Vui lòng kiểm tra số tiền và tỷ lệ phí.";TempData["Success"]=false;return RedirectToPage("/Admin/Finance/TaxFees",new{tab="Koc"});}
        var result = await walletService.UpdatePayoutPolicyAsync(
            Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            minimumAmount, dailyRequestLimit, fixedFee, feePercent / 100m, reason, cancellationToken);
        Message = result.Message;
        MessageSuccess = result.Success;
        TempData["Success"] = result.Success;
        return RedirectToPage("/Admin/Finance/TaxFees",new{tab="Koc"});
    }
}

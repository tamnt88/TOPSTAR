using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Finance;

namespace Topstar.Web.Pages.Admin.Finance;

[Authorize(Roles = "Admin")]
public sealed class SettlementsModel(IWalletService walletService) : PageModel
{
    public Topstar.Application.Common.ListWindow Window { get; private set; } = new();
    public Topstar.Application.Common.ListWindow PayoutWindow { get; private set; } = new();
    public IReadOnlyList<AdminSettlementItem> Items { get; private set; } = [];
    public IReadOnlyList<AdminPayoutItem> Payouts { get; private set; } = [];
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Window = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request, "adminSettlementList");
        PayoutWindow = Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request, "adminPayoutList");
        Items = await walletService.GetAdminSettlementsAsync(cancellationToken, Window);
        Payouts = await walletService.GetAdminPayoutsAsync(cancellationToken, PayoutWindow);
    }

    public async Task<IActionResult> OnPostCreateAsync(Guid id, decimal amount, string? description, CancellationToken cancellationToken)
    {
        var result = await walletService.CreateEarningAsync(UserId(), id, amount, description, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostStatusAsync(Guid id, string targetStatus, string? reason, CancellationToken cancellationToken)
    {
        var result = await walletService.ChangeStatusAsync(UserId(), id, targetStatus, reason, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success;
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostPayoutAsync(Guid id, string targetStatus, string? referenceOrReason, CancellationToken cancellationToken)
    {
        var result = await walletService.ProcessPayoutAsync(UserId(), id, targetStatus, referenceOrReason, cancellationToken);
        Message = result.Message; MessageSuccess = result.Success; return RedirectToPage();
    }

    private Guid UserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

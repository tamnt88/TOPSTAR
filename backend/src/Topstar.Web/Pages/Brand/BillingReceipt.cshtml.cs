using System.Security.Claims;using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Topstar.Application.Billing;using Topstar.Application.Profiles;
namespace Topstar.Web.Pages.Brand;
[Authorize(Roles="Brand")]
public sealed class BillingReceiptModel(IProfileAccessService access,IBillingService service):BrandGuardedPageModel(access){[BindProperty(SupportsGet=true)]public Guid Id{get;set;}public BillingInvoiceItem? Invoice{get;private set;}public async Task<IActionResult> OnGetAsync(CancellationToken ct){var redirect=await RedirectIfProfileIncompleteAsync(false);if(redirect is not null)return redirect;Invoice=await service.GetForBrandAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),Id,ct);return Invoice is null?NotFound():Page();}}

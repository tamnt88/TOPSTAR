using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Profiles;
using Topstar.Application.Settings;

namespace Topstar.Web.Pages.Admin;
[Authorize(Roles="Admin")]
public sealed class ViolationsModel(IUserViolationService service,IOperationalSetupService setup):PageModel
{
    public IReadOnlyList<UserViolationItem> Items{get;private set;}=[];
    public IReadOnlyList<OperationalCatalogItem> Categories{get;private set;}=[];
    [BindProperty(SupportsGet=true)]public string? Status{get;set;}
    [BindProperty(SupportsGet=true)]public Guid? AccountUserId{get;set;}
    [BindProperty(SupportsGet=true)]public string? ReturnType{get;set;}
    [BindProperty(SupportsGet=true)]public string? AccountName{get;set;}
    [BindProperty(SupportsGet=true)]public string? AccountEmail{get;set;}
    [TempData]public string? Message{get;set;}[TempData]public bool MessageSuccess{get;set;}
    public string AccountListType=>string.Equals(ReturnType,"Brand",StringComparison.OrdinalIgnoreCase)?"Brand":"KOC";
    public bool IsAccountScoped=>AccountUserId.HasValue;
    public async Task OnGetAsync(CancellationToken ct)
    {
        Items=await service.GetAsync(null,AccountUserId,ct);
        Categories=await setup.GetCatalogAsync(OperationalCatalogTypes.Violation,true,ct);
        var account=Items.FirstOrDefault();
        if(account is not null){AccountName=account.DisplayName;AccountEmail=account.Email;ReturnType=account.Role;}
    }
    public async Task<IActionResult> OnPostCreateAsync(string email,string category,string severity,int penaltyPoints,string reason,CancellationToken ct){var r=await service.CreateAsync(UserId(),email,category,severity,penaltyPoints,reason,ct);return Done(r);}
    public async Task<IActionResult> OnPostRevokeAsync(Guid id,string reason,CancellationToken ct){var r=await service.RevokeAsync(UserId(),id,reason,ct);return Done(r);}
    private IActionResult Done(UserViolationResult r){Message=r.Message;MessageSuccess=r.Success;return RedirectToPage(new{status=Status,accountUserId=AccountUserId,returnType=ReturnType,accountName=AccountName,accountEmail=AccountEmail});}
    private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}

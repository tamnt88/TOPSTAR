using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Academy;

namespace Topstar.Web.Pages.Public;

public sealed class VerifyCertificateModel(IAcademyService academyService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Code { get; set; }
    public AcademyCertificateItem? Certificate { get; private set; }
    public bool WasSearched => !string.IsNullOrWhiteSpace(Code);

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        Code = Code?.Trim().ToUpperInvariant();
        if (WasSearched)
            Certificate = await academyService.VerifyCertificateAsync(Code!, cancellationToken);
    }
}

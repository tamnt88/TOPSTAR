using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Profiles;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class ProfileReviewModel(IProfileReviewService profileReviewService) : PageModel
{
    [BindProperty(SupportsGet = true)] public string Type { get; set; } = string.Empty;
    [BindProperty(SupportsGet = true)] public Guid Id { get; set; }
    [BindProperty] public ReviewInput Input { get; set; } = new();
    public ProfileReviewDetail? Profile { get; private set; }
    public IReadOnlyList<ProfileStatusEvent> History { get; private set; } = [];
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        Profile = await profileReviewService.GetDetailAsync(Type, Id, cancellationToken);
        History = await profileReviewService.GetHistoryAsync(Type, Id, cancellationToken);
        return Profile is null ? NotFound() : Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid) { await LoadAsync(cancellationToken); return Page(); }
        var adminUserId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var result = await profileReviewService.ReviewAsync(adminUserId, Type, Id, Input.Decision, Input.ReviewNote, cancellationToken);
        if (!result.Success) { ModelState.AddModelError(string.Empty, result.Message); await LoadAsync(cancellationToken); return Page(); }
        Message = result.Message;
        MessageSuccess = true;
        return RedirectToPage("/Admin/Brands", new { Type = AccountListType() });
    }

    private async Task LoadAsync(CancellationToken cancellationToken) { Profile = await profileReviewService.GetDetailAsync(Type, Id, cancellationToken); History = await profileReviewService.GetHistoryAsync(Type, Id, cancellationToken); }
    public string AccountListType() => string.Equals(Type, "BRAND", StringComparison.OrdinalIgnoreCase) ? "Brand" : "KOC";

    public sealed class ReviewInput
    {
        [Required]
        public string Decision { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập thông tin và căn cứ xử lý hồ sơ.")]
        [StringLength(1000, MinimumLength = 10, ErrorMessage = "Thông tin xử lý phải có từ 10 đến 1.000 ký tự.")]
        public string ReviewNote { get; set; } = string.Empty;
    }
}

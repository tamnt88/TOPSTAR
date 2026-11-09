using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Data.SqlClient;
using Topstar.Web.Services;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles = "Admin")]
public sealed class TrackingModel(TrackingConfigurationService service) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    [TempData] public string? Message { get; set; }
    public string? Error { get; set; }
    public async Task OnGetAsync(CancellationToken ct)
    {
        var item = await service.ReadAsync(ct);
        if (item is not null) Input = new() { Enabled = item.Enabled, HeadCode = item.HeadCode, BodyCode = item.BodyCode };
    }
    public async Task<IActionResult> OnPostAsync(CancellationToken ct)
    {
        if (!ModelState.IsValid) return Page();
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var actor)) return Forbid();
        try { await service.SaveAsync(new(Input.Enabled, Input.HeadCode, Input.BodyCode), actor, ct); }
        catch (SqlException ex) when (ex.Number == 208)
        {
            Error = "Vui lòng chạy SQL 154 để tạo cấu hình tracking trước khi lưu.";
            return Page();
        }
        Message = "Đã lưu cấu hình mã tracking cho website công khai.";
        return RedirectToPage();
    }
    public sealed class InputModel
    {
        public bool Enabled { get; set; }
        [StringLength(50000)] public string? HeadCode { get; set; }
        [StringLength(50000)] public string? BodyCode { get; set; }
    }
}

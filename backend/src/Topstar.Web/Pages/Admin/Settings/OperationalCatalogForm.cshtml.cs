using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Settings;

namespace Topstar.Web.Pages.Admin.Settings;

[Authorize(Roles="Admin")]
public sealed class OperationalCatalogFormModel(IOperationalSetupService service) : PageModel
{
    [BindProperty] public InputModel Input { get; set; } = new();
    public bool Editing => Input.Id.HasValue;

    public async Task<IActionResult> OnGetAsync(Guid? id, string? type, CancellationToken cancellationToken)
    {
        if (!id.HasValue)
        {
            if (OperationalCatalogTypes.All.Contains(type)) Input.CatalogType = type!;
            return Page();
        }
        var item = await service.FindCatalogAsync(id.Value, cancellationToken);
        if (item is null) return NotFound();
        Input = new() { Id=item.Id, CatalogType=item.CatalogType, Code=item.Code, Name=item.Name,
            Description=item.Description, DefaultSeverity=item.DefaultSeverity,
            DefaultPenaltyPoints=item.DefaultPenaltyPoints, SortOrder=item.SortOrder, IsActive=item.IsActive };
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        if (Input.CatalogType == OperationalCatalogTypes.Violation)
        {
            if (string.IsNullOrWhiteSpace(Input.DefaultSeverity)) ModelState.AddModelError("Input.DefaultSeverity","Vui lòng chọn mức độ mặc định.");
            if (!Input.DefaultPenaltyPoints.HasValue) ModelState.AddModelError("Input.DefaultPenaltyPoints","Vui lòng nhập điểm phạt mặc định.");
        }
        if (!ModelState.IsValid) return Page();
        var result = await service.SaveCatalogAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),
            Input.Id,Input.CatalogType,Input.Code,Input.Name,Input.Description,Input.DefaultSeverity,
            Input.DefaultPenaltyPoints,Input.SortOrder,Input.IsActive,Input.Reason,cancellationToken);
        if (!result.Success) { ModelState.AddModelError(string.Empty,result.Message); return Page(); }
        TempData["Message"]=result.Message;TempData["Success"]=true;
        return Redirect($"/Admin/Settings/OperationalSetup#{TabKey(Input.CatalogType)}");
    }

    public static string TabKey(string type) => type switch
    {
        OperationalCatalogTypes.Violation => "violation",
        OperationalCatalogTypes.Deliverable => "deliverable",
        _ => "checkpoint"
    };

    public sealed class InputModel
    {
        public Guid? Id { get; set; }
        [Required(ErrorMessage="Vui lòng chọn nhóm danh mục.")]
        public string CatalogType { get; set; } = OperationalCatalogTypes.Violation;
        [Required(ErrorMessage="Vui lòng nhập mã danh mục.")]
        [StringLength(80,MinimumLength=2,ErrorMessage="Mã danh mục phải từ 2 đến 80 ký tự.")]
        [RegularExpression("^[a-zA-Z0-9_-]+$",ErrorMessage="Mã chỉ gồm chữ không dấu, số, gạch ngang hoặc gạch dưới.")]
        public string Code { get; set; } = "";
        [Required(ErrorMessage="Vui lòng nhập tên hiển thị.")]
        [StringLength(150,MinimumLength=2,ErrorMessage="Tên hiển thị phải từ 2 đến 150 ký tự.")]
        public string Name { get; set; } = "";
        [StringLength(500,ErrorMessage="Mô tả không được vượt quá 500 ký tự.")]
        public string? Description { get; set; }
        public string? DefaultSeverity { get; set; }
        [Range(0,100,ErrorMessage="Điểm phạt phải từ 0 đến 100.")]
        public int? DefaultPenaltyPoints { get; set; }
        [Range(0,99999,ErrorMessage="Thứ tự phải từ 0 đến 99.999.")]
        public int SortOrder { get; set; } = 10;
        public bool IsActive { get; set; } = true;
        [Required(ErrorMessage="Vui lòng nhập lý do thay đổi.")]
        [StringLength(500,MinimumLength=10,ErrorMessage="Lý do thay đổi phải từ 10 đến 500 ký tự.")]
        public string Reason { get; set; } = "";
    }
}

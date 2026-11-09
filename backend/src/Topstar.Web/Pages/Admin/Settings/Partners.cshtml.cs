using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Website;
using Topstar.Web.Services.Storage;

namespace Topstar.Web.Pages.Admin.Settings;

public sealed class PartnersModel(IHomepageContentService homepage, IFileStorageService fileStorage) : PageModel
{
    public HomepageContent HomepageContent { get; private set; } = HomepageContent.Default;
    public IReadOnlyList<HomepagePartnerItem> Partners { get; private set; } = [];

    [BindProperty]
    public PartnerInput Input { get; set; } = new();

    [BindProperty(SupportsGet = true)]
    public int? EditIndex { get; set; }

    public bool IsEditing => EditIndex.HasValue;

    public async Task<IActionResult> OnGetAsync()
    {
        await LoadAsync();
        if (!EditIndex.HasValue) return Page();
        if (EditIndex < 0 || EditIndex >= Partners.Count) return RedirectToPage();

        var item = Partners[EditIndex.Value];
        Input = new PartnerInput
        {
            Name = item.Name,
            LogoUrl = item.LogoUrl,
            Url = item.Url,
            DisplayOrder = item.DisplayOrder,
            IsVisible = item.IsVisible
        };
        return Page();
    }

    public async Task<IActionResult> OnPostSaveAsync()
    {
        await LoadAsync();
        Input.Name = Input.Name.Trim();
        Input.LogoUrl = Input.LogoUrl.Trim();
        Input.Url = string.IsNullOrWhiteSpace(Input.Url) ? "#" : Input.Url.Trim();

        if (Input.Url != "#" && (!Uri.TryCreate(Input.Url, UriKind.Absolute, out var partnerUri) ||
            (partnerUri.Scheme != Uri.UriSchemeHttp && partnerUri.Scheme != Uri.UriSchemeHttps)))
        {
            ModelState.AddModelError("Input.Url", "Liên kết phải bắt đầu bằng http://, https:// hoặc dùng # nếu không gắn liên kết.");
        }

        if (!ModelState.IsValid) return Page();
        if (!EditIndex.HasValue && Partners.Count >= 24)
        {
            ModelState.AddModelError(string.Empty, "Chỉ được quản lý tối đa 24 logo đối tác.");
            return Page();
        }

        var items = Partners.ToList();
        var partner = new HomepagePartnerItem(Input.Name, Input.LogoUrl, Input.Url, Input.DisplayOrder, Input.IsVisible);
        string reason;
        if (EditIndex.HasValue)
        {
            if (EditIndex < 0 || EditIndex >= items.Count) return NotFound();
            items[EditIndex.Value] = partner;
            reason = $"Cập nhật logo đối tác {Input.Name}.";
        }
        else
        {
            items.Add(partner);
            reason = $"Thêm logo đối tác {Input.Name}.";
        }

        return await SaveAsync(items, reason, IsEditing ? "Đã cập nhật đối tác." : "Đã thêm đối tác.");
    }

    public async Task<IActionResult> OnPostDeleteAsync(int index)
    {
        await LoadAsync();
        if (index < 0 || index >= Partners.Count) return NotFound();
        var items = Partners.ToList();
        var removed = items[index];
        items.RemoveAt(index);
        return await SaveAsync(items, $"Xóa logo đối tác {removed.Name}.", "Đã xóa đối tác.");
    }

    public async Task<IActionResult> OnPostToggleAsync(int index)
    {
        await LoadAsync();
        if (index < 0 || index >= Partners.Count) return NotFound();
        var items = Partners.ToList();
        var current = items[index];
        items[index] = current with { IsVisible = !current.IsVisible };
        var action = items[index].IsVisible ? "Hiển thị" : "Ẩn";
        return await SaveAsync(items, $"{action} logo đối tác {current.Name}.", $"Đã {action.ToLowerInvariant()} đối tác.");
    }

    public async Task<IActionResult> OnPostMoveAsync(int index, int direction)
    {
        await LoadAsync();
        var target = index + Math.Sign(direction);
        if (index < 0 || index >= Partners.Count || target < 0 || target >= Partners.Count) return RedirectToPage();
        var items = Partners.OrderBy(x => x.DisplayOrder).ToList();
        (items[index], items[target]) = (items[target], items[index]);
        items = items.Select((x, i) => x with { DisplayOrder = i + 1 }).ToList();
        return await SaveAsync(items, $"Sắp xếp lại logo đối tác {items[target].Name}.", "Đã cập nhật thứ tự đối tác.");
    }

    public async Task<JsonResult> OnPostUploadImageAsync(IFormFile? imageFile)
    {
        var url = await fileStorage.SaveImageAsync(imageFile, "uploads/partners", HttpContext.RequestAborted);
        return url is null
            ? new JsonResult(new { success = false, message = "Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB." }) { StatusCode = 400 }
            : new JsonResult(new { success = true, url });
    }

    private async Task LoadAsync()
    {
        HomepageContent = await homepage.GetAsync(HttpContext.RequestAborted);
        Partners = HomepageContent.Partners.OrderBy(x => x.DisplayOrder).ToArray();
    }

    private async Task<IActionResult> SaveAsync(List<HomepagePartnerItem> items, string reason, string successMessage)
    {
        var normalized = items.OrderBy(x => x.DisplayOrder).Select((x, i) => x with { DisplayOrder = i + 1 }).ToArray();
        var actor = Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId) ? userId : (Guid?)null;
        var result = await homepage.SaveAsync(HomepageContent with { Partners = normalized }, reason, actor, HttpContext.RequestAborted);
        if (!result.Success)
        {
            ModelState.AddModelError(string.Empty, result.Message);
            Partners = normalized;
            return Page();
        }

        TempData["AdminMessageSuccess"] = successMessage;
        return RedirectToPage();
    }

    public sealed class PartnerInput
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đối tác.")]
        [StringLength(120)]
        public string Name { get; set; } = "";

        [Required(ErrorMessage = "Vui lòng tải logo đối tác.")]
        [StringLength(500)]
        public string LogoUrl { get; set; } = "";

        [StringLength(500)]
        public string Url { get; set; } = "#";

        [Range(1, 999, ErrorMessage = "Thứ tự phải từ 1 đến 999.")]
        public int DisplayOrder { get; set; } = 1;

        public bool IsVisible { get; set; } = true;
    }
}

using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Topstar.Application.KocRegistration;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class RegisterKocStep2Model(IKocRegistrationService kocRegistrationService) : Microsoft.AspNetCore.Mvc.RazorPages.PageModel
{
    private const string Step1TempDataKey = "RegisterKocStep1";
    private const string Step2TempDataKey = "RegisterKocStep2";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public List<SelectListItem> CategoryOptions { get; private set; } = new();

    public IReadOnlyList<SocialPlatformVm> Platforms { get; } =
    [
        new("TikTok", true, "Link TikTok", "@username hoặc link đầy đủ", "VD: 50,000"),
        new("Facebook", false, "Link Facebook", "Link profile/page đầy đủ", "VD: 30,000"),
        new("Instagram", false, "Link Instagram", "@username hoặc link profile", "VD: 20,000"),
        new("YouTube", false, "Link YouTube", "Link kênh YouTube", "VD: 10,000")
    ];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        TempData.Peek(Step1TempDataKey);

        var cached = TempData.Peek(Step2TempDataKey) as string;
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var restored = JsonSerializer.Deserialize<InputModel>(cached);
            if (restored is not null)
            {
                Input = restored;
            }
        }

        EnsurePlatformRows();
        await LoadCategoriesAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        TempData.Peek(Step1TempDataKey);

        EnsurePlatformRows();
        NormalizeInput();

        for (var i = 0; i < Input.Platforms.Count; i++)
        {
            var row = Input.Platforms[i];
            if (row.Followers.HasValue && string.IsNullOrWhiteSpace(row.ChannelUrl))
            {
                ModelState.AddModelError($"Input.Platforms[{i}].ChannelUrl", "Vui lòng nhập link khi đã nhập số followers.");
            }

            if (row.SelectedCategoryCodes.Count > 3)
            {
                ModelState.AddModelError($"Input.Platforms[{i}].SelectedCategoryCodes", "Mỗi mạng xã hội chỉ chọn tối đa 3 danh mục.");
            }
        }

        var enteredPlatforms = Input.Platforms.Count(x =>
            !string.IsNullOrWhiteSpace(x.ChannelUrl) || x.Followers.HasValue);

        if (enteredPlatforms < 1)
        {
            for (var i = 0; i < Input.Platforms.Count; i++)
            {
                ModelState.AddModelError($"Input.Platforms[{i}].ChannelUrl", "Vui lòng nhập ít nhất 1 trong 4 mạng xã hội.");
            }
        }

        var selectedCategoryCount = Input.Platforms
            .SelectMany(x => x.SelectedCategoryCodes)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Count();

        if (selectedCategoryCount < 3)
        {
            for (var i = 0; i < Input.Platforms.Count; i++)
            {
                ModelState.AddModelError($"Input.Platforms[{i}].SelectedCategoryCodes", "Vui lòng chọn ít nhất 3 danh mục ngành hàng.");
            }
        }

        await LoadCategoriesAsync(cancellationToken);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        TempData[Step2TempDataKey] = JsonSerializer.Serialize(Input);
        return RedirectToPage("/Public/RegisterKocStep3");
    }

    private async Task LoadCategoriesAsync(CancellationToken cancellationToken)
    {
        var categories = await kocRegistrationService.GetCategoriesAsync(cancellationToken);
        CategoryOptions = categories
            .Select(x => new SelectListItem { Value = x.Code, Text = x.Name })
            .ToList();
    }

    private void EnsurePlatformRows()
    {
        if (Input.Platforms.Count == 0)
        {
            Input.Platforms = Platforms
                .Select(x => new SocialInputModel { Platform = x.Name })
                .ToList();
            return;
        }

        foreach (var platform in Platforms)
        {
            if (Input.Platforms.All(x => !x.Platform.Equals(platform.Name, StringComparison.OrdinalIgnoreCase)))
            {
                Input.Platforms.Add(new SocialInputModel { Platform = platform.Name });
            }
        }

        Input.Platforms = Input.Platforms
            .OrderBy(x => Array.FindIndex(Platforms.ToArray(), p => p.Name.Equals(x.Platform, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    private void NormalizeInput()
    {
        foreach (var item in Input.Platforms)
        {
            item.ChannelUrl = (item.ChannelUrl ?? string.Empty).Trim();
            item.SelectedCategoryCodes = item.SelectedCategoryCodes
                .SelectMany(x => (x ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
    }

    public sealed class InputModel
    {
        public List<SocialInputModel> Platforms { get; set; } = [];
    }

    public sealed class SocialInputModel
    {
        [Required]
        public string Platform { get; set; } = string.Empty;
        public string? ChannelUrl { get; set; }
        public int? Followers { get; set; }
        public List<string> SelectedCategoryCodes { get; set; } = [];
    }

    public sealed record SocialPlatformVm(
        string Name,
        bool IsRequired,
        string LinkLabel,
        string LinkPlaceholder,
        string FollowersPlaceholder);
}

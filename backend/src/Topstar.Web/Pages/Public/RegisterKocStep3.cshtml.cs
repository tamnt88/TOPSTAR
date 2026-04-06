using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Topstar.Application;
using Topstar.Application.KocRegistration;
using Topstar.Web.Services.Email;

namespace Topstar.Web.Pages.Public;

[AllowAnonymous]
public sealed class RegisterKocStep3Model(
    IAuthService authService,
    IKocRegistrationService kocRegistrationService,
    IAccountActivationService accountActivationService,
    IEmailSender emailSender,
    IEmailTemplateRenderer emailTemplateRenderer,
    IConfiguration configuration) : AuthPageBase
{
    private const string Step1TempDataKey = "RegisterKocStep1";
    private const string Step2TempDataKey = "RegisterKocStep2";
    private const string Step3TempDataKey = "RegisterKocStep3";

    [BindProperty]
    public InputModel Input { get; set; } = new();

    public string? ErrorMessage { get; private set; }

    public async Task<IActionResult> OnGetAsync(CancellationToken cancellationToken)
    {
        TempData.Peek(Step1TempDataKey);
        TempData.Peek(Step2TempDataKey);

        var cached = TempData.Peek(Step3TempDataKey) as string;
        if (!string.IsNullOrWhiteSpace(cached))
        {
            var restored = JsonSerializer.Deserialize<InputModel>(cached);
            if (restored is not null)
            {
                Input = restored;
            }
        }

        await Task.CompletedTask;
        return Page();
    }

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        var step1Json = TempData.Peek(Step1TempDataKey) as string;
        var step2Json = TempData.Peek(Step2TempDataKey) as string;

        var step1 = string.IsNullOrWhiteSpace(step1Json) ? null : JsonSerializer.Deserialize<RegisterKocStep1Model.InputModel>(step1Json);
        var step2 = string.IsNullOrWhiteSpace(step2Json) ? null : JsonSerializer.Deserialize<RegisterKocStep2Model.InputModel>(step2Json);

        if (step1 is null || step2 is null)
        {
            ErrorMessage = "Phiên đăng ký đã hết hạn. Vui lòng thực hiện lại từ bước 1.";
            return RedirectToPage("/Public/RegisterKocStep1");
        }

        NormalizeInput();
        ValidateChildren();
        if (!ModelState.IsValid)
        {
            TempData[Step3TempDataKey] = JsonSerializer.Serialize(Input);
            return Page();
        }

        var registerResult = await authService.RegisterAsync(
            new RegisterRequest(
                UserRoleType.Koc,
                step1.Email,
                step1.Phone,
                step1.Gender,
                step1.Password,
                step1.FullName,
                null,
                null,
                BuildDeviceContext()),
            cancellationToken);

        if (!registerResult.Success || registerResult.Data is null)
        {
            ErrorMessage = registerResult.Message;
            TempData[Step3TempDataKey] = JsonSerializer.Serialize(Input);
            return Page();
        }

        var userId = registerResult.Data.UserId;
        var completeProfileResult = await kocRegistrationService.CompleteProfileAsync(
            new CompleteKocRegistrationProfileRequest(
                userId,
                step1.StreetAddress,
                step1.ProvinceId,
                step1.WardId,
                Input.WeightKg,
                Input.HeightCm,
                Input.MaritalStatus,
                Input.HasChildren,
                Input.Children.Select(x => new KocChildInput(x.Age, x.Gender)).ToList(),
                step2.Platforms.Select(x => new KocSocialInput(
                    x.Platform,
                    x.ChannelUrl,
                    x.Followers,
                    x.SelectedCategoryCodes)).ToList()),
            cancellationToken);

        if (!completeProfileResult.Success)
        {
            ErrorMessage = completeProfileResult.Message;
            TempData[Step3TempDataKey] = JsonSerializer.Serialize(Input);
            return Page();
        }

        var activationTokenResult = await accountActivationService.CreateActivationTokenAsync(userId, TimeSpan.FromHours(24), cancellationToken);
        if (!activationTokenResult.Success || activationTokenResult.Data is null)
        {
            TempData["EmailWarning"] = "Tài khoản đã tạo nhưng chưa tạo được mã kích hoạt email.";
            TempData.Remove(Step1TempDataKey);
            TempData.Remove(Step2TempDataKey);
            TempData.Remove(Step3TempDataKey);
            return RedirectToPage("/Public/Login", new { registered = 1 });
        }

        var baseUrl = configuration["Email:ActivationBaseUrl"]?.TrimEnd('/') ?? $"{Request.Scheme}://{Request.Host}/kich-hoat-tai-khoan";
        var verifyUrl = $"{baseUrl}?token={activationTokenResult.Data.RawToken}";
        var contentHtml = $@"
            <p>Xin chào {System.Net.WebUtility.HtmlEncode(step1.FullName)},</p>
            <p>Vui lòng bấm vào nút bên dưới để kích hoạt tài khoản TopStar:</p>
            <p style=""margin:20px 0;"">
                <a href=""{verifyUrl}"" style=""display:inline-block;padding:12px 18px;background:linear-gradient(90deg,#e87b2e 0%,#f1c24b 100%);color:#fff;text-decoration:none;border-radius:10px;font-weight:700;"">
                    Kích hoạt tài khoản
                </a>
            </p>
            <p>Nếu nút không hoạt động, bạn có thể dùng liên kết sau:</p>
            <p><a href=""{verifyUrl}"">{verifyUrl}</a></p>
            <p>Liên kết có hiệu lực trong 24 giờ.</p>";
        var htmlBody = emailTemplateRenderer.Render(new EmailTemplateModel
        {
            ProjectName = "TopStar Platform",
            MainTitle = "Kích hoạt tài khoản",
            Preheader = "Hoàn tất bước kích hoạt tài khoản TopStar.",
            BodyHtml = contentHtml
        });

        var activationSent = await emailSender.SendAsync(step1.Email, "Kích hoạt tài khoản TopStar", htmlBody, cancellationToken);
        if (!activationSent)
        {
            TempData["EmailWarning"] = "Tài khoản đã tạo nhưng chưa gửi được email kích hoạt. Vui lòng thử lại chức năng gửi lại email.";
        }

        TempData.Remove(Step1TempDataKey);
        TempData.Remove(Step2TempDataKey);
        TempData.Remove(Step3TempDataKey);

        return RedirectToPage("/Public/Login", new { registered = 1 });
    }

    private void NormalizeInput()
    {
        if (!Input.HasChildren)
        {
            Input.Children.Clear();
            return;
        }

        Input.Children = Input.Children
            .Where(x => x.Age.HasValue || !string.IsNullOrWhiteSpace(x.Gender))
            .ToList();
    }

    private void ValidateChildren()
    {
        if (!Input.HasChildren)
        {
            return;
        }

        if (Input.Children.Count == 0)
        {
            ModelState.AddModelError("Input.Children", "Vui lòng nhập thông tin ít nhất 1 con.");
            return;
        }

        for (var i = 0; i < Input.Children.Count; i++)
        {
            var child = Input.Children[i];
            if (!child.Age.HasValue)
            {
                ModelState.AddModelError($"Input.Children[{i}].Age", "Vui lòng nhập tuổi.");
            }
            else if (child.Age < 0 || child.Age > 30)
            {
                ModelState.AddModelError($"Input.Children[{i}].Age", "Tuổi không hợp lệ.");
            }

            if (string.IsNullOrWhiteSpace(child.Gender))
            {
                ModelState.AddModelError($"Input.Children[{i}].Gender", "Vui lòng chọn giới tính.");
            }
        }
    }

    public sealed class InputModel
    {
        [Display(Name = "Cân nặng (kg)")]
        public decimal? WeightKg { get; set; }

        [Display(Name = "Chiều cao (cm)")]
        public decimal? HeightCm { get; set; }

        [Display(Name = "Số đo 3 vòng (cm)")]
        public string? Measurements { get; set; }

        [Display(Name = "Tình trạng hôn nhân")]
        public string? MaritalStatus { get; set; }

        [Display(Name = "Có con chưa?")]
        public bool HasChildren { get; set; }

        [Display(Name = "Bảng giá dịch vụ (VNĐ/post)")]
        public decimal? ServicePriceVnd { get; set; }

        public List<ChildInputModel> Children { get; set; } = [];
    }

    public sealed class ChildInputModel
    {
        public int? Age { get; set; }
        public string? Gender { get; set; }
    }
}

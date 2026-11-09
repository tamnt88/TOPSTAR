using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application;
using Topstar.Application.Profiles;
using Topstar.Web.Services.Email;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class AccountSecurityModel(
    IUserAccountAdminService accountService,
    IAccountActivationService activationService,
    IAuthService authService,
    IEmailSender emailSender,
    IEmailTemplateRenderer templates,
    IConfiguration configuration) : PageModel
{
    public UserAccountAdminDetail Detail { get; private set; } = null!;
    [TempData] public string? Message { get; set; }
    [TempData] public bool MessageSuccess { get; set; }

    public async Task<IActionResult> OnGetAsync(Guid userId,CancellationToken ct)
    {
        var detail=await accountService.GetDetailAsync(userId,ct);if(detail is null)return NotFound();
        Detail=detail;return Page();
    }
    public async Task<IActionResult> OnPostLockAsync(Guid userId,bool locked,string reason,CancellationToken ct)
        => Finish(userId,await accountService.ChangeLockAsync(AdminId(),userId,locked,reason,ct));
    public async Task<IActionResult> OnPostVerifyAsync(Guid userId,bool? emailConfirmed,bool? phoneConfirmed,string reason,CancellationToken ct)
        => Finish(userId,await accountService.SetVerificationAsync(AdminId(),userId,emailConfirmed,phoneConfirmed,reason,ct));
    public async Task<IActionResult> OnPostRevokeSessionAsync(Guid userId,Guid? sessionId,string reason,CancellationToken ct)
        => Finish(userId,await accountService.RevokeSessionAsync(AdminId(),userId,sessionId,reason,ct));
    public async Task<IActionResult> OnPostNoteAsync(Guid userId,string body,CancellationToken ct)
        => Finish(userId,await accountService.AddNoteAsync(AdminId(),userId,body,ct));

    public async Task<IActionResult> OnPostActivationAsync(Guid userId,string reason,CancellationToken ct)
    {
        var detail=await accountService.GetDetailAsync(userId,ct);if(detail is null)return NotFound();
        var result=await activationService.CreateActivationTokenAsync(userId,TimeSpan.FromHours(24),ct);
        if(!result.Success||result.Data is null)return Finish(userId,new(false,result.Message));
        var baseUrl=configuration["Email:ActivationBaseUrl"]?.TrimEnd('/')??$"{Request.Scheme}://{Request.Host}/kich-hoat-tai-khoan";
        var url=$"{baseUrl}?token={result.Data.RawToken}";
        var body=$"<p>Mã OTP kích hoạt mới:</p><p style=\"font-size:28px;font-weight:700;letter-spacing:6px\">{result.Data.RawCode}</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Kích hoạt tài khoản</a></p><p>Mã có hiệu lực 24 giờ.</p>";
        var sent=await emailSender.SendAsync(detail.Email,"Mã kích hoạt mới TopStar",templates.Render(new(){ProjectName="TopStar Platform",MainTitle="Kích hoạt tài khoản",Preheader="Mã kích hoạt tài khoản TopStar.",BodyHtml=body}),ct);
        if(sent)await accountService.RecordSecurityActionAsync(AdminId(),userId,"ResendActivation",reason,ct);
        return Finish(userId,new(sent,sent?"Đã gửi lại email kích hoạt.":"Không thể gửi email. Vui lòng kiểm tra cấu hình SMTP."));
    }
    public async Task<IActionResult> OnPostPasswordResetAsync(Guid userId,string reason,CancellationToken ct)
    {
        var detail=await accountService.GetDetailAsync(userId,ct);if(detail is null)return NotFound();
        var device=new DeviceContext($"admin-reset-{Guid.NewGuid():N}","Admin console","Server",null,null,null,null,Request.Headers.UserAgent,HttpContext.Connection.RemoteIpAddress?.ToString(),null,null);
        var result=await authService.RequestPasswordResetAsync(new(detail.Email,device),ct);
        if(!result.Success||result.Data is null)return Finish(userId,new(false,result.Message));
        var baseUrl=configuration["Email:ResetPasswordBaseUrl"]?.TrimEnd('/')??$"{Request.Scheme}://{Request.Host}/dat-lai-mat-khau";
        var url=$"{baseUrl}?token={result.Data.ResetToken}";
        var body=$"<p>Admin đã khởi tạo yêu cầu đặt lại mật khẩu cho tài khoản của bạn.</p><p><a href=\"{System.Net.WebUtility.HtmlEncode(url)}\">Đặt lại mật khẩu</a></p><p>Liên kết có hiệu lực 30 phút. Nếu không mong đợi email này, hãy liên hệ hỗ trợ.</p>";
        var sent=await emailSender.SendAsync(detail.Email,"Đặt lại mật khẩu TopStar",templates.Render(new(){ProjectName="TopStar Platform",MainTitle="Đặt lại mật khẩu",Preheader="Yêu cầu đặt lại mật khẩu.",BodyHtml=body}),ct);
        if(sent)await accountService.RecordSecurityActionAsync(AdminId(),userId,"AdminPasswordResetRequested",reason,ct);
        return Finish(userId,new(sent,sent?"Đã gửi email đặt lại mật khẩu.":"Không thể gửi email. Vui lòng kiểm tra cấu hình SMTP."));
    }
    private Guid AdminId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
    private IActionResult Finish(Guid userId,UserAccountAdminResult result){Message=result.Message;MessageSuccess=result.Success;return RedirectToPage(new{userId});}
}

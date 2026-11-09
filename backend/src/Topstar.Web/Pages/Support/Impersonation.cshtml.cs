using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Features.Auth;

namespace Topstar.Web.Pages.Support;

[Authorize]
public sealed class ImpersonationModel(TopstarDbContext db) : PageModel
{
    public async Task<IActionResult> OnPostExitAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ImpersonationClaims.OriginalAdminId), out var adminId))
            return Forbid();

        var admin = await (from user in db.Users.AsNoTracking()
                           join profile in db.AdminProfiles.AsNoTracking() on user.Id equals profile.UserId
                           join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                           join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                           where user.Id == adminId && user.Status == "Active" && profile.Status == "Active" && role.Code == "ADMIN"
                           select new { user.Id, user.Email, Name = profile.FullName }).FirstOrDefaultAsync(cancellationToken);
        if (admin is null)
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return Redirect("/admin/dang-nhap");
        }

        var targetId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var now = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = admin.Id, EntityName = "SupportImpersonation", EntityId = targetId ?? string.Empty, ActionName = "StopImpersonation", PayloadJson = JsonSerializer.Serialize(new { TargetUserId = targetId }), CreatedAtUtc = now });
        await db.SaveChangesAsync(cancellationToken);

        var claims = new[] { new Claim(ClaimTypes.NameIdentifier, admin.Id.ToString()), new Claim(ClaimTypes.Email, admin.Email), new Claim(ClaimTypes.Name, admin.Name), new Claim(ClaimTypes.Role, "Admin") };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8) });
        return Redirect("/Admin/Accounts");
    }
}

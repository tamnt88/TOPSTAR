using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using Topstar.Application;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Features.Auth;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class BrandsModel(IUserDirectoryService directoryService,IUserAccountAdminService accountService,IAuthService authService,TopstarDbContext db,ILogger<BrandsModel> logger) : PageModel
{
    [BindProperty(SupportsGet = true)] public string? Search { get; set; }
    [BindProperty(SupportsGet = true)] public string? Status { get; set; }
    [BindProperty(SupportsGet = true)] public string? Sort { get; set; }
    [BindProperty(SupportsGet = true)] public string? Type { get; set; }
    [BindProperty(SupportsGet = true)] public int PageNo { get; set; } = 1;
    [BindProperty(SupportsGet = true)] public int PageSize { get; set; } = 20;
    public UserDirectoryResult Result { get; private set; } = new([], [], 0, 0, 0, 0);

    public void OnGet()
    {
        Search = Search?.Trim();
        if (Search?.Length > 120) Search = Search[..120];
        Status = Status is "Approved" or "PendingReview" or "NeedUpdate" or "Rejected" or "Locked" ? Status : null;
        Sort = UserDirectorySorts.Normalize(Sort);
        Type = string.Equals(Type, "KOC", StringComparison.OrdinalIgnoreCase) ? "KOC" : "Brand";
        PageNo = Math.Max(1, PageNo);
        PageSize = PageSize is 20 or 50 or 100 ? PageSize : 20;
    }

    public async Task<JsonResult> OnGetDataAsync(
        int draw,
        int start,
        int length,
        string? query,
        string? profileStatus,
        string? userStatus,
        string? location,
        bool? hasViolation,
        string? sort,
        CancellationToken cancellationToken)
    {
        var type = string.Equals(Type, "KOC", StringComparison.OrdinalIgnoreCase) ? "KOC" : "Brand";
        query = query?.Trim();
        if (query?.Length > 120) query = query[..120];
        profileStatus = profileStatus is "Approved" or "PendingReview" or "NeedUpdate" or "Rejected" ? profileStatus : null;
        userStatus = userStatus is "Active" or "Locked" ? userStatus : null;
        location = string.IsNullOrWhiteSpace(location) ? null : location.Trim();
        if(location?.Length>100)location=location[..100];
        sort = UserDirectorySorts.Normalize(sort);

        var result = await directoryService.GetPageAsync(
            new UserDirectoryPageRequest(type, query, profileStatus, userStatus, location, hasViolation, sort, start, length),
            cancellationToken);

        var pageUserIds = type == "KOC"
            ? result.Kocs.Select(x => x.UserId).ToArray()
            : result.Brands.Select(x => x.UserId).ToArray();
        var activationStates = await db.Users.AsNoTracking()
            .Where(x => pageUserIds.Contains(x.Id))
            .ToDictionaryAsync(x => x.Id, x => x.EmailConfirmed, cancellationToken);

        var data = type == "KOC"
            ? result.Kocs.Select(x => new
            {
                x.ProfileId,
                x.UserId,
                Name = x.DisplayName,
                x.Email,
                Location = x.Region,
                x.ProfileStatus,
                x.UserStatus,
                EmailConfirmed = activationStates.GetValueOrDefault(x.UserId),
                x.Followers,
                x.EffectiveScore,
                PerformanceCount = x.CompletedCollaborationCount,
                x.ActiveViolationCount,
                x.CreatedAtUtc
            }).Cast<object>().ToList()
            : result.Brands.Select(x => new
            {
                x.ProfileId,
                x.UserId,
                Name = x.CompanyName,
                x.Email,
                Location = x.Industry,
                x.ProfileStatus,
                x.UserStatus,
                EmailConfirmed = activationStates.GetValueOrDefault(x.UserId),
                x.CampaignCount,
                x.ActiveCampaignCount,
                PerformanceCount = x.CompletedCampaignCount,
                x.TotalCampaignBudget,
                x.ActiveViolationCount,
                x.CreatedAtUtc
            }).Cast<object>().ToList();

        var isKoc=type=="KOC";
        var summary=isKoc
            ?new{
                Total=await(from p in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" select p.Id).CountAsync(cancellationToken),
                Active=await(from p in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status=="Active" && u.EmailConfirmed select p.Id).CountAsync(cancellationToken),
                Pending=await(from p in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" && p.Status=="PendingReview" select p.Id).CountAsync(cancellationToken),
                Locked=await(from p in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status=="Locked" select p.Id).CountAsync(cancellationToken),
                Unverified=await(from p in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" && !u.EmailConfirmed select p.Id).CountAsync(cancellationToken),
                WithViolations=await(from p in db.KocProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" && db.UserViolations.Any(v=>v.UserId==p.UserId&&v.Status=="Active") select p.Id).CountAsync(cancellationToken)}
            :new{
                Total=await(from p in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" select p.Id).CountAsync(cancellationToken),
                Active=await(from p in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status=="Active" && u.EmailConfirmed select p.Id).CountAsync(cancellationToken),
                Pending=await(from p in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" && p.Status=="PendingReview" select p.Id).CountAsync(cancellationToken),
                Locked=await(from p in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status=="Locked" select p.Id).CountAsync(cancellationToken),
                Unverified=await(from p in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" && !u.EmailConfirmed select p.Id).CountAsync(cancellationToken),
                WithViolations=await(from p in db.BrandProfiles.AsNoTracking() join u in db.Users.AsNoTracking() on p.UserId equals u.Id where u.Status!="Disabled" && db.UserViolations.Any(v=>v.UserId==p.UserId&&v.Status=="Active") select p.Id).CountAsync(cancellationToken)};

        return new JsonResult(new
        {
            draw = Math.Max(0, draw),
            recordsTotal = result.TotalCount,
            recordsFiltered = result.FilteredCount,
            data,
            summary
        });
    }

    // DataTables includes a columns[] descriptor for every column. On IIS the
    // resulting GET query can exceed requestFiltering.maxQueryString (404.15).
    public Task<JsonResult> OnPostDataAsync(
        int draw, int start, int length, string? query, string? profileStatus,
        string? userStatus, string? location, bool? hasViolation, string? sort,
        CancellationToken cancellationToken)
        => OnGetDataAsync(draw, start, length, query, profileStatus, userStatus,
            location, hasViolation, sort, cancellationToken);

    public async Task<IActionResult> OnGetDetailAsync(Guid userId,CancellationToken cancellationToken)
    {
        var account=await accountService.GetDetailAsync(userId,cancellationToken);if(account is null)return NotFound();
        var expectedType=string.Equals(Type,"KOC",StringComparison.OrdinalIgnoreCase)?"KOC":"Brand";
        if(!string.Equals(account.ProfileType,expectedType,StringComparison.OrdinalIgnoreCase))return NotFound();
        var brand=await db.BrandProfiles.AsNoTracking().FirstOrDefaultAsync(x=>x.UserId==userId,cancellationToken);
        var campaigns=brand is null?[]:await db.Campaigns.AsNoTracking().Where(x=>x.BrandProfileId==brand.Id).OrderByDescending(x=>x.CreatedAtUtc).Take(10)
            .Select(x=>new{x.Id,x.Title,x.Status,x.BudgetAmount,x.StartDate,x.EndDate,x.DesiredKocCount}).ToListAsync(cancellationToken);
        var campaignIds=campaigns.Select(x=>x.Id).ToArray();
        var fundings=campaignIds.Length==0?[]:await db.CampaignFundings.AsNoTracking().Where(x=>campaignIds.Contains(x.CampaignId)).OrderByDescending(x=>x.VersionNo)
            .Select(x=>new{x.CampaignId,x.Status,x.TotalPayableSnapshot,x.PaidAtUtc,x.PaymentMethod}).ToListAsync(cancellationToken);
        var violations=await db.UserViolations.AsNoTracking().Where(x=>x.UserId==userId).OrderByDescending(x=>x.CreatedAtUtc).Take(10)
            .Select(x=>new{x.Id,x.Category,x.Severity,x.PenaltyPoints,x.Status,x.CreatedAtUtc}).ToListAsync(cancellationToken);
        return new JsonResult(new{account,profile=new{Industry=brand?.Industry},campaigns,fundings,violations});
    }
    [TempData]public string? Message{get;set;}[TempData]public bool MessageSuccess{get;set;}
    public async Task<IActionResult> OnPostLockAsync(Guid userId,bool locked,string reason,CancellationToken cancellationToken)
    {
        var adminId=Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result=await accountService.ChangeLockAsync(adminId,userId,locked,reason,cancellationToken);Message=result.Message;MessageSuccess=result.Success;
        return RedirectToPage(new{search=Search,status=Status,sort=Sort,type=Type,pageNo=PageNo,pageSize=PageSize});
    }

    public async Task<JsonResult> OnPostToggleStatusAsync(Guid userId,bool locked,string reason,CancellationToken cancellationToken)
    {
        try
        {
            var adminId=Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result=await accountService.ChangeLockAsync(adminId,userId,locked,reason,cancellationToken);
            var detail=result.Success?await accountService.GetDetailAsync(userId,cancellationToken):null;
            Response.StatusCode=result.Success?200:400;
            return new JsonResult(new{success=result.Success,message=result.Message,userStatus=detail?.UserStatus,profileStatus=detail?.ProfileStatus});
        }
        catch(Exception exception)
        {
            logger.LogError(exception,"Admin failed to toggle account {UserId} to locked={Locked}",userId,locked);
            Response.StatusCode=500;
            return new JsonResult(new{success=false,message="Hệ thống không thể lưu trạng thái tài khoản. Lỗi đã được ghi nhật ký để kiểm tra."});
        }
    }

    public async Task<JsonResult> OnPostActivateAsync(Guid userId,string reason,CancellationToken cancellationToken)
    {
        try
        {
            var detail = await accountService.GetDetailAsync(userId, cancellationToken);
            if (detail is null || !string.Equals(detail.ProfileType, "KOC", StringComparison.OrdinalIgnoreCase))
            {
                Response.StatusCode = 404;
                return new JsonResult(new { success = false, message = "Không tìm thấy tài khoản KOC." });
            }
            if (detail.EmailConfirmed)
            {
                Response.StatusCode = 400;
                return new JsonResult(new { success = false, message = "Email của KOC đã được xác minh." });
            }

            var adminId=Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result=await accountService.SetVerificationAsync(adminId,userId,true,null,reason,cancellationToken);
            Response.StatusCode=result.Success?200:400;
            return new JsonResult(new{success=result.Success,message=result.Success?"Đã xác minh email cho KOC.":result.Message,emailConfirmed=result.Success});
        }
        catch(Exception exception)
        {
            logger.LogError(exception,"Admin failed to verify KOC email {UserId}",userId);
            Response.StatusCode=500;
            return new JsonResult(new{success=false,message="Không thể xác minh email lúc này."});
        }
    }

    public async Task<JsonResult> OnPostDeleteAccountAsync(Guid userId,string reason,CancellationToken cancellationToken)
    {
        try
        {
            var adminId=Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
            var result=await accountService.RemoveAsync(adminId,userId,reason,cancellationToken);
            Response.StatusCode=result.Success?200:400;
            return new JsonResult(new{success=result.Success,message=result.Message});
        }
        catch(Exception exception)
        {
            logger.LogError(exception,"Admin failed to remove account {UserId}",userId);
            Response.StatusCode=500;
            return new JsonResult(new{success=false,message="Không thể xóa tài khoản lúc này. Lỗi đã được ghi nhật ký."});
        }
    }

    public async Task<IActionResult> OnPostImpersonateAsync(Guid userId, string reason, CancellationToken cancellationToken)
    {
        reason = (reason ?? string.Empty).Trim();
        if (reason.Length is < 10 or > 500)
        {
            Message = "Vui lòng nhập lý do hỗ trợ từ 10 đến 500 ký tự."; MessageSuccess = false;
            return RedirectToPage(new { type = Type });
        }
        var adminId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        var target = await (from user in db.Users.AsNoTracking()
                            join userRole in db.UserRoles.AsNoTracking() on user.Id equals userRole.UserId
                            join role in db.Roles.AsNoTracking() on userRole.RoleId equals role.Id
                            where user.Id == userId && user.Status == "Active" && (role.Code == "KOC" || role.Code == "BRAND")
                            select new { user.Id, user.Email, Role = role.Code }).FirstOrDefaultAsync(cancellationToken);
        if (target is null)
        {
            Message = "Chỉ có thể hỗ trợ tài khoản KOC hoặc Brand đang hoạt động."; MessageSuccess = false;
            return RedirectToPage(new { type = Type });
        }
        var displayName = target.Role == "KOC"
            ? await db.KocProfiles.AsNoTracking().Where(x => x.UserId == target.Id).Select(x => x.DisplayName).FirstOrDefaultAsync(cancellationToken)
            : await db.BrandProfiles.AsNoTracking().Where(x => x.UserId == target.Id).Select(x => x.CompanyName).FirstOrDefaultAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(displayName))
        {
            Message = "Tài khoản chưa có hồ sơ hợp lệ để mở phiên hỗ trợ."; MessageSuccess = false;
            return RedirectToPage(new { type = Type });
        }
        var now = DateTime.UtcNow;
        db.AuditLogs.Add(new AuditLogEntity { ActorUserId = adminId, EntityName = "SupportImpersonation", EntityId = target.Id.ToString(), ActionName = "StartImpersonation", PayloadJson = JsonSerializer.Serialize(new { TargetUserId = target.Id, target.Role, Reason = reason }), CreatedAtUtc = now });
        await db.SaveChangesAsync(cancellationToken);

        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, target.Id.ToString()), new(ClaimTypes.Email, target.Email), new(ClaimTypes.Name, displayName),
            new(ClaimTypes.Role, target.Role == "KOC" ? "Koc" : "Brand"),
            new(ImpersonationClaims.OriginalAdminId, adminId.ToString()),
            new(ImpersonationClaims.OriginalAdminEmail, User.FindFirstValue(ClaimTypes.Email) ?? string.Empty),
            new(ImpersonationClaims.OriginalAdminName, User.Identity?.Name ?? "Admin"),
            new(ImpersonationClaims.StartedAtUtc, now.ToString("O"))
        };
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme)),
            new AuthenticationProperties { IsPersistent = false, ExpiresUtc = DateTimeOffset.UtcNow.AddHours(2), AllowRefresh = false });
        return Redirect(target.Role == "KOC" ? "/Koc/Dashboard" : "/Brand/Dashboard");
    }

    public async Task<IActionResult> OnPostEditAsync(
        Guid userId,
        string profileType,
        string name,
        string email,
        string? location,
        string reason,
        CancellationToken cancellationToken)
    {
        var adminId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var result = await accountService.UpdateDirectoryAsync(
            adminId, userId, profileType, name, email, location, reason, cancellationToken);
        Message = result.Message;
        MessageSuccess = result.Success;
        return RedirectToPage(new { type = Type });
    }

    public async Task<IActionResult> OnPostCreateAsync(
        string profileType,
        string name,
        string email,
        string? phone,
        string password,
        string? location,
        CancellationToken cancellationToken)
    {
        var isKoc = string.Equals(profileType, "KOC", StringComparison.OrdinalIgnoreCase);
        var role = isKoc ? UserRoleType.Koc : UserRoleType.Brand;
        var device = new DeviceContext(
            $"admin-create-{Guid.NewGuid():N}",
            "Admin account management",
            "Server",
            null, null, null, null,
            Request.Headers.UserAgent.ToString(),
            HttpContext.Connection.RemoteIpAddress?.ToString(),
            null, null);
        var result = await authService.RegisterAsync(
            new RegisterRequest(
                role,
                email,
                phone,
                null,
                password,
                name,
                isKoc ? null : name,
                phone,
                device),
            cancellationToken);

        if (!result.Success || result.Data is null)
        {
            Message = result.Message;
            MessageSuccess = false;
            return RedirectToPage(new { type = profileType });
        }

        var adminId = Guid.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)!.Value);
        var audit = await accountService.RecordCreateAsync(
            adminId,
            result.Data.UserId,
            isKoc ? "KOC" : "Brand",
            "Admin tạo tài khoản từ màn hình quản lý.",
            cancellationToken);
        if (!string.IsNullOrWhiteSpace(location))
        {
            await accountService.UpdateDirectoryAsync(
                adminId,
                result.Data.UserId,
                isKoc ? "KOC" : "Brand",
                name,
                email,
                location,
                "Admin bổ sung thông tin khi tạo tài khoản.",
                cancellationToken);
        }
        Message = audit.Success ? "Đã tạo tài khoản mới và lưu audit." : audit.Message;
        MessageSuccess = audit.Success;
        return RedirectToPage(new { type = profileType });
    }
}

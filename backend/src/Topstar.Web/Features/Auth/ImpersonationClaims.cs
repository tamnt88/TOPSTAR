namespace Topstar.Web.Features.Auth;

public static class ImpersonationClaims
{
    public const string OriginalAdminId = "topstar:impersonation:admin_id";
    public const string OriginalAdminEmail = "topstar:impersonation:admin_email";
    public const string OriginalAdminName = "topstar:impersonation:admin_name";
    public const string StartedAtUtc = "topstar:impersonation:started_at";
}

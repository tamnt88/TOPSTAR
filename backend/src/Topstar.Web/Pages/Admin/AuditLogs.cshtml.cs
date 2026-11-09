using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Auditing;
using System.Text;
using System.Security.Claims;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class AuditLogsModel(IAuditLogReader reader, IAuditLogExportRecorder exportRecorder) : PageModel
{
    [BindProperty(SupportsGet=true)] public string? Keyword { get; set; }
    [BindProperty(SupportsGet=true)] public string? EntityName { get; set; }
    [BindProperty(SupportsGet=true)] public string? ActionName { get; set; }
    [BindProperty(SupportsGet=true)] public DateTime? FromDate { get; set; }
    [BindProperty(SupportsGet=true)] public DateTime? ToDate { get; set; }
    [BindProperty(SupportsGet=true)] public int PageNumber { get; set; } = 1;
    public AuditLogPage Result { get; private set; } = new([], 0, [], [], 1, 1);

    public async Task OnGetAsync(CancellationToken ct)
    {
        DateTime? fromUtc = FromDate.HasValue ? DateTime.SpecifyKind(FromDate.Value.Date.AddHours(-7), DateTimeKind.Utc) : null;
        DateTime? toUtc = ToDate.HasValue ? DateTime.SpecifyKind(ToDate.Value.Date.AddHours(-7), DateTimeKind.Utc) : null;
        Result = await reader.SearchAsync(new AuditLogQuery(Keyword, EntityName, ActionName, fromUtc, toUtc, PageNumber), ct);
        PageNumber = Result.CurrentPage;
    }
    public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
    {
        DateTime? fromUtc=FromDate.HasValue?DateTime.SpecifyKind(FromDate.Value.Date.AddHours(-7),DateTimeKind.Utc):null;
        DateTime? toUtc=ToDate.HasValue?DateTime.SpecifyKind(ToDate.Value.Date.AddHours(-7),DateTimeKind.Utc):null;
        var query=new AuditLogQuery(Keyword,EntityName,ActionName,fromUtc,toUtc);
        var items=await reader.ExportAsync(query,ct);
        await exportRecorder.RecordAsync(Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!),query,items.Count,ct);
        var csv=new StringBuilder("\uFEFFTimeUtc,TimeUtcPlus7,Actor,Role,Action,Entity,EntityId\r\n");
        foreach(var x in items) csv.AppendJoin(',',AuditCsvRules.Escape(x.CreatedAtUtc.ToString("O")),AuditCsvRules.Escape(x.CreatedAtUtc.AddHours(7).ToString("yyyy-MM-dd HH:mm:ss")),AuditCsvRules.Escape(x.Actor),AuditCsvRules.Escape(x.ActorRole),AuditCsvRules.Escape(x.ActionName),AuditCsvRules.Escape(x.EntityName),AuditCsvRules.Escape(x.EntityId)).Append("\r\n");
        return File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv; charset=utf-8",$"audit-logs-{DateTime.UtcNow:yyyyMMdd-HHmmss}.csv");
    }
}

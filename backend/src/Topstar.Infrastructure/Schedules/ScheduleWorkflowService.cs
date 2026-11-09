using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Schedules;
using Topstar.Application.Billing;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Schedules;

public sealed class ScheduleWorkflowService(TopstarDbContext db) : IScheduleWorkflowService
{
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");

    public Task<IReadOnlyList<ScheduleWorkItem>> GetBrandItemsAsync(Guid userId, CancellationToken ct = default, Topstar.Application.Common.ListWindow? window = null) => LoadAsync(userId, true, ct, window: window);
    public Task<IReadOnlyList<ScheduleWorkItem>> GetBrandItemsForCampaignAsync(Guid userId, Guid campaignId, CancellationToken ct = default, Topstar.Application.Common.ListWindow? window = null) => LoadAsync(userId, true, ct, campaignId, window);
    public Task<IReadOnlyList<ScheduleWorkItem>> GetKocItemsAsync(Guid userId, CancellationToken ct = default, Topstar.Application.Common.ListWindow? window = null) => LoadAsync(userId, false, ct, window: window);

    public async Task<ScheduleCalendarView> GetCalendarAsync(Guid? userId,string role,DateOnly month,CancellationToken ct=default)
    {
        role=(role??"").Trim().ToUpperInvariant();if(role is not("ADMIN" or "BRAND" or "KOC"))return new(month,[]);if(role!="ADMIN"&&!userId.HasValue)return new(month,[]);var first=new DateOnly(month.Year,month.Month,1);var localStart=first.ToDateTime(TimeOnly.MinValue,DateTimeKind.Unspecified);var localEnd=first.AddMonths(1).ToDateTime(TimeOnly.MinValue,DateTimeKind.Unspecified);var utcStart=TimeZoneInfo.ConvertTimeToUtc(localStart,BusinessTimeZone);var utcEnd=TimeZoneInfo.ConvertTimeToUtc(localEnd,BusinessTimeZone);
        var rows=await(from s in db.Schedules.AsNoTracking() join p in db.Participations.AsNoTracking() on s.ParticipationId equals p.Id join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id where s.StartsAtUtc>=utcStart&&s.StartsAtUtc<utcEnd&&(role=="ADMIN"||(role=="BRAND"&&b.UserId==userId)||(role=="KOC"&&k.UserId==userId)) orderby s.StartsAtUtc select new{s.Id,c.Title,BrandName=b.CompanyName,KocName=k.DisplayName,s.StartsAtUtc,s.EndsAtUtc,s.Status,s.IsLateCancellation}).ToListAsync(ct);
        return new(first,rows.Where(x=>x.StartsAtUtc.HasValue&&x.EndsAtUtc.HasValue).Select(x=>new ScheduleCalendarEvent(x.Id,x.Title,x.BrandName,x.KocName,TimeZoneInfo.ConvertTimeFromUtc(x.StartsAtUtc!.Value,BusinessTimeZone),TimeZoneInfo.ConvertTimeFromUtc(x.EndsAtUtc!.Value,BusinessTimeZone),x.Status,x.IsLateCancellation)).ToList());
    }

    public async Task<ScheduleCalendarView> GetCalendarForCampaignAsync(Guid brandUserId,Guid campaignId,DateOnly month,CancellationToken ct=default)
    {
        var calendar=await GetCalendarAsync(brandUserId,"BRAND",month,ct);
        var title=await(from c in db.Campaigns.AsNoTracking() join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id where c.Id==campaignId&&b.UserId==brandUserId select c.Title).FirstOrDefaultAsync(ct);
        return title is null?new(calendar.Month,[]):new(calendar.Month,calendar.Events.Where(x=>x.CampaignTitle==title).ToList());
    }

    public async Task<ScheduleResult> ProposeAsync(Guid brandUserId, Guid participationId, DateTime startsAtLocal, DateTime endsAtLocal, CancellationToken ct = default)
    {
        var row = await (from p in db.Participations join c in db.Campaigns on p.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id join k in db.KocProfiles on p.KocProfileId equals k.Id where p.Id == participationId && b.UserId == brandUserId select new { Participation = p, Campaign = c, KocUserId = k.UserId }).FirstOrDefaultAsync(ct);
        if (row is null) return Fail("Không tìm thấy lượt tham gia hoặc bạn không có quyền đề xuất lịch.");
        if (!row.Campaign.RequiresBooking || row.Participation.Status != "Confirmed") return Fail("Chỉ có thể tạo lịch với KOC đã được xác nhận trong chiến dịch có yêu cầu đặt lịch.");
        if (!await (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                    join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                    where allocation.ParticipationId == participationId
                       && (funding.Status == CampaignFundingStatuses.Paid
                           || funding.Status == CampaignFundingStatuses.PartiallyReleased
                           || funding.Status == CampaignFundingStatuses.Released)
                    select allocation.Id).AnyAsync(ct))
            return Fail("Khoản thù lao của KOC này chưa được ký quỹ nên chưa thể tạo lịch làm việc.");
        if (await db.Schedules.AnyAsync(x => x.ParticipationId == participationId, ct)) return Fail("Lượt tham gia này đã có lịch.");
        var policy=await db.OperationalRuntimePolicies.AsNoTracking().SingleAsync(ct);var range = ValidateRange(startsAtLocal, endsAtLocal, row.Campaign.StartDate, row.Campaign.EndDate,policy.BookingLeadMinutes,policy.BookingMaxHours); if (!range.Success) return Fail(range.Error!);
        var now = DateTime.UtcNow; var schedule = new ScheduleEntity { Id = Guid.NewGuid(), ParticipationId = participationId, Status = ScheduleStatuses.Proposed, StartsAtUtc = range.StartUtc, EndsAtUtc = range.EndUtc, CreatedAtUtc = now };
        db.Schedules.Add(schedule); History(schedule, null, ScheduleStatuses.Proposed, "Brand đề xuất lịch", brandUserId, now); Notify(row.KocUserId, "KOC", "Schedule", "Brand vừa đề xuất lịch", "Vui lòng kiểm tra và xác nhận lịch booking.", schedule.Id, now); Audit(brandUserId, schedule.Id, "Propose", range, now);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException) { return Fail("Lịch vừa được tạo ở phiên khác. Vui lòng tải lại trang."); }
        return Ok("Đã gửi lịch đề xuất cho KOC.");
    }

    public async Task<ScheduleResult> ConfirmAsync(Guid actorUserId, bool isBrand, Guid scheduleId, CancellationToken ct = default)
    {
        var row = await OwnedAsync(scheduleId, actorUserId, isBrand, ct); if (row is null) return Fail("Không tìm thấy lịch hoặc bạn không có quyền xác nhận.");
        if (!ScheduleTransitions.CanConfirm(row.Schedule.Status)) return Fail("Lịch hiện tại không ở trạng thái chờ xác nhận.");
        var proposer = await db.ScheduleStatusHistories.AsNoTracking().Where(x => x.ScheduleId == scheduleId).OrderByDescending(x => x.CreatedAtUtc).Select(x => x.ActorUserId).FirstOrDefaultAsync(ct);
        if (proposer == actorUserId) return Fail("Bên tạo đề xuất gần nhất không thể tự xác nhận đề xuất của mình.");
        return await Transition(row, ScheduleStatuses.Confirmed, isBrand ? "Brand xác nhận lịch KOC đề xuất" : "KOC xác nhận lịch Brand đề xuất", actorUserId, isBrand ? row.KocUserId : row.BrandUserId, isBrand ? "Brand đã xác nhận lịch" : "KOC đã xác nhận lịch", ct);
    }

    public async Task<ScheduleResult> RescheduleAsync(Guid actorUserId, bool isBrand, Guid scheduleId, DateTime startsAtLocal, DateTime endsAtLocal, string note, CancellationToken ct = default)
    {
        var cleanNote = Clean(note); if (cleanNote is null || cleanNote.Length > 1000) return Fail("Vui lòng nhập lý do đổi lịch, tối đa 1.000 ký tự.");
        var row = await OwnedAsync(scheduleId, actorUserId, isBrand, ct); if (row is null) return Fail("Không tìm thấy lịch hoặc bạn không có quyền đổi lịch.");
        if (!ScheduleTransitions.CanReschedule(row.Schedule.Status, row.Schedule.RescheduleCount)) return Fail($"Không thể đổi lịch thêm. Giới hạn là {ScheduleTransitions.MaxReschedules} lần.");
        var policy=await db.OperationalRuntimePolicies.AsNoTracking().SingleAsync(ct);var range = ValidateRange(startsAtLocal, endsAtLocal, row.StartDate, row.EndDate,policy.BookingLeadMinutes,policy.BookingMaxHours); if (!range.Success) return Fail(range.Error!);
        row.Schedule.StartsAtUtc = range.StartUtc; row.Schedule.EndsAtUtc = range.EndUtc; row.Schedule.RescheduleCount++;
        return await Transition(row, ScheduleStatuses.Rescheduled, cleanNote, actorUserId, isBrand ? row.KocUserId : row.BrandUserId, isBrand ? "Brand đề xuất đổi lịch" : "KOC đề xuất đổi lịch", ct);
    }

    public async Task<ScheduleResult> CancelAsync(Guid actorUserId, bool isBrand, Guid scheduleId, string reason, CancellationToken ct = default)
    {
        var cleanReason = Clean(reason); if (cleanReason is null || cleanReason.Length > 1000) return Fail("Vui lòng nhập lý do hủy, tối đa 1.000 ký tự.");
        var row = await OwnedAsync(scheduleId, actorUserId, isBrand, ct); if (row is null) return Fail("Không tìm thấy lịch hoặc bạn không có quyền hủy.");
        if (!ScheduleTransitions.CanCancel(row.Schedule.Status)) return Fail("Lịch đã hủy hoặc không còn cho phép thay đổi.");
        var now=DateTime.UtcNow;var isLate=ScheduleTransitions.IsLateCancellation(row.Schedule.Status,row.Schedule.StartsAtUtc,now);row.Schedule.IsLateCancellation=isLate;row.Schedule.LateCancellationThresholdHours=isLate?ScheduleTransitions.LateCancellationHours:null;row.Schedule.CancelledAtUtc=now;row.Schedule.CancelledByUserId=actorUserId;
        return await Transition(row, ScheduleStatuses.Cancelled, isLate?$"[Hủy sát giờ ≤ {ScheduleTransitions.LateCancellationHours}h] {cleanReason}":cleanReason, actorUserId, isBrand ? row.KocUserId : row.BrandUserId, "Lịch làm việc đã bị hủy", ct);
    }

    public async Task<int> ProcessDueRemindersAsync(CancellationToken ct=default)
    {
        var now=DateTime.UtcNow;var cutoff=now.AddHours(24);var rows=await(from s in db.Schedules.AsNoTracking() join p in db.Participations.AsNoTracking() on s.ParticipationId equals p.Id join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id where s.Status==ScheduleStatuses.Confirmed&&s.StartsAtUtc>now&&s.StartsAtUtc<=cutoff select new{s.Id,s.StartsAtUtc,c.Title,BrandUser=b.UserId,KocUser=k.UserId}).ToListAsync(ct);var sent=0;
        foreach(var row in rows){var remaining=row.StartsAtUtc!.Value-now;var code=remaining<=TimeSpan.FromHours(2)?"REMINDER_2H":"REMINDER_24H";if(await db.ScheduleReminderEvents.AsNoTracking().AnyAsync(x=>x.ScheduleId==row.Id&&x.ReminderCode==code&&x.StartsAtUtcSnapshot==row.StartsAtUtc.Value,ct))continue;var label=code=="REMINDER_2H"?"2 giờ":"24 giờ";var body=$"Lịch {row.Title} bắt đầu lúc {row.StartsAtUtc.Value.ToLocalTime():dd/MM/yyyy HH:mm}.";db.ScheduleReminderEvents.Add(new(){Id=Guid.NewGuid(),ScheduleId=row.Id,ReminderCode=code,StartsAtUtcSnapshot=row.StartsAtUtc.Value,SentAtUtc=now,CreatedAtUtc=now});Notify(row.BrandUser,"BRAND","ScheduleReminder",$"Lịch booking bắt đầu trong {label}",body,row.Id,now);Notify(row.KocUser,"KOC","ScheduleReminder",$"Lịch booking bắt đầu trong {label}",body,row.Id,now);try{await db.SaveChangesAsync(ct);sent++;}catch(DbUpdateException){db.ChangeTracker.Clear();}}
        return sent;
    }

    private async Task<ScheduleResult> Transition(Owned row, string to, string note, Guid actor, Guid notifyUser, string title, CancellationToken ct)
    {
        var now = DateTime.UtcNow; var from = row.Schedule.Status; row.Schedule.Status = to; row.Schedule.UpdatedAtUtc = now;
        History(row.Schedule, from, to, note, actor, now); Notify(notifyUser, notifyUser==row.BrandUserId?"BRAND":"KOC", "Schedule", title, note, row.Schedule.Id, now); Audit(actor, row.Schedule.Id, to, new { FromStatus = from, row.Schedule.StartsAtUtc, row.Schedule.EndsAtUtc, Note = note }, now);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { throw new ScheduleConcurrencyException(); }
        return Ok(to == ScheduleStatuses.Confirmed ? "Đã xác nhận lịch." : to == ScheduleStatuses.Cancelled ? "Đã hủy lịch và lưu lý do." : "Đã gửi đề xuất đổi lịch.");
    }

    private async Task<IReadOnlyList<ScheduleWorkItem>> LoadAsync(Guid userId, bool brand, CancellationToken ct, Guid? campaignId = null, Topstar.Application.Common.ListWindow? window = null)
    {
        var source = from p in db.Participations.AsNoTracking()
                     join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id
                     join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                     join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id
                     join s0 in db.Schedules.AsNoTracking() on p.Id equals s0.ParticipationId into schedules
                     from s in schedules.DefaultIfEmpty()
                     where (!campaignId.HasValue || c.Id == campaignId.Value) && c.RequiresBooking &&
                           (p.Status == "Confirmed" || p.Status == "Completed") &&
                           (brand ? b.UserId == userId : k.UserId == userId && s != null)
                     select new { Schedule = s, ParticipationId = p.Id, c.Title, KocName = k.DisplayName };
        if (window is not null)
            window.AvailableCount = await source.CountAsync(x => x.Schedule != null && x.Schedule.StartsAtUtc > DateTime.UtcNow && x.Schedule.Status != ScheduleStatuses.Cancelled, ct);
        var rows = await source.OrderBy(x => x.Schedule == null ? 0 : x.Schedule.Status == ScheduleStatuses.Rescheduled ? 0 : 1)
            .ThenBy(x => x.Title).ThenBy(x => x.ParticipationId)
            .ToWindowAsync(window, ct,
                q => q.Where(x => window!.Search == null || x.Title.Contains(window.Search) || x.KocName.Contains(window.Search))
                      .Where(x => window!.Status == null || (x.Schedule == null ? "NotCreated" : x.Schedule.Status) == window.Status),
                x => x.Schedule == null ? "NotCreated" : x.Schedule.Status);
        var ids = rows.Where(x => x.Schedule != null).Select(x => x.Schedule!.Id).ToArray(); var historyRows = await db.ScheduleStatusHistories.AsNoTracking().Where(x => ids.Contains(x.ScheduleId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        var map = historyRows.GroupBy(x => x.ScheduleId).ToDictionary(g => g.Key, g => (IReadOnlyList<ScheduleHistoryItem>)g.Select(h => new ScheduleHistoryItem(h.FromStatus, h.ToStatus, h.StartsAtUtc, h.EndsAtUtc, h.Note, h.CreatedAtUtc)).ToList());
        return rows.Select(x => { var latestActor = x.Schedule is null ? null : historyRows.Where(h => h.ScheduleId == x.Schedule.Id).OrderByDescending(h => h.CreatedAtUtc).Select(h => h.ActorUserId).FirstOrDefault(); var status = x.Schedule?.Status ?? "NotCreated"; return new ScheduleWorkItem(x.Schedule?.Id, x.ParticipationId, x.Title, x.KocName, x.Schedule?.StartsAtUtc, x.Schedule?.EndsAtUtc, status, x.Schedule?.RescheduleCount ?? 0, ScheduleTransitions.CanConfirm(status) && latestActor != userId,x.Schedule?.IsLateCancellation??false,x.Schedule?.LateCancellationThresholdHours, x.Schedule is null ? [] : map.GetValueOrDefault(x.Schedule.Id, [])); }).ToList();
    }

    private async Task<Owned?> OwnedAsync(Guid id, Guid userId, bool brand, CancellationToken ct) => await (from s in db.Schedules join p in db.Participations on s.ParticipationId equals p.Id join c in db.Campaigns on p.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id join k in db.KocProfiles on p.KocProfileId equals k.Id where s.Id == id && (brand ? b.UserId == userId : k.UserId == userId) select new Owned(s, c.StartDate, c.EndDate, b.UserId, k.UserId)).FirstOrDefaultAsync(ct);
    private static RangeResult ValidateRange(DateTime startLocal, DateTime endLocal, DateOnly? campaignStart, DateOnly? campaignEnd,int leadMinutes,int maxHours)
    {
        if (!campaignStart.HasValue || !campaignEnd.HasValue) return new(false, null, null, "Chiến dịch chưa có đầy đủ ngày bắt đầu và ngày kết thúc.");
        var start = DateTime.SpecifyKind(startLocal, DateTimeKind.Unspecified); var end = DateTime.SpecifyKind(endLocal, DateTimeKind.Unspecified);
        if (end <= start) return new(false, null, null, "Giờ kết thúc phải sau giờ bắt đầu.");
        if (end - start > TimeSpan.FromHours(maxHours)) return new(false, null, null, $"Một lịch làm việc không được kéo dài quá {maxHours} giờ.");
        if (DateOnly.FromDateTime(start) < campaignStart.Value || DateOnly.FromDateTime(end) > campaignEnd.Value) return new(false, null, null, "Lịch phải nằm trong thời gian campaign.");
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(start, BusinessTimeZone); var endUtc = TimeZoneInfo.ConvertTimeToUtc(end, BusinessTimeZone);
        if (startUtc <= DateTime.UtcNow.AddMinutes(leadMinutes)) return new(false, null, null, $"Lịch phải bắt đầu sau hiện tại ít nhất {leadMinutes} phút.");
        return new(true, startUtc, endUtc, null);
    }
    private void History(ScheduleEntity s, string? from, string to, string note, Guid actor, DateTime now) => db.ScheduleStatusHistories.Add(new() { Id = Guid.NewGuid(), ScheduleId = s.Id, FromStatus = from, ToStatus = to, StartsAtUtc = s.StartsAtUtc, EndsAtUtc = s.EndsAtUtc, Note = note, ActorUserId = actor, CreatedAtUtc = now });
    private void Notify(Guid user, string audienceRole, string type, string title, string body, Guid sourceId, DateTime now) => db.Notifications.Add(NotificationFactory.Create(user,type,title,body,now,"Schedule",sourceId,audienceRole:audienceRole));
    private void Audit(Guid actor, Guid id, string action, object payload, DateTime now) => db.AuditLogs.Add(new() { ActorUserId = actor, EntityName = "Schedule", EntityId = id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = now });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim(); private static ScheduleResult Ok(string m) => new(true, m); private static ScheduleResult Fail(string m) => new(false, m);
    private sealed record Owned(ScheduleEntity Schedule, DateOnly? StartDate, DateOnly? EndDate, Guid BrandUserId, Guid KocUserId);
    private sealed record RangeResult(bool Success, DateTime? StartUtc, DateTime? EndUtc, string? Error);
}

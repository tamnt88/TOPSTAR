namespace Topstar.Application.Schedules;

public static class ScheduleStatuses
{
    public const string Proposed = "Proposed";
    public const string Confirmed = "Confirmed";
    public const string Rescheduled = "Rescheduled";
    public const string Cancelled = "Cancelled";
}

public static class ScheduleTransitions
{
    public const int MaxReschedules = 3;
    public const int LateCancellationHours = 24;
    public static bool CanConfirm(string status) => status is ScheduleStatuses.Proposed or ScheduleStatuses.Rescheduled;
    public static bool CanReschedule(string status, int count) => (status is ScheduleStatuses.Proposed or ScheduleStatuses.Confirmed or ScheduleStatuses.Rescheduled) && count < MaxReschedules;
    public static bool CanCancel(string status) => status is ScheduleStatuses.Proposed or ScheduleStatuses.Confirmed or ScheduleStatuses.Rescheduled;
    public static bool IsLateCancellation(string status,DateTime? startsAtUtc,DateTime cancelledAtUtc)
        => status==ScheduleStatuses.Confirmed&&startsAtUtc.HasValue&&startsAtUtc.Value<=cancelledAtUtc.AddHours(LateCancellationHours);
    public static string? DueReminderCode(DateTime startsAtUtc,DateTime evaluatedAtUtc)
        => startsAtUtc<=evaluatedAtUtc?null:startsAtUtc<=evaluatedAtUtc.AddHours(2)?"REMINDER_2H":startsAtUtc<=evaluatedAtUtc.AddHours(24)?"REMINDER_24H":null;
}

public sealed record ScheduleHistoryItem(string? FromStatus, string ToStatus, DateTime? StartsAtUtc, DateTime? EndsAtUtc, string? Note, DateTime CreatedAtUtc);
public sealed record ScheduleWorkItem(Guid? ScheduleId, Guid ParticipationId, string CampaignTitle, string KocName, DateTime? StartsAtUtc, DateTime? EndsAtUtc, string Status, int RescheduleCount, bool CanCurrentUserConfirm,bool IsLateCancellation,int? LateCancellationThresholdHours, IReadOnlyList<ScheduleHistoryItem> History);
public sealed record ScheduleResult(bool Success, string Message);
public sealed record ScheduleCalendarEvent(Guid ScheduleId,string CampaignTitle,string BrandName,string KocName,DateTime StartsAtLocal,DateTime EndsAtLocal,string Status,bool IsLateCancellation);
public sealed record ScheduleCalendarView(DateOnly Month,IReadOnlyList<ScheduleCalendarEvent> Events);
public sealed class ScheduleConcurrencyException : Exception { public ScheduleConcurrencyException() : base("Lịch vừa được cập nhật ở phiên khác. Vui lòng tải lại trang.") { } }

public interface IScheduleWorkflowService
{
    Task<IReadOnlyList<ScheduleWorkItem>> GetBrandItemsAsync(Guid brandUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<IReadOnlyList<ScheduleWorkItem>> GetBrandItemsForCampaignAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<IReadOnlyList<ScheduleWorkItem>> GetKocItemsAsync(Guid kocUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null);
    Task<ScheduleResult> ProposeAsync(Guid brandUserId, Guid participationId, DateTime startsAtLocal, DateTime endsAtLocal, CancellationToken cancellationToken = default);
    Task<ScheduleResult> ConfirmAsync(Guid actorUserId, bool isBrand, Guid scheduleId, CancellationToken cancellationToken = default);
    Task<ScheduleResult> RescheduleAsync(Guid actorUserId, bool isBrand, Guid scheduleId, DateTime startsAtLocal, DateTime endsAtLocal, string note, CancellationToken cancellationToken = default);
    Task<ScheduleResult> CancelAsync(Guid actorUserId, bool isBrand, Guid scheduleId, string reason, CancellationToken cancellationToken = default);
    Task<int> ProcessDueRemindersAsync(CancellationToken cancellationToken = default);
    Task<ScheduleCalendarView> GetCalendarAsync(Guid? userId,string role,DateOnly month,CancellationToken cancellationToken = default);
    Task<ScheduleCalendarView> GetCalendarForCampaignAsync(Guid brandUserId,Guid campaignId,DateOnly month,CancellationToken cancellationToken = default);
}

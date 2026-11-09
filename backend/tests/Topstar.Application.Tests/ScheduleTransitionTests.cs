using Topstar.Application.Schedules;
namespace Topstar.Application.Tests;
public sealed class ScheduleTransitionTests
{
    [Fact] public void Koc_can_confirm_proposal_or_reschedule() { Assert.True(ScheduleTransitions.CanConfirm(ScheduleStatuses.Proposed)); Assert.True(ScheduleTransitions.CanConfirm(ScheduleStatuses.Rescheduled)); Assert.False(ScheduleTransitions.CanConfirm(ScheduleStatuses.Confirmed)); }
    [Fact] public void Reschedule_is_limited() { Assert.True(ScheduleTransitions.CanReschedule(ScheduleStatuses.Confirmed, 2)); Assert.False(ScheduleTransitions.CanReschedule(ScheduleStatuses.Confirmed, 3)); }
    [Fact] public void Cancelled_is_terminal() { Assert.False(ScheduleTransitions.CanCancel(ScheduleStatuses.Cancelled)); Assert.False(ScheduleTransitions.CanReschedule(ScheduleStatuses.Cancelled, 0)); }
    [Fact] public void Late_cancellation_boundary_is_inclusive() { var start=new DateTime(2026,7,28,12,0,0,DateTimeKind.Utc);Assert.True(ScheduleTransitions.IsLateCancellation(ScheduleStatuses.Confirmed,start,start.AddHours(-24)));Assert.False(ScheduleTransitions.IsLateCancellation(ScheduleStatuses.Confirmed,start,start.AddHours(-24).AddSeconds(-1)));Assert.False(ScheduleTransitions.IsLateCancellation(ScheduleStatuses.Proposed,start,start.AddHours(-1))); }
    [Fact] public void Reminder_windows_do_not_include_started_schedule() { var now=new DateTime(2026,7,27,12,0,0,DateTimeKind.Utc);Assert.Equal("REMINDER_24H",ScheduleTransitions.DueReminderCode(now.AddHours(24),now));Assert.Equal("REMINDER_2H",ScheduleTransitions.DueReminderCode(now.AddHours(2),now));Assert.Null(ScheduleTransitions.DueReminderCode(now,now));Assert.Null(ScheduleTransitions.DueReminderCode(now.AddHours(25),now)); }
}

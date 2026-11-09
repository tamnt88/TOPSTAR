namespace Topstar.Application.Campaigns;
public static class CampaignScheduleRules
{
 public static DateOnly TodayVietnam => DateOnly.FromDateTime(DateTime.UtcNow.AddHours(7));
 public static string? ValidateDeadline(DateOnly? start,DateOnly? end,DateTime? due)
 {
  if(!due.HasValue)return null;
  var day=DateOnly.FromDateTime(due.Value);
  if(start.HasValue&&day<start.Value)return "Hạn nộp không được trước ngày bắt đầu dự kiến của chiến dịch.";
  if(end.HasValue&&day>end.Value)return "Hạn nộp không được sau ngày kết thúc dự kiến của chiến dịch.";
  return null;
 }
}

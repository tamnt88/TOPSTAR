using System.Net;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Email;

public interface ICampaignParticipationEmailService
{
    Task<bool> SendInvitationAsync(Guid participationId, CancellationToken cancellationToken = default);
    Task<bool> SendResponseAsync(Guid participationId, bool accepted, string? reason, CancellationToken cancellationToken = default);
    Task<bool> SendWorkOpenedAsync(Guid participationId, CancellationToken cancellationToken = default);
    Task SendLifecycleAsync(Guid campaignId,string action,string reason,CancellationToken cancellationToken=default);
}

public sealed class CampaignParticipationEmailService(
    TopstarDbContext db,
    IEmailSender sender,
    IEmailTemplateRenderer renderer,
    IConfiguration configuration) : ICampaignParticipationEmailService
{
    public async Task SendLifecycleAsync(Guid campaignId,string action,string reason,CancellationToken cancellationToken=default)
    {
        var campaign=await (from c in db.Campaigns.AsNoTracking() join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id join u in db.Users.AsNoTracking() on b.UserId equals u.Id where c.Id==campaignId select new{c.Title,b.CompanyName,u.Email}).SingleAsync(cancellationToken);
        var emails=await (from p in db.Participations.AsNoTracking() join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id join u in db.Users.AsNoTracking() on k.UserId equals u.Id where p.CampaignId==campaignId&&(p.Status=="Invited"||p.Status=="Confirmed"||p.Status=="Cancelled") select u.Email).Distinct().ToListAsync(cancellationToken);
        emails.Add(campaign.Email);
        var title=action switch{"Pause"=>"Chiến dịch tạm ngừng","Resume"=>"Chiến dịch tiếp tục",_=>"Chiến dịch đã hủy"};
        var detail=action=="Cancel"?"Công việc chưa triển khai đã được đóng. Bộ phận hỗ trợ sẽ đối soát các khoản ký quỹ và nghĩa vụ liên quan trước khi xử lý hoàn tiền.":"Các xác nhận tham gia và khoản ký quỹ được giữ nguyên. Công việc chỉ bắt đầu khi Brand chủ động mở cho KOC đủ điều kiện.";
        foreach(var email in emails.Where(e=>!string.IsNullOrWhiteSpace(e)).Distinct(StringComparer.OrdinalIgnoreCase))
            await sender.SendAsync(email,$"[TopStar] {title} - {campaign.Title}",renderer.Render(new EmailTemplateModel{MainTitle=title,Preheader=campaign.Title,BodyHtml=$"{Summary(campaign.Title,campaign.CompanyName)}<p>{H(reason)}</p><p>{H(detail)}</p>{Button(AbsoluteUrl("/Account/Login"),"Đăng nhập để xem chi tiết")}"}),cancellationToken);
    }
    public async Task<bool> SendInvitationAsync(Guid participationId, CancellationToken cancellationToken = default)
    {
        var row = await Query(participationId).FirstOrDefaultAsync(cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.KocEmail)) return false;
        var offer=await Topstar.Web.Services.Payments.KocTaxPresentation.ReadAsync(db,participationId,null,cancellationToken);
        var needsOffer=await db.Campaigns.AnyAsync(c=>c.Id==row.CampaignId&&c.CheckoutAtCreation,cancellationToken);
        if(needsOffer&&offer is null)return false;
        string Money(decimal? value)=>value.HasValue?value.Value.ToString("N0",System.Globalization.CultureInfo.GetCultureInfo("vi-VN"))+" đ":"Chờ xác minh";
        var remuneration=offer is null?"":$"<div style='padding:16px;background:#f0f9f7;border-radius:12px;margin:16px 0'><p><strong>Thù lao trước khấu trừ: {Money(offer.Gross)}</strong></p><p>Phí nền tảng KOC: {Money(offer.PlatformFee)}</p><p>Thuế dự kiến: {Money(offer.Tax)}</p><p><strong>Dự kiến thực nhận: {Money(offer.Net)}</strong></p><small>Số thuế được chốt khi cộng ví.</small></div>";
        var url = AbsoluteUrl($"/Koc/CampaignDetail?id={row.CampaignId}");
        var body = $"""
            <p>Xin chào <strong>{H(row.KocName)}</strong>,</p>
            <p><strong>{H(row.BrandName)}</strong> vừa gửi lời mời bạn tham gia chiến dịch dưới đây. Đây mới là lời mời; hệ thống chỉ ghi nhận bạn tham gia sau khi bạn chủ động đồng ý.</p>
            {Summary(row.CampaignTitle, row.BrandName)}
            {remuneration}
            {Button(url, "Xem và phản hồi lời mời")}
            <p style="color:#6b7280;font-size:13px">Bạn có thể đồng ý hoặc từ chối ngay trên trang chi tiết chiến dịch. Brand sẽ được thông báo về lựa chọn của bạn.</p>
            """;
        return await sender.SendAsync(row.KocEmail, $"[TopStar] Lời mời tham gia chiến dịch {row.CampaignTitle}", renderer.Render(new EmailTemplateModel
        {
            MainTitle = "Bạn có lời mời hợp tác mới",
            Preheader = $"{row.BrandName} mời bạn tham gia {row.CampaignTitle}",
            BodyHtml = body
        }), cancellationToken);
    }

    public async Task<bool> SendResponseAsync(Guid participationId, bool accepted, string? reason, CancellationToken cancellationToken = default)
    {
        var row = await Query(participationId).FirstOrDefaultAsync(cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.BrandEmail)) return false;
        var url = AbsoluteUrl($"/Brand/Shortlist?id={row.CampaignId}");
        var result = accepted ? "đồng ý" : "từ chối";
        var reasonHtml = !accepted && !string.IsNullOrWhiteSpace(reason)
            ? $"<div style=\"margin:16px 0;padding:12px 14px;background:#fff7ed;border:1px solid #fed7aa;border-radius:10px\"><strong>Lý do:</strong> {H(reason)}</div>"
            : string.Empty;
        var body = $"""
            <p>Xin chào <strong>{H(row.BrandName)}</strong>,</p>
            <p>KOC <strong>{H(row.KocName)}</strong> đã <strong>{result}</strong> lời mời tham gia chiến dịch.</p>
            {Summary(row.CampaignTitle, row.BrandName)}
            {reasonHtml}
            {Button(url, "Mở trang quản lý KOC")}
            """;
        return await sender.SendAsync(row.BrandEmail, $"[TopStar] KOC đã {result} lời mời - {row.CampaignTitle}", renderer.Render(new EmailTemplateModel
        {
            MainTitle = accepted ? "KOC đã đồng ý tham gia" : "KOC đã từ chối lời mời",
            Preheader = $"{row.KocName} đã {result} tham gia {row.CampaignTitle}",
            BodyHtml = body
        }), cancellationToken);
    }

    public async Task<bool> SendWorkOpenedAsync(Guid participationId, CancellationToken cancellationToken = default)
    {
        var row = await Query(participationId).FirstOrDefaultAsync(cancellationToken);
        if (row is null || string.IsNullOrWhiteSpace(row.KocEmail)) return false;
        var url = AbsoluteUrl("/Koc/Jobs");
        var body = $"""
            <p>Xin chào <strong>{H(row.KocName)}</strong>,</p>
            <p><strong>{H(row.BrandName)}</strong> đã hoàn tất ký quỹ và chủ động mở công việc cho bạn.</p>
            {Summary(row.CampaignTitle, row.BrandName)}
            <div style="margin:16px 0;padding:14px;background:#ecfdf5;border:1px solid #a7f3d0;border-radius:10px;color:#065f46"><strong>Trạng thái:</strong> Bạn có thể bắt đầu thực hiện và nộp nội dung theo yêu cầu chiến dịch.</div>
            {Button(url, "Mở danh sách công việc")}
            """;
        return await sender.SendAsync(row.KocEmail, $"[TopStar] Công việc đã được mở - {row.CampaignTitle}", renderer.Render(new EmailTemplateModel
        {
            MainTitle = "Công việc đã sẵn sàng",
            Preheader = $"Brand đã mở công việc cho chiến dịch {row.CampaignTitle}",
            BodyHtml = body
        }), cancellationToken);
    }

    private IQueryable<EmailRow> Query(Guid participationId) =>
        from participation in db.Participations.AsNoTracking()
        join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
        join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
        join brandUser in db.Users.AsNoTracking() on brand.UserId equals brandUser.Id
        join koc in db.KocProfiles.AsNoTracking() on participation.KocProfileId equals koc.Id
        join kocUser in db.Users.AsNoTracking() on koc.UserId equals kocUser.Id
        where participation.Id == participationId
        select new EmailRow(campaign.Id, campaign.Title, brand.CompanyName, brandUser.Email, koc.DisplayName, kocUser.Email);

    private string AbsoluteUrl(string path) => $"{(configuration["Email:Template:WebsiteUrl"] ?? "https://topstar.vn").TrimEnd('/')}{path}";
    private static string H(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
    private static string Summary(string campaign, string brand) => $"<table role=\"presentation\" width=\"100%\" style=\"margin:18px 0;background:#f7f2ff;border:1px solid #e6d8ff;border-radius:12px\"><tr><td style=\"padding:16px\"><div style=\"color:#6d28d9;font-size:12px;font-weight:700;text-transform:uppercase\">Chiến dịch</div><div style=\"font-size:18px;font-weight:700;margin:4px 0\">{H(campaign)}</div><div style=\"color:#6b7280\">Brand: {H(brand)}</div></td></tr></table>";
    private static string Button(string url, string label) => $"<div style=\"margin:22px 0\"><a href=\"{H(url)}\" style=\"display:inline-block;background:#6d28d9;color:#fff;text-decoration:none;font-weight:700;padding:12px 20px;border-radius:9px\">{H(label)}</a></div>";
    private sealed record EmailRow(Guid CampaignId, string CampaignTitle, string BrandName, string BrandEmail, string KocName, string KocEmail);
}

using System.Net;
using System.Text;

namespace Topstar.Web.Services.Email;

public interface IEmailTemplateRenderer
{
    string Render(EmailTemplateModel model);
}

public sealed class EmailTemplateRenderer(IConfiguration configuration) : IEmailTemplateRenderer
{
    public string Render(EmailTemplateModel model)
    {
        var websiteName = configuration["Email:Template:WebsiteName"] ?? "TopStar";
        var websiteUrl = configuration["Email:Template:WebsiteUrl"] ?? "https://topstar.vn";
        var address = configuration["Email:Template:Address"] ?? "TP. Hồ Chí Minh, Việt Nam";
        var contactEmail = configuration["Email:Template:ContactEmail"] ?? "hello@topstar.vn";
        var hotline = configuration["Email:Template:Hotline"] ?? "1900-xxxx";
        const string logoUrl = "{{TOPSTAR_EMAIL_LOGO}}";
        var projectName = model.ProjectName?.Trim();
        if (string.IsNullOrWhiteSpace(projectName))
        {
            projectName = websiteName;
        }

        var mainTitle = WebUtility.HtmlEncode(model.MainTitle ?? string.Empty);
        var preheader = string.IsNullOrWhiteSpace(model.Preheader)
            ? "Thông báo từ hệ thống TopStar."
            : WebUtility.HtmlEncode(model.Preheader);

        var sb = new StringBuilder();
        sb.AppendLine("<!doctype html>");
        sb.AppendLine("<html lang=\"vi\">");
        sb.AppendLine("<head><meta charset=\"utf-8\"><meta name=\"viewport\" content=\"width=device-width,initial-scale=1\"></head>");
        sb.AppendLine("<body style=\"margin:0;padding:0;background:#f6f0e4;font-family:Arial,Helvetica,sans-serif;color:#2e2620;\">");
        sb.AppendLine($"<div style=\"display:none;max-height:0;overflow:hidden;opacity:0;\">{preheader}</div>");
        sb.AppendLine("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"padding:24px 12px;\">");
        sb.AppendLine("<tr><td align=\"center\">");
        sb.AppendLine("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\" style=\"max-width:680px;background:#ffffff;border-radius:16px;overflow:hidden;border:1px solid #ecd7b2;\">");
        sb.AppendLine("<tr><td style=\"background:linear-gradient(135deg,#fff3da 0%,#f8e8c5 100%);padding:18px 22px;border-bottom:1px solid #ecd7b2;\">");
        sb.AppendLine("<table role=\"presentation\" width=\"100%\" cellspacing=\"0\" cellpadding=\"0\"><tr>");
        sb.AppendLine($"<td style=\"width:164px;vertical-align:middle;\"><img src=\"{logoUrl}\" alt=\"{WebUtility.HtmlEncode(websiteName)}\" width=\"148\" style=\"display:block;width:148px;max-width:100%;height:auto;border:0;\"></td>");
        sb.AppendLine("<td style=\"vertical-align:top;padding-left:12px;\">");
        sb.AppendLine($"<div style=\"font-size:14px;color:#7f5e2b;font-weight:600;line-height:1.4;\">{WebUtility.HtmlEncode(projectName)}</div>");
        sb.AppendLine($"<div style=\"font-size:22px;color:#2d1f0f;font-weight:700;line-height:1.3;margin-top:2px;\">{mainTitle}</div>");
        sb.AppendLine("</td></tr></table>");
        sb.AppendLine("</td></tr>");
        sb.AppendLine($"<tr><td style=\"padding:24px 22px;font-size:15px;line-height:1.7;color:#3f3327;\">{model.BodyHtml}</td></tr>");
        sb.AppendLine("<tr><td style=\"padding:18px 22px;background:#1f140c;color:#f7e5c8;font-size:13px;line-height:1.7;\">");
        sb.AppendLine($"<div style=\"font-weight:700;color:#ffd58f;margin-bottom:8px;\">{WebUtility.HtmlEncode(websiteName)}</div>");
        sb.AppendLine($"<div>Địa chỉ: {WebUtility.HtmlEncode(address)}</div>");
        sb.AppendLine($"<div>Email: <a href=\"mailto:{WebUtility.HtmlEncode(contactEmail)}\" style=\"color:#ffd58f;text-decoration:none;\">{WebUtility.HtmlEncode(contactEmail)}</a></div>");
        sb.AppendLine($"<div>Hotline: <a href=\"tel:{WebUtility.HtmlEncode(hotline)}\" style=\"color:#ffd58f;text-decoration:none;\">{WebUtility.HtmlEncode(hotline)}</a></div>");
        sb.AppendLine($"<div>Website: <a href=\"{WebUtility.HtmlEncode(websiteUrl)}\" style=\"color:#ffd58f;text-decoration:none;\">{WebUtility.HtmlEncode(websiteUrl)}</a></div>");
        sb.AppendLine("</td></tr></table>");
        sb.AppendLine("</td></tr></table></body></html>");
        return sb.ToString();
    }

}

public sealed class EmailTemplateModel
{
    public string? ProjectName { get; init; }
    public required string MainTitle { get; init; }
    public string? Preheader { get; init; }
    public required string BodyHtml { get; init; }
}

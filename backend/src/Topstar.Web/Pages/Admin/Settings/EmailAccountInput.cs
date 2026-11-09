using System.ComponentModel.DataAnnotations;
namespace Topstar.Web.Pages.Admin.Settings;
public sealed class EmailAccountInput
{
 public Guid? Id{get;set;}[Required(ErrorMessage="Vui lòng nhập tên tài khoản.")][StringLength(100,MinimumLength=2,ErrorMessage="Tên tài khoản phải từ 2 đến 100 ký tự.")]public string Name{get;set;}="";
 [Required(ErrorMessage="Vui lòng nhập địa chỉ email.")][EmailAddress(ErrorMessage="Địa chỉ email không đúng định dạng.")][StringLength(200)]public string EmailAddress{get;set;}="";
 [Required(ErrorMessage="Vui lòng nhập tên hiển thị.")][StringLength(150,MinimumLength=2,ErrorMessage="Tên hiển thị phải từ 2 đến 150 ký tự.")]public string DisplayName{get;set;}="";
 [Required(ErrorMessage="Vui lòng nhập SMTP Host.")][StringLength(200)]public string? SmtpHost{get;set;}[Required(ErrorMessage="Vui lòng nhập SMTP Port.")][Range(1,65535,ErrorMessage="SMTP Port phải từ 1 đến 65.535.")]public int? SmtpPort{get;set;}[StringLength(200)]public string? SmtpUsername{get;set;}[StringLength(500)]public string? SmtpPassword{get;set;}public bool SmtpEnableSsl{get;set;}=true;
 [StringLength(200)]public string? ImapHost{get;set;}[Range(1,65535,ErrorMessage="IMAP Port phải từ 1 đến 65.535.")]public int? ImapPort{get;set;}[StringLength(200)]public string? ImapUsername{get;set;}[StringLength(500)]public string? ImapPassword{get;set;}public bool ImapEnableSsl{get;set;}=true;
 public bool IsDefaultSender{get;set;}public bool IsDefaultReceiver{get;set;}public bool IsActive{get;set;}=true;public bool IsVisible{get;set;}=true;
}

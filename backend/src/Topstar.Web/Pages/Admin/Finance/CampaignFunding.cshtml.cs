using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Billing;
using Topstar.Application.Campaigns;
using Topstar.Infrastructure.Persistence;
using Topstar.Infrastructure.Entities;
using Topstar.Web.Services.Storage;
using Topstar.Web.Services.Payments;
namespace Topstar.Web.Pages.Admin.Finance;
[Authorize(Roles="Admin")]
public sealed class CampaignFundingModel(ICampaignFundingService service,TopstarDbContext db,IFileStorageService storage,VnPayCallbackJournal journal):PageModel
{
 public IReadOnlyDictionary<string,IReadOnlyList<VnPayCallbackLogEntity>> VnPayLogs{get;private set;}=new Dictionary<string,IReadOnlyList<VnPayCallbackLogEntity>>();
 private async Task LoadVnPayLogsAsync(CancellationToken ct){VnPayLogs=await journal.ReadAsync(Items.Where(x=>x.Id.HasValue&&x.PaymentMethod=="VnPay").Select(x=>"F"+x.Id!.Value.ToString("N")),ct);}
 public Topstar.Application.Common.ListWindow Window{get;private set;}=new();public IReadOnlyList<CampaignFundingItem> Items{get;private set;}=[];public HashSet<Guid> AdjudicatedCampaignIds{get;private set;}=[];public IReadOnlyDictionary<Guid,IReadOnlyList<CampaignFundLedgerEntity>> Ledgers{get;private set;}=new Dictionary<Guid,IReadOnlyList<CampaignFundLedgerEntity>>();public IReadOnlyDictionary<Guid,IReadOnlyList<CampaignFundingEvidenceEntity>> AdminEvidences{get;private set;}=new Dictionary<Guid,IReadOnlyList<CampaignFundingEvidenceEntity>>();public IReadOnlyDictionary<Guid,CampaignFundingEvidenceReviewEntity> LatestEvidenceReviews{get;private set;}=new Dictionary<Guid,CampaignFundingEvidenceReviewEntity>();[TempData]public string? Message{get;set;}[TempData]public bool Success{get;set;}
 public string Queue{get;private set;}="all";
 public async Task OnGetAsync(CancellationToken ct){Queue=Request.Query["queue"].ToString();if(Queue is not ("pending" or "funded" or "settlement" or "exception"))Queue="all";Window=Topstar.Web.Features.Common.Paging.ListWindowRequest.Read(Request,"campaignFundingTable");Window.AllowedStatuses=Queue switch{"pending"=>new[]{"Pending"},"funded"=>new[]{"Paid"},"settlement"=>new[]{"Paid","PartiallyReleased"},"exception"=>new[]{"Cancelled","Refunded","PartiallyReleased"},_=>null};Items=await service.GetForAdminAsync(ct,Window);await LoadVnPayLogsAsync(ct);AdjudicatedCampaignIds=(await db.Campaigns.AsNoTracking().Where(x=>x.Status==CampaignStatuses.Cancelled&&x.CancellationDisputeCaseId!=null).Select(x=>x.Id).ToListAsync(ct)).ToHashSet();var ids=Items.Where(x=>x.Id.HasValue).Select(x=>x.Id!.Value).ToList();Ledgers=(await db.CampaignFundLedgers.AsNoTracking().Where(x=>ids.Contains(x.FundingId)).OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct)).GroupBy(x=>x.FundingId).ToDictionary(x=>x.Key,x=>(IReadOnlyList<CampaignFundLedgerEntity>)x.ToList());var evidences=await db.CampaignFundingEvidences.AsNoTracking().Where(x=>ids.Contains(x.FundingId)).OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct);AdminEvidences=evidences.GroupBy(x=>x.FundingId).ToDictionary(x=>x.Key,x=>(IReadOnlyList<CampaignFundingEvidenceEntity>)x.ToList());var evidenceIds=evidences.Select(x=>x.Id).ToList();LatestEvidenceReviews=(await db.CampaignFundingEvidenceReviews.AsNoTracking().Where(x=>evidenceIds.Contains(x.EvidenceId)).OrderByDescending(x=>x.CreatedAtUtc).ToListAsync(ct)).GroupBy(x=>x.EvidenceId).ToDictionary(x=>x.Key,x=>x.First());}
 public async Task<IActionResult> OnGetExportAsync(CancellationToken ct)
 {
  var rows=await service.GetForAdminAsync(ct);var csv=new StringBuilder("\uFEFFChiến dịch,Brand,Mã yêu cầu,Ngân sách,Phí nền tảng,Phí cổng,Tổng thanh toán,Phương thức,Trạng thái,Mã giao dịch,Ngân hàng hoàn,Số tài khoản hoàn,Chủ tài khoản hoàn,Ngày tạo yêu cầu,Ngày thanh toán\r\n");
  foreach(var x in rows.Where(x=>x.Id.HasValue))csv.AppendLine(string.Join(',',Csv(x.CampaignTitle),Csv(x.BrandName),Csv(x.OrderNumber),x.CampaignBudget,x.PlatformFeeSnapshot,x.GatewayFeeSnapshot,x.TotalPayableSnapshot,Csv(x.PaymentMethod),Csv(x.Status),Csv(x.PaymentReference),Csv(x.RefundBankName),Csv(x.RefundAccountNumber),Csv(x.RefundAccountHolder),Csv(x.SubmittedAtUtc?.AddHours(7).ToString("dd/MM/yyyy HH:mm")),Csv(x.PaidAtUtc?.AddHours(7).ToString("dd/MM/yyyy HH:mm"))));
  return File(Encoding.UTF8.GetBytes(csv.ToString()),"text/csv; charset=utf-8",$"doi-soat-quy-chien-dich-{DateTime.UtcNow.AddHours(7):yyyyMMdd-HHmm}.csv");
 }
 private static string Csv(string? value)=>$"\"{(value??string.Empty).Replace("\"","\"\"")}\"";
 private Guid UserId()=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 public async Task<IActionResult> OnPostPaidAsync(Guid id,string reference,string? manualReason,CancellationToken ct)=>Finish(await service.ConfirmPaidAsync(UserId(),id,reference,manualReason,ct));
 public async Task<IActionResult> OnPostSettleAsync(Guid allocationId,decimal acceptedAmount,string reason,CancellationToken ct)=>Finish(await service.SettleAllocationAsync(UserId(),allocationId,acceptedAmount,reason,ct));
 public async Task<IActionResult> OnPostRefundAsync(Guid fundingId,decimal amount,string reference,string reason,CancellationToken ct)
 {
  var hasRefundAccount=await db.CampaignFundings.AsNoTracking().AnyAsync(x=>x.Id==fundingId&&x.RefundBankName!=null&&x.RefundBankName!=""&&x.RefundAccountNumber!=null&&x.RefundAccountNumber!=""&&x.RefundAccountHolder!=null&&x.RefundAccountHolder!="",ct);
  if(!hasRefundAccount){Message="Yêu cầu thanh toán chưa có đầy đủ tài khoản nhận hoàn.";Success=false;return RedirectToPage();}
  return Finish(await service.ConfirmRefundAsync(UserId(),fundingId,amount,reference,reason,ct));
 }
 public async Task<IActionResult> OnPostEvidenceAsync(Guid fundingId,string evidenceType,string reference,decimal? amount,string note,IFormFile? evidenceFile,CancellationToken ct)
 {
  note=(note??string.Empty).Trim();reference=(reference??string.Empty).Trim();
  if(reference.Length<3||reference.Length>200||note.Length<10||note.Length>500){Message="Vui lòng nhập đầy đủ mã giao dịch và ghi chú chứng từ.";Success=false;return RedirectToPage();}
  var url=await storage.SaveCampaignFundingEvidenceAsync(evidenceFile,ct);
  if(url is null){Message="Vui lòng chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB.";Success=false;return RedirectToPage();}
  CampaignFundingResult result;
  try{result=await service.AddAdminEvidenceAsync(UserId(),fundingId,evidenceType,url,reference,amount,note,ct);}catch{await storage.DeleteCampaignFundingEvidenceAsync(url,ct);Message="Không thể lưu chứng từ vào lúc này.";Success=false;return RedirectToPage();}
  if(!result.Success)await storage.DeleteCampaignFundingEvidenceAsync(url,ct);return Finish(result);
 }
 public async Task<IActionResult> OnPostReviewEvidenceAsync(Guid evidenceId,string reviewStatus,string reviewReason,CancellationToken ct)=>Finish(await service.ReviewEvidenceAsync(UserId(),evidenceId,reviewStatus,reviewReason,ct));
 private IActionResult Finish(CampaignFundingResult result){Message=result.Message;Success=result.Success;return RedirectToPage();}
}

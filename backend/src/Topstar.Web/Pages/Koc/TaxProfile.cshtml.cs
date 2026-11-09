using System.Security.Claims;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using Topstar.Infrastructure.Persistence;
namespace Topstar.Web.Pages.Koc;
[Authorize(Roles="Koc,Admin")]
[ResponseCache(NoStore=true,Location=ResponseCacheLocation.None)]
[RequestSizeLimit(12*1024*1024)]
public sealed class TaxProfileModel(TopstarDbContext db,IDataProtectionProvider protection,IWebHostEnvironment env,ILogger<TaxProfileModel> logger):PageModel
{
 [BindProperty(SupportsGet=true)]public Guid? Id{get;set;}
 [BindProperty]public string TaxNumber{get;set;}="";
 [BindProperty]public string IdentityNumber{get;set;}="";
 [BindProperty]public Guid Version{get;set;}
 [BindProperty]public IFormFile? Front{get;set;}
 [BindProperty]public IFormFile? Back{get;set;}
 [TempData]public string? Message{get;set;}
 [BindProperty]public string Treatment{get;set;}="Withholding";
 [BindProperty]public string TreatmentReason{get;set;}="";
 public bool TaxSetupReady{get;private set;}
 public string Name{get;private set;}="";
 public bool HasFront{get;private set;}public bool HasBack{get;private set;}
 public bool Ready{get;private set;}=true;
 public bool IsAdmin=>User.IsInRole("Admin");
 private Guid Actor=>Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 private IDataProtector Protector=>protection.CreateProtector("Topstar.KocTaxProfile.v1");
 private string Folder=>Path.Combine(env.ContentRootPath,"App_Data","private-koc-tax");
 public sealed class TaxRow{public Guid KocProfileId{get;set;}public string TaxNumberCiphertext{get;set;}="";public string IdentityNumberCiphertext{get;set;}="";public string FrontFile{get;set;}="";public string BackFile{get;set;}="";public Guid Version{get;set;}public DateTime UpdatedAtUtc{get;set;}public Guid UpdatedByUserId{get;set;}}
 private async Task<bool> ResolveAsync(CancellationToken ct){
  var query=db.KocProfiles.AsNoTracking();
  var profile=IsAdmin?await query.FirstOrDefaultAsync(p=>p.Id==Id,ct):await query.FirstOrDefaultAsync(p=>p.UserId==Actor,ct);
  if(profile is null)return false;Id=profile.Id;Name=profile.DisplayName;
  Ready=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.KocTaxProfiles')").AnyAsync(x=>x>0,ct);return true;
 }
 private Task<TaxRow?> ReadAsync(CancellationToken ct)=>db.Database.SqlQuery<TaxRow>($"SELECT * FROM dbo.KocTaxProfiles WHERE KocProfileId={Id}").SingleOrDefaultAsync(ct);
 private void Headers(){Response.Headers.CacheControl="no-store, private";Response.Headers["X-Content-Type-Options"]="nosniff";Response.Headers["Referrer-Policy"]="no-referrer";}
 public async Task<IActionResult> OnGetAsync(CancellationToken ct){
 if(IsAdmin){TaxSetupReady=await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.KocTaxTreatments')").AnyAsync(x=>x>0,ct);if(TaxSetupReady&&Id.HasValue){var treatment=await db.Database.SqlQuery<Topstar.Infrastructure.Finance.KocTaxTreatment>($"SELECT * FROM dbo.KocTaxTreatments WHERE KocProfileId={Id}").SingleOrDefaultAsync(ct);if(treatment is not null){Treatment=treatment.Treatment;TreatmentReason=treatment.Reason;}}}
 Headers();if(!await ResolveAsync(ct))return NotFound();if(!Ready)return Page();var row=await ReadAsync(ct);if(row is not null){try{TaxNumber=Protector.Unprotect(row.TaxNumberCiphertext);IdentityNumber=Protector.Unprotect(row.IdentityNumberCiphertext);}catch(CryptographicException){logger.LogError("Cannot decrypt tax identity for {KocId}",Id);ModelState.AddModelError("", "Không thể đọc thông tin đã lưu. Vui lòng liên hệ quản trị viên.");return Page();}Version=row.Version;HasFront=true;HasBack=true;if(IsAdmin){db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="KocTaxProfile",EntityId=Id.ToString()!,ActionName="ViewPrivateTaxProfile",CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);}}return Page();}
 private async Task<string?> SaveFileAsync(IFormFile? file,string field,CancellationToken ct){
  if(file is null)return null;
  if(file.Length<=0||file.Length>5*1024*1024){ModelState.AddModelError(field,"Chọn ảnh JPG, PNG hoặc WEBP không quá 5 MB.");return null;}
  await using var stream=file.OpenReadStream();using var memory=new MemoryStream();await stream.CopyToAsync(memory,ct);var bytes=memory.ToArray();
  var jpeg=bytes.Length>3&&bytes[0]==255&&bytes[1]==216&&bytes[2]==255;
  var png=bytes.Length>8&&bytes.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10});
  var webp=bytes.Length>12&&System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(bytes,8,4)=="WEBP";
  if(!jpeg&&!png&&!webp){ModelState.AddModelError(field,"Tệp phải là ảnh JPG, PNG hoặc WEBP hợp lệ.");return null;}
  Directory.CreateDirectory(Folder);var name=Guid.NewGuid().ToString("N")+(jpeg?".jpg":png?".png":".webp");await System.IO.File.WriteAllBytesAsync(Path.Combine(Folder,name),Protector.Protect(bytes),ct);return name;
 }
 private void DeleteFile(string? name){if(name is not null&&Regex.IsMatch(name,@"^[a-f0-9]{32}\.(jpg|png|webp)$")){var path=Path.Combine(Folder,name);try{if(System.IO.File.Exists(path))System.IO.File.Delete(path);}catch(IOException ex){logger.LogWarning(ex,"Cannot remove superseded private image");}}}
 public async Task<IActionResult> OnPostAsync(CancellationToken ct){
  Headers();if(IsAdmin)return Forbid();if(!await ResolveAsync(ct))return NotFound();if(!Ready)return Page();var old=await ReadAsync(ct);HasFront=old is not null;HasBack=old is not null;
  TaxNumber=(TaxNumber??"").Trim();IdentityNumber=(IdentityNumber??"").Trim();
  if(string.IsNullOrWhiteSpace(TaxNumber))ModelState.AddModelError(nameof(TaxNumber),"Vui lòng nhập mã số thuế.");
  if(string.IsNullOrWhiteSpace(IdentityNumber))ModelState.AddModelError(nameof(IdentityNumber),"Vui lòng nhập số CCCD.");
  if(old is null&&Front is null)ModelState.AddModelError(nameof(Front),"Vui lòng chọn ảnh CCCD mặt trước.");
  if(old is null&&Back is null)ModelState.AddModelError(nameof(Back),"Vui lòng chọn ảnh CCCD mặt sau.");
  if(!ModelState.IsValid)return Page();
  string? front=null,back=null;var committed=false;
  try{
   front=await SaveFileAsync(Front,nameof(Front),ct);back=await SaveFileAsync(Back,nameof(Back),ct);if(!ModelState.IsValid)return Page();
   await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
   var current=await db.Database.SqlQuery<TaxRow>($"SELECT * FROM dbo.KocTaxProfiles WITH(UPDLOCK,HOLDLOCK) WHERE KocProfileId={Id}").SingleOrDefaultAsync(ct);
   if((current?.Version??Guid.Empty)!=Version){ModelState.AddModelError("","Thông tin vừa được cập nhật ở phiên khác. Vui lòng tải lại trang.");return Page();}
   var tax=Protector.Protect(TaxNumber);var identity=Protector.Protect(IdentityNumber);var f=front??current!.FrontFile;var b=back??current!.BackFile;var next=Guid.NewGuid();var now=DateTime.UtcNow;
   if(current is null)await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.KocTaxProfiles(KocProfileId,TaxNumberCiphertext,IdentityNumberCiphertext,FrontFile,BackFile,Version,UpdatedAtUtc,UpdatedByUserId) VALUES({Id},{tax},{identity},{f},{b},{next},{now},{Actor})",ct);
   else await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.KocTaxProfiles SET TaxNumberCiphertext={tax},IdentityNumberCiphertext={identity},FrontFile={f},BackFile={b},Version={next},UpdatedAtUtc={now},UpdatedByUserId={Actor} WHERE KocProfileId={Id}",ct);
   if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.KocTaxTreatments')").AnyAsync(x=>x>0,ct))await db.Database.ExecuteSqlInterpolatedAsync($"DELETE FROM dbo.KocTaxTreatments WHERE KocProfileId={Id}",ct);
   db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="KocTaxProfile",EntityId=Id.ToString()!,ActionName="UpdatePrivateTaxProfile",CreatedAtUtc=now});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);committed=true;
   if(current is not null){if(front is not null)DeleteFile(current.FrontFile);if(back is not null)DeleteFile(current.BackFile);}
   Message="Đã lưu thông tin thuế và CCCD.";return RedirectToPage();
  }catch(Exception ex)when(ex is not OperationCanceledException){logger.LogError(ex,"Cannot save KOC tax profile {KocId}",Id);ModelState.AddModelError("","Chưa lưu được thông tin. Vui lòng thử lại.");return Page();}
  finally{if(!committed){DeleteFile(front);DeleteFile(back);}}
 }
 public async Task<IActionResult> OnPostTreatmentAsync(CancellationToken ct){
  if(!IsAdmin)return Forbid();if(!await ResolveAsync(ct)||!Ready)return NotFound();
  if(Treatment is not ("Withholding" or "Business" or "Exempt")||string.IsNullOrWhiteSpace(TreatmentReason)){Message="Vui lòng chọn phân loại và nhập căn cứ xác minh.";return RedirectToPage(new{id=Id});}
  if(await ReadAsync(ct) is null){Message="KOC chưa khai báo hồ sơ thuế và CCCD.";return RedirectToPage(new{id=Id});}
  var now=DateTime.UtcNow;var reason=TreatmentReason.Trim();if(reason.Length>1000)reason=reason[..1000];
  await using var tx=await db.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable,ct);
  await db.Database.ExecuteSqlInterpolatedAsync($"IF EXISTS(SELECT 1 FROM dbo.KocTaxTreatments WITH(UPDLOCK,HOLDLOCK) WHERE KocProfileId={Id}) UPDATE dbo.KocTaxTreatments SET Treatment={Treatment},Reason={reason},UpdatedAtUtc={now},ActorUserId={Actor} WHERE KocProfileId={Id} ELSE INSERT dbo.KocTaxTreatments(KocProfileId,Treatment,Reason,UpdatedAtUtc,ActorUserId) VALUES({Id},{Treatment},{reason},{now},{Actor})",ct);
  db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="KocTaxTreatment",EntityId=Id.ToString()!,ActionName="VerifyTaxTreatment",PayloadJson=System.Text.Json.JsonSerializer.Serialize(new{Treatment,reason}),CreatedAtUtc=now});await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);Message="Đã lưu phân loại thuế KOC.";return RedirectToPage(new{id=Id});
 }
 public async Task<IActionResult> OnGetImageAsync(string side,CancellationToken ct){Headers();if(!await ResolveAsync(ct)||!Ready)return NotFound();var row=await ReadAsync(ct);if(row is null||side is not ("front" or "back"))return NotFound();var name=side=="front"?row.FrontFile:row.BackFile;if(!Regex.IsMatch(name,@"^[a-f0-9]{32}\.(jpg|png|webp)$"))return NotFound();var path=Path.Combine(Folder,name);if(!System.IO.File.Exists(path))return NotFound();try{var bytes=Protector.Unprotect(await System.IO.File.ReadAllBytesAsync(path,ct));db.AuditLogs.Add(new(){ActorUserId=Actor,EntityName="KocTaxProfile",EntityId=Id.ToString()!,ActionName="ViewPrivateIdentityImage",CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync(ct);return File(bytes,name.EndsWith(".jpg")?"image/jpeg":name.EndsWith(".png")?"image/png":"image/webp");}catch(CryptographicException){return NotFound();}}
}

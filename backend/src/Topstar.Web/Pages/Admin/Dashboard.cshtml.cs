using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Topstar.Application.Dashboard;
using Topstar.Application.Operations;
using Topstar.Application.SystemSettings;

namespace Topstar.Web.Pages.Admin;

[Authorize(Roles = "Admin")]
public sealed class DashboardModel(
    IDashboardDataService dashboardDataService,
    IOperationsAdminService operationsAdminService,
    ISystemSettingsAdminService systemSettingsAdminService, Topstar.Infrastructure.Persistence.TopstarDbContext db) : PageModel
{
    public OperationsAdminDashboard Operations { get; private set; } = new([],[],[],[],[],[],[],[],[]);
    public IReadOnlyList<EmailAccountItem> EmailAccounts { get; private set; } = [];
    public IReadOnlyList<SocialLoginConfigurationItem> SocialLoginConfigurations { get; private set; } = [];

    public AdminDashboardData Data { get; private set; } =
        new(0, 0, 0, 0m, 0, 0, 0, [], []);

    public FinanceOverview Finance{get;private set;}=new();
    private async Task<FinanceOverview> LoadFinanceAsync(CancellationToken ct){
        var data=new FinanceOverview();
        data.PackagePending=await db.BrandPackagePurchases.CountAsync(p=>p.Status=="Pending",ct);
        data.PackageCancellations=await db.BrandPackagePurchases.CountAsync(p=>p.Status=="CancelRequested",ct);
        data.FundingPending=await db.CampaignFundings.CountAsync(f=>f.Status=="Pending",ct);
        var packages=await (from p in db.BrandPackagePurchases.AsNoTracking() join b in db.BrandProfiles on p.BrandProfileId equals b.Id where p.Status=="Pending"||p.Status=="CancelRequested" orderby p.UpdatedAtUtc??p.PurchasedAtUtc descending select new{b.CompanyName,p.OrderNumber,p.Status,p.AmountSnapshot,Date=p.UpdatedAtUtc??p.PurchasedAtUtc}).Take(6).ToListAsync(ct);
        data.Recent.AddRange(packages.Select(p=>new FinanceRequest(p.CompanyName,p.Status=="CancelRequested"?"Hủy gói":"Thanh toán gói",p.OrderNumber,p.AmountSnapshot,p.Date,"/Admin/Finance/Billing?list.adminPackageList.status="+p.Status,p.Status=="CancelRequested"?"fa-rotate-left":"fa-receipt")));
        var fundings=await (from f in db.CampaignFundings.AsNoTracking() join c in db.Campaigns on f.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id where f.Status=="Pending" orderby f.SubmittedAtUtc??f.CreatedAtUtc descending select new{b.CompanyName,f.OrderNumber,f.TotalPayableSnapshot,Date=f.SubmittedAtUtc??f.CreatedAtUtc}).Take(6).ToListAsync(ct);
        data.Recent.AddRange(fundings.Select(f=>new FinanceRequest(f.CompanyName,"Ký quỹ",f.OrderNumber??"",f.TotalPayableSnapshot,f.Date,"/Admin/Finance/CampaignFunding?queue=pending","fa-shield-halved")));
        if(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM sys.tables WHERE object_id=OBJECT_ID(N'dbo.BrandBalanceWithdrawals')").AnyAsync(x=>x>0,ct)){
            var requests=db.Database.SqlQuery<Topstar.Infrastructure.Finance.BrandWithdrawal>($"SELECT * FROM dbo.BrandBalanceWithdrawals").Where(w=>w.Status=="Pending"||w.Status=="Approved");
            data.Withdrawals=await requests.CountAsync(ct);
            var withdrawals=await requests.OrderByDescending(w=>w.CreatedAtUtc).Take(6).ToListAsync(ct);var ids=withdrawals.Select(w=>w.BrandProfileId).ToList();var brands=await db.BrandProfiles.Where(b=>ids.Contains(b.Id)).ToDictionaryAsync(b=>b.Id,b=>b.CompanyName,ct);
            data.Recent.AddRange(withdrawals.Select(w=>new FinanceRequest(brands.GetValueOrDefault(w.BrandProfileId,"Brand"),w.Status=="Pending"?"Chờ duyệt rút tiền":"Chờ chuyển tiền",w.Id.ToString("N")[..8].ToUpperInvariant(),w.Amount,w.CreatedAtUtc,"/Admin/Finance/BrandBalances?tab=withdrawals","fa-money-bill-transfer")));
        }
        data.Recent=data.Recent.OrderByDescending(r=>r.CreatedAtUtc).Take(6).ToList();return data;
    }
    public sealed class FinanceOverview{public int PackagePending{get;set;}public int FundingPending{get;set;}public int PackageCancellations{get;set;}public int Withdrawals{get;set;}public List<FinanceRequest> Recent{get;set;}=[];}
    public sealed record FinanceRequest(string Brand,string Label,string Reference,decimal Amount,DateTime CreatedAtUtc,string Url,string Icon);
    public async Task OnGetAsync()
    {
        Data = await dashboardDataService.GetAdminDashboardAsync(HttpContext.RequestAborted);
        Finance=await LoadFinanceAsync(HttpContext.RequestAborted);
        Operations=await operationsAdminService.GetDashboardAsync(HttpContext.RequestAborted);
        EmailAccounts = await systemSettingsAdminService.GetEmailAccountsAsync(HttpContext.RequestAborted);
        SocialLoginConfigurations = await systemSettingsAdminService.GetSocialLoginConfigurationsAsync(HttpContext.RequestAborted);
    }

}
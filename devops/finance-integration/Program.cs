// Isolated LocalDB only. Never reads application connection strings or gateway credentials.
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Topstar.Infrastructure;
using System.Data;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Primitives;
using Topstar.Application.Billing;
using Topstar.Infrastructure.Campaigns;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Finance;
using Topstar.Infrastructure.Persistence;
using Topstar.Web.Services.Payments;

var database="TopstarFinanceTest_"+Guid.NewGuid().ToString("N");
var connection=$"Server=(localdb)\\MSSQLLocalDB;Database={database};Integrated Security=true;TrustServerCertificate=true;Connect Timeout=20";
var options=new DbContextOptionsBuilder<TopstarDbContext>().UseSqlServer(connection).Options;
TopstarDbContext Db()=>new(options);
var root=Directory.GetCurrentDirectory();
while(!Directory.Exists(Path.Combine(root,"database","sql")))
    root=Directory.GetParent(root)?.FullName??throw new Exception("Repository root not found.");
var taxTestRoot=Path.Combine(Path.GetTempPath(),"TopstarTaxTest_"+Guid.NewGuid().ToString("N"));
var count=0;
void Check(bool ok,string name){if(!ok)throw new Exception("FAILED: "+name);System.Console.WriteLine("PASS: "+name);count++;}
async Task Script(string file)
{
    await using var db=Db();
    foreach(var batch in Regex.Split(await File.ReadAllTextAsync(Path.Combine(root,"database","sql",file)),@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
        if(!string.IsNullOrWhiteSpace(batch))await db.Database.ExecuteSqlRawAsync(batch);
}
async Task<BrandBalanceResult> Balance(Func<BrandBalanceService,Task<BrandBalanceResult>> action)
{
    await using var db=Db();
    try{return await action(new(db));}
    catch(SqlException ex)when(ex.Number==1205){return new(false,"Concurrent request rolled back by SQL Server.");}
}
async Task<BrandBalanceRow> Account(Guid user){await using var db=Db();return await new BrandBalanceService(db).GetAsync(user,default);}
async Task<CampaignFundingEntity> Fund(Guid id){await using var db=Db();return await db.CampaignFundings.AsNoTracking().SingleAsync(f=>f.Id==id);}
var brandUser=Guid.NewGuid();var admin=Guid.NewGuid();var admin2=Guid.NewGuid();var stranger=Guid.NewGuid();var brand=Guid.NewGuid();
async Task SeedBalance(decimal amount)
{
    await using var db=Db();await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);
    var account=await BrandBalanceService.LockAsync(db,brand,default);
    await BrandBalanceService.WriteAsync(db,account,amount,0,"TestSeed",Guid.NewGuid(),Guid.NewGuid(),"Isolated test balance",admin,default);
    await tx.CommitAsync();
}
async Task<(Guid Campaign,Guid Funding)> SeedFunding(decimal budget,string method="BankTransfer",string status="Pending")
{
    await using var db=Db();var c=Guid.NewGuid();var f=Guid.NewGuid();
    db.Campaigns.Add(new(){Id=c,BrandProfileId=brand,Title="Isolated test campaign",Status="Published",BudgetAmount=budget,DesiredKocCount=3,IsVisible=true,CreatedAtUtc=DateTime.UtcNow});
    db.CampaignFundings.Add(new(){Id=f,CampaignId=c,Status=status,CampaignBudgetSnapshot=budget,TotalPayableSnapshot=budget,PaymentMethod=method,OrderNumber="TEST-"+f.ToString("N"),CreatedAtUtc=DateTime.UtcNow});
    await db.SaveChangesAsync();return(c,f);
}
try
{
    await using(var db=Db())
    {
        // Production uses reviewed SQL scripts. For this disposable fixture only,
        // remove EF convention cascades that are absent in the deployed schema.
        await using(var master=new SqlConnection(connection.Replace("Database="+database,"Database=master")))
        { await master.OpenAsync();await using var command=master.CreateCommand();command.CommandText=$"CREATE DATABASE [{database}]";await command.ExecuteNonQueryAsync(); }
        var schema=db.Database.GenerateCreateScript().Replace(" ON DELETE CASCADE", "");
        foreach(var batch in Regex.Split(schema,@"^\s*GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
            if(!string.IsNullOrWhiteSpace(batch))await db.Database.ExecuteSqlRawAsync(batch);
        foreach(var u in new[]{brandUser,admin,admin2,stranger})db.Users.Add(new(){Id=u,Email=u+"@example.invalid",Status="Active",CreatedAtUtc=DateTime.UtcNow});
        db.BrandProfiles.Add(new(){Id=brand,UserId=brandUser,CompanyName="Isolated test Brand",Status="Approved",RefundBankName="TEST",RefundAccountNumber="123456789",RefundAccountHolder="TEST BRAND",CreatedAtUtc=DateTime.UtcNow});
        await db.SaveChangesAsync();
    }
    await Script("29_create_campaign_lifecycle_histories.sql");
    await Script("36_add_campaign_dispute_cancellation.sql");
    await Script("165_add_koc_tax_profiles.sql");
    await Script("167_add_koc_income_tax.sql");await Script("167_add_koc_income_tax.sql");
    await Script("168_guard_koc_tax_hold.sql");await Script("168_guard_koc_tax_hold.sql");
    await Script("169_campaign_creation_checkout.sql");await Script("169_campaign_creation_checkout.sql");
    await Script("166_add_brand_tax_policies.sql");await Script("166_add_brand_tax_policies.sql");
    await Script("159_add_vnpay_payment_configuration.sql");
    await Script("160_add_vnpay_callback_logs.sql");
    await Script("147_enable_prepaid_campaign_escrow.sql");
    await Script("161_add_brand_balance.sql");await Script("161_add_brand_balance.sql");
    await Script("162_add_campaign_pause_status.sql");await Script("162_add_campaign_pause_status.sql");
    await Script("163_enable_package_balance_payment.sql");await Script("163_enable_package_balance_payment.sql");
    Check(true,"deployment scripts 159–162 execute; 161/162 rerun safely");
    await SeedBalance(500000);
    var key1=Guid.NewGuid();var key2=Guid.NewGuid();
    var parallel=await Task.WhenAll(Balance(s=>s.RequestWithdrawalAsync(brandUser,350000,key1,default)),Balance(s=>s.RequestWithdrawalAsync(brandUser,350000,key2,default)));
    Check(parallel.Count(r=>r.Success)==1,"concurrent withdrawals cannot overspend");
    var account=await Account(brandUser);Check(account.Available==150000&&account.Reserved==350000,"withdrawal reserves gross amount atomically");
    Guid withdrawal;Guid winningKey;
    await using(var db=Db()){var row=(await new BrandBalanceService(db).WithdrawalsAsync(brand,default)).Single();withdrawal=row.Id;winningKey=row.RequestKey;}
    Check((await Balance(s=>s.RequestWithdrawalAsync(brandUser,350000,winningKey,default))).Success,"replayed withdrawal is idempotent");
    Check(!(await Balance(s=>s.ProcessWithdrawalAsync(stranger,withdrawal,"Cancel",null,null,null,false,default))).Success,"another Brand cannot cancel withdrawal");
    Check((await Balance(s=>s.ProcessWithdrawalAsync(brandUser,withdrawal,"Cancel",null,null,null,false,default))).Success,"owner cancels pending request");
    Check(!(await Balance(s=>s.ProcessWithdrawalAsync(brandUser,withdrawal,"Cancel",null,null,null,false,default))).Success,"repeated cancellation cannot refund twice");
    account=await Account(brandUser);Check(account.Available==500000&&account.Reserved==0,"cancellation releases reserved amount exactly once");
    Check(!(await Balance(s=>s.RequestWithdrawalAsync(brandUser,500001,Guid.NewGuid(),default))).Success,"tampered withdrawal amount rejected");
    Check((await Balance(s=>s.SavePolicyAsync(admin,true,50000,1000,300000,3,default))).Success,"admin policy saved");
    Check((await Balance(s=>s.RequestWithdrawalAsync(brandUser,400000,Guid.NewGuid(),default))).Success,"withdrawal snapshots fee and dual approval");
    await using(var db=Db())withdrawal=(await new BrandBalanceService(db).WithdrawalsAsync(brand,default)).Single(w=>w.Status=="Pending").Id;
    await Balance(s=>s.ProcessWithdrawalAsync(admin,withdrawal,"Approve",null,null,null,true,default));
    Check(!(await Balance(s=>s.ProcessWithdrawalAsync(admin,withdrawal,"Approve",null,null,null,true,default))).Success,"one admin cannot satisfy two approvals");
    Check(!(await Balance(s=>s.ProcessWithdrawalAsync(admin,withdrawal,"Paid","TEST-PAID","/uploads/campaign-funding-evidence/test.png",null,true,default))).Success,"cannot pay before second approval");
    await Balance(s=>s.ProcessWithdrawalAsync(admin2,withdrawal,"Approve",null,null,null,true,default));
    Check((await Balance(s=>s.ProcessWithdrawalAsync(admin,withdrawal,"Paid","TEST-PAID","/uploads/campaign-funding-evidence/test.png",null,true,default))).Success,"approved withdrawal paid with evidence");
    Check(!(await Balance(s=>s.ProcessWithdrawalAsync(admin,withdrawal,"Paid","TEST-PAID","/uploads/campaign-funding-evidence/test.png",null,true,default))).Success,"cannot debit paid withdrawal twice");
    account=await Account(brandUser);Check(account.Available==100000&&account.Reserved==0,"fee deducted from payout rather than extra wallet debit");
    await SeedBalance(400000);
    var mixed=await SeedFunding(500000,"VnPay");var paymentOp=Guid.NewGuid();
    Check((await Balance(s=>s.PayFundingAsync(brandUser,mixed.Funding,200000,paymentOp,default))).Success,"partial balance payment accepted");
    await Balance(s=>s.PayFundingAsync(brandUser,mixed.Funding,200000,paymentOp,default));
    Check((await Fund(mixed.Funding)).BalanceAppliedAmount==200000,"balance payment replay does not debit again");
    var protection=new EphemeralDataProtectionProvider();const string secret="test-secret-not-live";
    await using(var db=Db())
    {
        db.PaymentMethodConfigurations.Add(new(){Id=Guid.NewGuid(),VnPayEnabled=true,VnPayTmnCode="TEST",VnPayHashSecretCiphertext=protection.CreateProtector("Topstar.VnPay.HashSecret.v1").Protect(secret),VnPayEnvironment="Test",VnPayBaseUrl="https://sandbox.vnpayment.vn/paymentv2/vpcpay.html",VnPayReturnUrl="http://localhost/Brand/VnPayReturn",VnPayIpnUrl="http://localhost/api/payments/vnpay/ipn",UpdatedAtUtc=DateTime.UtcNow});
        await db.SaveChangesAsync();
    }
    var services=new ServiceCollection().AddLogging().AddDbContext<TopstarDbContext>(o=>o.UseSqlServer(connection)).BuildServiceProvider();
    var journal=new VnPayCallbackJournal(services.GetRequiredService<IServiceScopeFactory>(),new HttpContextAccessor(),NullLogger<VnPayCallbackJournal>.Instance);
    await using(var db=Db())
    {
        var created=await new VnPayService(db,protection,journal).CreateAsync(brandUser,mixed.Funding,true,"127.0.0.1",default);
        Check(created.Success&&created.RedirectUrl!.Contains("vnp_Amount=30000000"),"online checkout uses remainder only");
    }
    Check(!(await Balance(s=>s.PayFundingAsync(brandUser,mixed.Funding,100000,Guid.NewGuid(),default))).Success,"checkout freezes remainder against later balance changes");
    await using(var db=Db())Check(!(await new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance).CancelPendingAsync(brandUser,mixed.Funding,"Attempt cancellation during checkout")).Success,"cannot cancel an online payment awaiting reconciliation");
    Dictionary<string,StringValues> Callback(Guid funding,decimal amount,string txn,bool valid=true)
    {
        var values=new Dictionary<string,string>{{"vnp_TmnCode","TEST"},{"vnp_TxnRef","F"+funding.ToString("N")},{"vnp_Amount",(amount*100).ToString(System.Globalization.CultureInfo.InvariantCulture)},{"vnp_TransactionNo",txn},{"vnp_ResponseCode","00"},{"vnp_TransactionStatus","00"}};
        values["vnp_SecureHash"]=valid?VnPayProtocol.Sign(values,secret):new string('0',128);
        return values.ToDictionary(p=>p.Key,p=>new StringValues(p.Value));
    }
    async Task<VnPayResult> Process(Dictionary<string,StringValues> payload,bool ipn)
    {await using var db=Db();return await new VnPayService(db,protection,journal).ProcessAsync(payload,ipn,default);}
    Check(!(await Process(Callback(mixed.Funding,500000,"123"),false)).Success,"signed callback with wrong amount rejected");
    Check(!(await Process(Callback(mixed.Funding,300000,"123",false),false)).Success,"invalid callback signature rejected");
    Check((await Process(Callback(mixed.Funding,300000,"123"),false)).Success,"verified Return completes mixed payment");
    Check((await Process(Callback(mixed.Funding,300000,"123"),true)).Success,"repeated IPN acknowledged without second credit");
    await using(var db=Db())Check(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.VnPayReceipts WHERE TransactionNo=N'123'").SingleAsync()==1,"one receipt per gateway transaction");
    var cancel=await SeedFunding(200000);
    await Balance(s=>s.PayFundingAsync(brandUser,cancel.Funding,100000,Guid.NewGuid(),default));
    var beforeCancel=await Account(brandUser);
    await using(var db=Db())Check((await new CampaignLifecycleService(db).ChangeAsync(brandUser,cancel.Campaign,"Cancel","Test pre-work cancellation")).Success,"pre-work cancellation closes pending funding");
    account=await Account(brandUser);Check(account.Available==beforeCancel.Available+100000,"cancel campaign restores only balance used on unpaid request");
    await using(var db=Db())Check((await new CampaignLifecycleService(db).ChangeAsync(brandUser,mixed.Campaign,"Pause","Test pause")).Changed,"funded pre-work campaign can pause");
    Check(!(await Balance(s=>s.PayFundingAsync(brandUser,mixed.Funding,1,Guid.NewGuid(),default))).Success,"paid funding cannot charge balance again");
    await using(var db=Db())Check((await new CampaignLifecycleService(db).ChangeAsync(brandUser,mixed.Campaign,"Resume","Test resume")).Success,"resume restores operational status");
    await using(var db=Db())Check((await new CampaignLifecycleService(db).ChangeAsync(brandUser,mixed.Campaign,"Cancel","Test paid cancellation")).Success,"paid cancellation preserves escrow for review");
    var beforeRefund=await Account(brandUser);
    var refunds=await Task.WhenAll(Balance(s=>s.CreditRefundAsync(admin,mixed.Funding,250000,Guid.NewGuid(),default)),Balance(s=>s.CreditRefundAsync(admin,mixed.Funding,250000,Guid.NewGuid(),default)));
    Check(refunds.All(r=>r.Success),"two distinct valid refund approvals can consume separate portions");
    account=await Account(brandUser);Check(account.Available==beforeRefund.Available+500000,"cancelled unallocated escrow returned without exceeding budget");
    Check(!(await Balance(s=>s.CreditRefundAsync(admin,mixed.Funding,1,Guid.NewGuid(),default))).Success,"refund cannot exceed already returned escrow");
    // A withdrawal and payment compete for the same available balance.
    var competing=await SeedFunding(1000000);
    var available=(await Account(brandUser)).Available;
    var competition=await Task.WhenAll(Balance(s=>s.RequestWithdrawalAsync(brandUser,available,Guid.NewGuid(),default)),Balance(s=>s.PayFundingAsync(brandUser,competing.Funding,available,Guid.NewGuid(),default)));
    Check(competition.Count(r=>r.Success)==1,"withdrawal and escrow payment cannot spend the same balance");
    account=await Account(brandUser);Check(account.Available==0,"competing operations never create negative available balance");
    var allocated=await SeedFunding(300000,"BankTransfer","Paid");
    var kocUser=Guid.NewGuid();var koc=Guid.NewGuid();var participation=Guid.NewGuid();var allocation=Guid.NewGuid();
    await using(var db=Db())
    {
        db.Users.Add(new(){Id=kocUser,Email="koc-test@example.invalid",Status="Active",CreatedAtUtc=DateTime.UtcNow});
        db.KocProfiles.Add(new(){Id=koc,UserId=kocUser,DisplayName="Test KOC",Status="Approved",CreatedAtUtc=DateTime.UtcNow});
        db.Participations.Add(new(){Id=participation,CampaignId=allocated.Campaign,KocProfileId=koc,Status="Confirmed",ConfirmedAtUtc=DateTime.UtcNow,CreatedAtUtc=DateTime.UtcNow});
        var f=await db.CampaignFundings.SingleAsync(f=>f.Id==allocated.Funding);f.AllocationMode="Prepaid";
        await db.SaveChangesAsync();
        db.CampaignFundingAllocations.Add(new(){Id=allocation,FundingId=f.Id,ParticipationId=participation,Amount=300000,Status="Active",CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();
    }
    await using(var db=Db())Check((await new CampaignLifecycleService(db).ChangeAsync(brandUser,allocated.Campaign,"Pause","Test confirmed KOC pause")).Success,"pause allowed with confirmed KOC before work");
    await using(var db=Db())Check((await db.Participations.SingleAsync(p=>p.Id==participation)).Status=="Confirmed","pause preserves KOC acceptance");
    await using(var db=Db())Check(!(await new CampaignWorkspaceService(db).OpenWorkAsync(brandUser,allocated.Campaign,participation,false)).Success,"paused campaign cannot open KOC work");
    await using(var db=Db())await new CampaignLifecycleService(db).ChangeAsync(brandUser,allocated.Campaign,"Resume","Test resume with KOC");
    await using(var db=Db()){var p=await db.Participations.SingleAsync(p=>p.Id==participation);p.WorkOpenedAtUtc=DateTime.UtcNow;await db.SaveChangesAsync();}
    await using(var db=Db())Check(!(await new CampaignLifecycleService(db).ChangeAsync(brandUser,allocated.Campaign,"Cancel","Cancel after work starts")).Success,"work already opened prevents self-service cancellation");
    await using(var db=Db()){var p=await db.Participations.SingleAsync(p=>p.Id==participation);p.WorkOpenedAtUtc=null;await db.SaveChangesAsync();}
    await using(var db=Db())Check((await new CampaignLifecycleService(db).ChangeAsync(brandUser,allocated.Campaign,"Cancel","Test cancellation before work")).Success,"pre-work cancellation with accepted KOC awaits settlement");
    Check(!(await Balance(s=>s.CreditRefundAsync(admin,allocated.Funding,300000,Guid.NewGuid(),default))).Success,"accepted KOC obligation cannot be refunded before settlement");
    await using(var db=Db())Check((await new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance).SettleAllocationAsync(admin,allocation,100000,"Test agreed compensation for KOC")).Success,"admin can settle KOC entitlement after pre-work cancellation");
    var beforeCredit=await Account(brandUser);
    Check((await Balance(s=>s.CreditRefundAsync(admin,allocated.Funding,200000,Guid.NewGuid(),default))).Success,"only approved refund portion becomes Brand balance");
    account=await Account(brandUser);Check(account.Available==beforeCredit.Available+200000,"KOC entitlement excluded from refund");
    await using(var db=Db())Check(await db.WalletTransactions.Where(w=>w.ParticipationId==participation).SumAsync(w=>w.Amount)==100000,"KOC compensation recorded independently in wallet");
    // A bank refund racing a wallet credit must share one refund ceiling.
    var race=await SeedFunding(300000,"BankTransfer","Paid");var racePart=Guid.NewGuid();var raceAllocation=Guid.NewGuid();
    await using(var db=Db())
    {
        var c=await db.Campaigns.SingleAsync(c=>c.Id==race.Campaign);c.Status="Cancelled";c.CancelledAtUtc=DateTime.UtcNow;c.CancelledByUserId=brandUser;c.CancellationReason="Isolated refund race cancellation";
        var f=await db.CampaignFundings.SingleAsync(f=>f.Id==race.Funding);f.AllocationMode="Prepaid";
        db.Participations.Add(new(){Id=racePart,CampaignId=c.Id,KocProfileId=koc,Status="Cancelled",ConfirmedAtUtc=DateTime.UtcNow,CreatedAtUtc=DateTime.UtcNow});
        await db.SaveChangesAsync();
        db.CampaignFundingAllocations.Add(new(){Id=raceAllocation,FundingId=f.Id,ParticipationId=racePart,Amount=300000,Status="Rejected",RefundedAmount=300000,ReleasedAtUtc=DateTime.UtcNow,CreatedAtUtc=DateTime.UtcNow});
        db.CampaignFundingEvidences.Add(new(){Id=Guid.NewGuid(),FundingId=f.Id,EvidenceType="Refund",Reference="BANK-RACE",Amount=200000,FileUrl="/uploads/campaign-funding-evidence/race.png",ActorUserId=admin,CreatedAtUtc=DateTime.UtcNow});
        db.CampaignFeePolicies.Add(new(){Id=Guid.NewGuid(),Code="DEFAULT",IsActive=true,EffectiveAtUtc=DateTime.UtcNow.AddDays(-1),DualApprovalThreshold=50000000,CreatedAtUtc=DateTime.UtcNow});
        await db.SaveChangesAsync();
    }
    async Task<CampaignFundingResult> BankRefund()
    {
        await using var db=Db();
        try{return await new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance).ConfirmRefundAsync(admin,race.Funding,200000,"BANK-RACE","Isolated refund concurrency check");}
        catch(SqlException ex)when(ex.Number==1205){return new(false,"Rolled back concurrency victim.");}
    }
    var walletRefund=Balance(s=>s.CreditRefundAsync(admin,race.Funding,200000,Guid.NewGuid(),default));var bankRefund=BankRefund();
    await Task.WhenAll(walletRefund,bankRefund);
    Check(new[]{walletRefund.Result.Success,bankRefund.Result.Success}.Count(x=>x)==1,"bank refund and wallet credit cannot refund same escrow twice");
    Check((await Fund(race.Funding)).RefundedAmount==200000,"one shared returned amount across both refund routes");
    var dualRefund=await SeedFunding(400000,"BankTransfer","Paid");var dualOperation=Guid.NewGuid();
    await using(var db=Db()){var c=await db.Campaigns.SingleAsync(c=>c.Id==dualRefund.Campaign);c.Status="Cancelled";c.CancelledAtUtc=DateTime.UtcNow;c.CancelledByUserId=brandUser;c.CancellationReason="Isolated dual approval cancellation";await db.SaveChangesAsync();}
    var beforeDual=await Account(brandUser);
    Check((await Balance(s=>s.CreditRefundAsync(admin,dualRefund.Funding,400000,dualOperation,default))).Success,"large wallet credit records first approval");
    Check((await Account(brandUser)).Available==beforeDual.Available,"first approval does not release money");
    Check(!(await Balance(s=>s.CreditRefundAsync(admin,dualRefund.Funding,400000,dualOperation,default))).Success,"same admin cannot approve large credit twice");
    Check((await Balance(s=>s.CreditRefundAsync(admin2,dualRefund.Funding,400000,dualOperation,default))).Success,"second independent admin releases approved credit");
    Check((await Account(brandUser)).Available==beforeDual.Available+400000,"large credit posted exactly once after two approvals");
    await using(var db=Db())
    {
        var blocked=false;try{await db.Database.ExecuteSqlRawAsync("DELETE FROM dbo.BrandBalanceLedgers");}catch(SqlException){blocked=true;}
        Check(blocked,"ledger deletion blocked by deployment trigger");
    }
    await using(var db=Db())
    {
        var page=new Topstar.Web.Pages.Admin.Finance.BrandBalancesModel(new BrandBalanceService(db),null!,db);
        page.PageContext=new Microsoft.AspNetCore.Mvc.RazorPages.PageContext{HttpContext=new DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,admin.ToString()),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,"Admin")},"test"))}};
        page.PageContext.ViewData=new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary(new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider(),new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary());
        page.MetadataProvider=new Microsoft.AspNetCore.Mvc.ModelBinding.EmptyModelMetadataProvider();
        await page.OnGetAsync(default);
        Check(page.Ready&&page.Summary.Available==(await Account(brandUser)).Available,"admin summary aggregates all balances from database");
        var result=(Microsoft.AspNetCore.Mvc.JsonResult)await page.OnGetDataAsync("Isolated test Brand","",null,1,0,10,3,"desc",default);
        using var rows=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(result.Value));
        Check(rows.RootElement.GetProperty("recordsFiltered").GetInt32()>0,"withdrawal DataTable filters by Brand name in SQL");
        Check(rows.RootElement.GetProperty("data").GetArrayLength()<=10,"withdrawal DataTable paginates in SQL");
        result=(Microsoft.AspNetCore.Mvc.JsonResult)await page.OnGetDataAsync("",null,"refunds",1,0,10,3,"desc",default);
        using var refundsPage=System.Text.Json.JsonDocument.Parse(System.Text.Json.JsonSerializer.Serialize(result.Value));
        Check(refundsPage.RootElement.GetProperty("recordsTotal").GetInt32()>0,"refund DataTable returns only remaining refundable sources");
        Check(await page.OnGetDetailAsync(withdrawal,default) is Microsoft.AspNetCore.Mvc.PartialViewResult,"withdrawal modal loads account and approval details");
        Check(await page.OnGetApprovalsAsync(default) is Microsoft.AspNetCore.Mvc.JsonResult,"independent approval queue loads through authorized handler");
    }
    await Script("164_add_package_cancellation.sql");await Script("164_add_package_cancellation.sql");
    await using(var db=Db()){var role=Guid.NewGuid();db.Roles.Add(new(){Id=role,Code="Admin",Name="Admin"});db.UserRoles.Add(new(){UserId=admin,RoleId=role});db.UserRoles.Add(new(){UserId=admin2,RoleId=role});await db.SaveChangesAsync();}
    var packagePolicy=Guid.NewGuid();var packageOp=Guid.NewGuid();
    await SeedBalance(1000000);
    await using(var db=Db()){db.CampaignPackagePolicies.Add(new(){Id=packagePolicy,Code="BALANCE-TEST",Name="Test balance package",SalePrice=990000,ValidityDays=90,IsActive=true,CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();}
    var packageBefore=await Account(brandUser);
    Check((await Balance(s=>s.PurchasePackageAsync(brandUser,packagePolicy,packageOp,default))).Success,"package paid and activated from available balance");
    Check((await Balance(s=>s.PurchasePackageAsync(brandUser,packagePolicy,packageOp,default))).Success,"repeated package submission is idempotent");
    Check((await Account(brandUser)).Available==packageBefore.Available-990000,"package balance debited exactly once");
    await using(var db=Db()){var orders=await db.BrandPackagePurchases.Where(p=>p.PackagePolicyId==packagePolicy).ToListAsync();Check(orders.Count==1&&orders[0].Status=="Paid"&&orders[0].PaymentMethod=="BrandBalance"&&orders[0].ExpiresAtUtc.HasValue,"one paid package with snapshotted expiry");}
    Check(!(await Balance(s=>s.PurchasePackageAsync(stranger,packagePolicy,Guid.NewGuid(),default))).Success,"unauthorized user cannot spend Brand balance on package");
    await using(var db=Db()){var p=await db.CampaignPackagePolicies.SingleAsync(p=>p.Id==packagePolicy);p.SalePrice=999999999;await db.SaveChangesAsync();}
    Check(!(await Balance(s=>s.PurchasePackageAsync(brandUser,packagePolicy,Guid.NewGuid(),default))).Success,"insufficient balance rejects package without creating order");
    Guid cancelId;await using(var db=Db())cancelId=await db.BrandPackagePurchases.Where(p=>p.PackagePolicyId==packagePolicy).Select(p=>p.Id).SingleAsync();
    Check(!(await Balance(s=>s.CancelPackageAsync(stranger,cancelId,"Unauthorized cancel test",false,false,default))).Success,"foreign Brand cannot cancel package");
    Check((await Balance(s=>s.CancelPackageAsync(brandUser,cancelId,"Request package cancellation",false,false,default))).Success,"owner requests cancellation");
    await using(var db=Db())Check((await db.BrandPackagePurchases.SingleAsync(p=>p.Id==cancelId)).Status=="CancelRequested","requested package locked from campaign creation");
    Check(!(await Balance(s=>s.CancelPackageAsync(brandUser,cancelId,"Fake admin approval test",true,true,default))).Success,"Brand cannot approve own refund");
    Check((await Balance(s=>s.CancelPackageAsync(admin,cancelId,"Reject cancellation request",true,false,default))).Success,"Admin rejection restores package");
    Check((await Balance(s=>s.CancelPackageAsync(brandUser,cancelId,"Request cancellation again",false,false,default))).Success,"owner may request after rejection");
    var refundBefore=await Account(brandUser);
    Check((await Balance(s=>s.CancelPackageAsync(admin,cancelId,"Approve refund first review",true,true,default))).Success,"large package refund first approval");
    Check((await Account(brandUser)).Available==refundBefore.Available,"first approval cannot move money");
    Check(!(await Balance(s=>s.CancelPackageAsync(admin,cancelId,"Duplicate same admin approval",true,true,default))).Success,"same admin cannot approve twice");
    Check((await Balance(s=>s.CancelPackageAsync(admin2,cancelId,"Independent second approval",true,true,default))).Success,"second admin refunds package");
    Check((await Account(brandUser)).Available==refundBefore.Available+990000,"refund uses original paid price not current policy price");
    Check(!(await Balance(s=>s.CancelPackageAsync(admin2,cancelId,"Repeated refund approval test",true,true,default))).Success,"repeated refund rejected");
    await using(var db=Db())Check(await db.Database.SqlQuery<int>($"SELECT COUNT(*) AS Value FROM dbo.BrandBalanceLedgers WHERE SourceId={cancelId} AND EntryType=N'PackageRefund'").SingleAsync()==1,"exactly one package refund ledger entry");
    await using(var db=Db()){
     var fundingService=new Topstar.Infrastructure.Finance.CampaignFundingService(db,NullLogger<Topstar.Infrastructure.Finance.CampaignFundingService>.Instance);
     var window=new Topstar.Application.Common.ListWindow{AllowedStatuses=new[]{"Refunded"},Size=10};
     var filtered=await fundingService.GetForAdminAsync(default,window);
     Check(filtered.All(f=>f.Status=="Refunded"),"funding queue filters status in database before pagination");
     Check(window.Counts.Values.Sum()>window.Filtered,"funding statistics remain global across queues");
    }
    await using(var db=Db()){
     var dashboard=new Topstar.Web.Pages.Admin.DashboardModel(null!,null!,null!,db);
     var method=typeof(Topstar.Web.Pages.Admin.DashboardModel).GetMethod("LoadFinanceAsync",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance)!;
     var overview=await (Task<Topstar.Web.Pages.Admin.DashboardModel.FinanceOverview>)method.Invoke(dashboard,new object[]{CancellationToken.None})!;
     Check(overview.PackageCancellations==await db.BrandPackagePurchases.CountAsync(p=>p.Status=="CancelRequested"),"dashboard cancellation count uses all pending requests");
     Check(overview.FundingPending==await db.CampaignFundings.CountAsync(f=>f.Status=="Pending"),"dashboard escrow count uses real pending funds");
     Check(overview.Recent.Count<=6&&overview.Recent.All(r=>r.Url.StartsWith("/Admin/Finance/")),"dashboard latest requests limited and link to finance queues");
     Check(overview.Recent.SequenceEqual(overview.Recent.OrderByDescending(r=>r.CreatedAtUtc)),"dashboard newest requests appear first");
    }
    await Script("165_add_koc_tax_profiles.sql");await Script("165_add_koc_tax_profiles.sql");
    var taxProtection=new EphemeralDataProtectionProvider();
    var taxEnv=new TaxTestEnvironment{ContentRootPath=taxTestRoot};
    Topstar.Web.Pages.Koc.TaxProfileModel TaxPage(TopstarDbContext context,Guid user,string role){
     var page=new Topstar.Web.Pages.Koc.TaxProfileModel(context,taxProtection,taxEnv,NullLogger<Topstar.Web.Pages.Koc.TaxProfileModel>.Instance);
     page.PageContext=new Microsoft.AspNetCore.Mvc.RazorPages.PageContext{HttpContext=new DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,user.ToString()),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,role)},"test"))}};
     return page;
    }
    var imageBytes=Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+a6XcAAAAASUVORK5CYII=");
    IFormFile TaxImage()=>new FormFile(new MemoryStream(imageBytes),0,imageBytes.Length,"image","identity.png");
    await using(var db=Db()){
     var page=TaxPage(db,kocUser,"Koc");page.TaxNumber="0123456789";page.IdentityNumber="012345678901";page.Front=TaxImage();page.Back=TaxImage();
     Check(await page.OnPostAsync(default) is Microsoft.AspNetCore.Mvc.RedirectToPageResult,"KOC saves tax identity and both private images");
    }
    await using(var db=Db()){
     var page=TaxPage(db,kocUser,"Koc");await page.OnGetAsync(default);
     Check(page.TaxNumber=="0123456789"&&page.IdentityNumber=="012345678901"&&page.HasFront&&page.HasBack,"tax identity reload preserves leading zeroes and images");
     var row=await db.Database.SqlQuery<Topstar.Web.Pages.Koc.TaxProfileModel.TaxRow>($"SELECT * FROM dbo.KocTaxProfiles WHERE KocProfileId={koc}").SingleAsync();
     Check(!row.IdentityNumberCiphertext.Contains("012345678901")&&!row.TaxNumberCiphertext.Contains("0123456789"),"tax numbers stored encrypted");
     var stored=await File.ReadAllBytesAsync(Path.Combine(taxTestRoot,"App_Data","private-koc-tax",row.FrontFile));Check(!stored.SequenceEqual(imageBytes),"private identity image encrypted on disk");
     Check(await page.OnGetImageAsync("front",default) is Microsoft.AspNetCore.Mvc.FileContentResult image&&image.FileContents.SequenceEqual(imageBytes),"authorized owner can view decrypted identity image");
     var stale=TaxPage(db,kocUser,"Koc");stale.TaxNumber="0123456789";stale.IdentityNumber="012345678901";stale.Version=Guid.NewGuid();
     Check(await stale.OnPostAsync(default) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult&&!stale.ModelState.IsValid,"stale identity updates cannot overwrite another session");
    }
    await using(var db=Db()){
     var foreign=TaxPage(db,stranger,"Koc");foreign.Id=koc;Check(await foreign.OnGetImageAsync("front",default) is Microsoft.AspNetCore.Mvc.NotFoundResult,"foreign KOC cannot access private identity images by ID");
     var administrator=TaxPage(db,admin,"Admin");administrator.Id=koc;await administrator.OnGetAsync(default);Check(administrator.IdentityNumber=="012345678901","Admin can inspect private tax profile");
     Check(await administrator.OnPostAsync(default) is Microsoft.AspNetCore.Mvc.ForbidResult,"Admin tax profile access is read-only");
     var invalid=TaxPage(db,kocUser,"Koc");invalid.IdentityNumber="";invalid.TaxNumber="";await invalid.OnPostAsync(default);Check(!invalid.ModelState.IsValid,"empty tax and identity numbers rejected");
    }
    await using(var db=Db()){
     var page=TaxPage(db,kocUser,"Koc");await page.OnGetAsync(default);page.TaxNumber="123";page.IdentityNumber="45";
     Check(await page.OnPostAsync(default) is Microsoft.AspNetCore.Mvc.RedirectToPageResult,"nonempty tax identity accepts different lengths");
    }
    var vatRule=new BrandTaxPolicy{VatMode="Excluded",VatRate=10,TaxBase="ServiceFee"};
    Check(BrandTaxPricing.Calculate(vatRule,"Package",1000000,1000000).Total==1100000,"package exclusive VAT added once");
    Check(BrandTaxPricing.Calculate(vatRule,"Funding",11000000,1000000).Total==11100000,"funding VAT only on service fee by default");
    vatRule.VatMode="Included";var inclusive=BrandTaxPricing.Calculate(vatRule,"Package",1100000,1100000);Check(inclusive.Total==1100000&&inclusive.Vat==100000,"inclusive VAT does not increase payable");
    await using(var db=Db()){
     var now=DateTime.UtcNow;var id=Guid.NewGuid();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.BrandTaxPolicies(Id,Scope,VatMode,VatRate,TaxBase,EffectiveAtUtc,CreatedAtUtc,ActorUserId) VALUES({id},N'Package',N'Excluded',10,N'Total',{now},{now},{admin})");
     var quoted=await BrandTaxPricing.QuoteAsync(db,"Package",1000000,1000000,default);Check(quoted.Total==1100000&&quoted.PolicyId==id,"effective policy loaded from database");
     var campaignQuote=await BrandTaxPricing.QuoteAsync(db,"Funding",5500000,500000,default);
     Check(campaignQuote.PolicyId==id&&campaignQuote.Vat==550000&&campaignQuote.Total==6050000,"campaign checkout reuses legacy configured Brand VAT with original tax base");
     await using var overrideTx=await db.Database.BeginTransactionAsync();
     var explicitNone=Guid.NewGuid();await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.BrandTaxPolicies(Id,Scope,VatMode,VatRate,TaxBase,EffectiveAtUtc,CreatedAtUtc,ActorUserId) VALUES({explicitNone},N'Funding',N'None',0,N'ServiceFee',{now.AddMinutes(-1)},{now},{admin})");
     var noVat=await BrandTaxPricing.QuoteAsync(db,"Funding",5500000,500000,default);
     Check(noVat.PolicyId==explicitNone&&noVat.Vat==0,"explicit campaign VAT disabled overrides legacy enabled Brand VAT");
     await overrideTx.RollbackAsync();

    }
    await SeedBalance(2000000);var vatPurchaseOp=Guid.NewGuid();
    await using(var db=Db()){var policy=await db.CampaignPackagePolicies.SingleAsync(p=>p.Id==packagePolicy);policy.SalePrice=1000000;await db.SaveChangesAsync();}
    var beforeVatPurchase=await Account(brandUser);
    Check((await Balance(s=>s.PurchasePackageAsync(brandUser,packagePolicy,vatPurchaseOp,default))).Success,"balance checkout includes configured package VAT");
    Check((await Account(brandUser)).Available==beforeVatPurchase.Available-1100000,"wallet debits VAT-inclusive payable exactly once");
    await using(var db=Db()){
      var paid=await db.BrandPackagePurchases.OrderByDescending(p=>p.CreatedAtUtc).FirstAsync();Check(paid.AmountSnapshot==1100000&&paid.VatAmountSnapshot==100000&&paid.VatAddedSnapshot==100000,"purchase snapshots total tax and applied version");
      var now=DateTime.UtcNow;await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.BrandTaxPolicies(Id,Scope,VatMode,VatRate,TaxBase,EffectiveAtUtc,CreatedAtUtc,ActorUserId) VALUES({Guid.NewGuid()},N'Package',N'None',0,N'Total',{now},{now},{admin})");
      Check((await db.BrandPackagePurchases.AsNoTracking().SingleAsync(p=>p.Id==paid.Id)).AmountSnapshot==1100000,"new tax policy cannot change existing order total");
    }
    var vatFunding=await SeedFunding(1000000,"BankTransfer","Draft");
    await using(var db=Db()){
     var f=await db.CampaignFundings.SingleAsync(f=>f.Id==vatFunding.Funding);f.AllocationMode="Prepaid";
     var b=await db.BrandProfiles.SingleAsync(b=>b.Id==brand);b.RefundBankName="TEST BANK";b.RefundAccountNumber="123456789";b.RefundAccountHolder="TEST BRAND";
     var config=await db.PaymentMethodConfigurations.FirstAsync();config.BankTransferEnabled=true;
     var fee=await db.CampaignFeePolicies.FirstAsync();fee.FixedFee=100000;fee.FeeRate=0;fee.GatewayFeeRate=0;
     await db.SaveChangesAsync();var now=DateTime.UtcNow;
     await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.BrandTaxPolicies(Id,Scope,VatMode,VatRate,TaxBase,EffectiveAtUtc,CreatedAtUtc,ActorUserId) VALUES({Guid.NewGuid()},N'Funding',N'Excluded',10,N'ServiceFee',{now},{now},{admin})");
    }
    await using(var db=Db()){
     var service=new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance);
     Check(!(await service.SubmitAsync(brandUser,vatFunding.Campaign,"BankTransfer",default,1100000)).Success,"funding confirmation rejects old total missing VAT");
     Check((await service.SubmitAsync(brandUser,vatFunding.Campaign,"BankTransfer",default,1110000)).Success,"funding confirmation includes configured VAT");
    }
    var savedVatFunding=await Fund(vatFunding.Funding);Check(savedVatFunding.TotalPayableSnapshot==1110000&&savedVatFunding.VatAmountSnapshot==10000,"funding keeps KOC budget separate from service VAT");
    Check(KocIncomeTax.Calculate(10000000,10,5000000)==1000000,"KOC withholding uses full gross above configured threshold");
    Check(KocIncomeTax.Calculate(4999999,10,5000000)==0,"KOC withholding threshold evaluated on gross");
    var taxEarning=Guid.NewGuid();var taxParticipation=Guid.NewGuid();
    await using(var db=Db()){
     var now=DateTime.UtcNow;await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.KocIncomeTaxPolicies(Id,IsEnabled,Rate,Threshold,EffectiveAtUtc,CreatedAtUtc,ActorUserId) VALUES({Guid.NewGuid()},1,10,5000000,{now},{now},{admin})");
     db.Participations.Add(new(){Id=taxParticipation,KocProfileId=koc,CampaignId=vatFunding.Campaign,Status="Confirmed",ConfirmedAtUtc=now,CreatedAtUtc=now});
     db.WalletTransactions.Add(new(){Id=taxEarning,KocProfileId=koc,CampaignId=vatFunding.Campaign,ParticipationId=taxParticipation,TransactionType="CampaignEarning",Amount=10000000,Status="Pending",CreatedAtUtc=now,CreatedByUserId=admin});await db.SaveChangesAsync();
     var held=await db.WalletTransactions.AsNoTracking().SingleAsync(w=>w.Id==taxEarning);Check(held.Status=="OnHold"&&held.TaxState=="Awaiting","missing tax verification holds earning from withdrawal");
     var blocked=false;try{await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.WalletTransactions SET Status=N'Available' WHERE Id={taxEarning}");}catch(SqlException){blocked=true;}Check(blocked,"direct database update cannot release income awaiting tax verification");

    }
    await using(var db=Db()){
     var now=DateTime.UtcNow;await db.Database.ExecuteSqlInterpolatedAsync($"INSERT dbo.KocTaxTreatments(KocProfileId,Treatment,Reason,UpdatedAtUtc,ActorUserId) VALUES({koc},N'Withholding',N'Isolated verification',{now},{admin})");
     var wallet=await db.WalletTransactions.SingleAsync(w=>w.Id==taxEarning);wallet.Status="Pending";await db.SaveChangesAsync();
     Check(wallet.GrossAmountSnapshot==10000000&&wallet.TaxAmountSnapshot==1000000&&wallet.Amount==9000000,"verified earning credits only net and retains gross tax snapshots");
     var taxDisplay=await Topstar.Web.Services.Payments.KocTaxPresentation.ReadAsync(db,wallet.ParticipationId,null,CancellationToken.None);
     Check(taxDisplay is { Final:true,Gross:10000000,Tax:1000000,Net:9000000 },"tax views use final wallet snapshots rather than current policy");
     wallet.Status="Available";await db.SaveChangesAsync();Check(wallet.Amount==9000000&&wallet.TaxAmountSnapshot==1000000,"availability transition cannot deduct tax twice");
    }
    await using(var db=Db()){
     var blocked=false;try{await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.WalletTransactions SET Amount=10000000 WHERE Id={taxEarning}");}catch(SqlException){blocked=true;}Check(blocked,"database rejects changing already calculated net amount");
     var page=new Topstar.Web.Pages.Admin.Finance.KocIncomeTaxModel(db);
     page.PageContext=new Microsoft.AspNetCore.Mvc.RazorPages.PageContext{HttpContext=new DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,admin.ToString()),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,"Admin")},"test"))}};
     await page.OnGetAsync(default);Check(page.Withheld==1000000&&page.Remitted==0,"tax report separates withholding from remittance");
     await page.OnPostRemitAsync(taxEarning,"TEST-TAX-RECEIPT",default);await page.OnGetAsync(default);Check(page.Remitted==1000000,"Admin remittance receipt updates paid tax only");
     var wallet=await db.WalletTransactions.AsNoTracking().SingleAsync(w=>w.Id==taxEarning);Check(wallet.Amount==9000000,"recording remittance cannot deduct KOC wallet again");
    }
    await using(var db=Db()){
      var page=new Topstar.Web.Pages.Admin.Finance.BrandTaxPolicyModel(db);
      page.PageContext=new Microsoft.AspNetCore.Mvc.RazorPages.PageContext{HttpContext=new DefaultHttpContext()};page.Request.Method="GET";page.Scope="Funding";await page.OnGetAsync(default);
      Check(page.Current is not null&&page.VatRate==10&&page.VatMode=="Excluded"&&page.TaxBase=="ServiceFee","Brand funding settings show actual effective policy");
      page.Scope="Package";await page.OnGetAsync(default);Check(page.VatMode=="None"&&page.VatRate==0,"switching Brand tax scope loads its own settings");
      page.Request.Method="POST";page.Scope="Funding";page.VatRate=101;await page.OnPostAsync(default);Check(!page.ModelState.IsValid&&page.VatRate==101,"invalid Brand tax settings retain entered values");
    }

    // Needs-first creation: no purchase prerequisite; no content approval before money is secured.
    Guid createdCampaign;
    await using(var db=Db()){
      var rules=(await CampaignCreationRules.ReadAsync(db))!;
      Check(CampaignCreationRules.Validate(rules,7000000,3,false) is null,"custom 7m allows TopStar allocation");
      Check(CampaignCreationRules.Validate(rules,7000000,3,true) is not null,"custom 7m cannot bypass self-selection threshold");
      Check(CampaignCreationRules.Validate(rules,10000000,3,true) is null,"10m enables self-selection");
      Check(CampaignCreationRules.Validate(rules,7000000,8,false) is not null,"custom KOC count enforces budget per KOC");
      db.OperationalCatalogItems.Add(new(){Id=Guid.NewGuid(),CatalogType=Topstar.Application.Settings.OperationalCatalogTypes.Deliverable,Code="video",Name="Video",IsActive=true,CreatedAtUtc=DateTime.UtcNow,UpdatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();
      await db.Database.ExecuteSqlRawAsync("UPDATE dbo.CampaignCreationPolicies SET KocPlatformFeeRate=5 WHERE Id=1");
      var start=new DateOnly(2026,11,1);var end=new DateOnly(2026,11,30);
      Check(Topstar.Application.Campaigns.CampaignScheduleRules.ValidateDeadline(start,end,null) is null,"deadline can be omitted");
      Check(Topstar.Application.Campaigns.CampaignScheduleRules.ValidateDeadline(start,end,new DateTime(2026,10,31,23,59,0)) is not null,"deadline cannot precede start");
      Check(Topstar.Application.Campaigns.CampaignScheduleRules.ValidateDeadline(start,end,new DateTime(2026,12,1)) is not null,"deadline cannot exceed end");
      Check(Topstar.Application.Campaigns.CampaignScheduleRules.ValidateDeadline(start,end,new DateTime(2026,11,30,23,59,59)) is null,"deadline includes full final day");
      var service=new CampaignService(db);
      var request=new Topstar.Application.Campaigns.CampaignUpsertRequest("Needs-first integration", "Test brief",7000000,"CUSTOM_ASSIGNED",false,true,DateOnly.FromDateTime(DateTime.UtcNow),DateOnly.FromDateTime(DateTime.UtcNow.AddDays(30)),"video",1,DateTime.UtcNow.AddDays(15)){CheckoutAtCreation=true,DesiredKocCount=3,Execution=new(){ShippingInstructions=null}};
      Check(!(await service.CreateDraftAsync(brandUser,request with{BrandSelectionRequested=true})).Success,"server rejects forged self-selection below threshold");
      var onsite=await service.CreateDraftAsync(brandUser,request with{Execution=new(){Mode="FoodReviewOnsite"}});
      Check(onsite.Success,"new campaign can be created before entering detailed onsite preparation");
      var preparation=await db.Campaigns.SingleAsync(c=>c.Id==onsite.Id);
      preparation.Status="Published";await db.SaveChangesAsync();
      Check(CampaignWorkspaceService.PreparationError(preparation) is not null,"onsite details are required before opening work rather than before creation");
      Check(!(await new CampaignWorkspaceService(db).OpenWorkAsync(brandUser,preparation.Id,null,false)).Success,"opening work cannot bypass missing preparation");
      Topstar.Web.Pages.Brand.CampaignPreparationModel PreparationPage(Guid user){
       var page=new Topstar.Web.Pages.Brand.CampaignPreparationModel(new PreparationTestAccess(),db){Id=preparation.Id};
       var context=new DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,user.ToString()),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,"Brand")},"test"))};
       page.PageContext=new Microsoft.AspNetCore.Mvc.RazorPages.PageContext{HttpContext=context};
       page.TempData=new Microsoft.AspNetCore.Mvc.ViewFeatures.TempDataDictionary(context,new PreparationTestTempData());return page;
      }
      Check(await PreparationPage(stranger).OnGetAsync(default) is Microsoft.AspNetCore.Mvc.NotFoundResult,"another Brand cannot access campaign preparation");
      var prepPage=PreparationPage(brandUser);prepPage.Input=new(){Mode="LivestreamOnline",VenueName="Test venue",VenueAddress="Test address",ContactName="Test contact",ContactPhone="0900000000",Schedule="10:00"};
      Check(await prepPage.OnPostAsync(default) is Microsoft.AspNetCore.Mvc.RedirectToPageResult,"Brand can save preparation after campaign approval before work opens");
      Check(preparation.ExecutionMode=="FoodReviewOnsite"&&preparation.BudgetAmount==7000000,"preparation cannot change recruitment mode or financial budget");
      Check(CampaignWorkspaceService.PreparationError(preparation) is null,"complete preparation allows launch prerequisite to pass");
      preparation.Status="InProgress";await db.SaveChangesAsync();
      Check(await PreparationPage(brandUser).OnPostAsync(default) is Microsoft.AspNetCore.Mvc.ForbidResult,"preparation cannot change once campaign work is in progress");
      Check(!(await service.CreateDraftAsync(brandUser,request with{DeliverableDueDateUtc=DateTime.UtcNow.AddDays(-1)})).Success,"campaign service rejects deadline before start");
      Check(!(await service.CreateDraftAsync(brandUser,request with{DeliverableDueDateUtc=DateTime.UtcNow.AddDays(31)})).Success,"campaign service rejects deadline after end");
      Check(!(await service.CreateDraftAsync(brandUser,request with{DeliverableDueDateUtc=null})).Success,"new campaign cannot save with missing deadline");
      Check(!(await service.CreateDraftAsync(brandUser,request with{StartDate=Topstar.Application.Campaigns.CampaignScheduleRules.TodayVietnam.AddDays(-1)})).Success,"server rejects past campaign start even with valid future deadline");
      var create=await service.CreateDraftAsync(brandUser,request);Check(create.Success,"approved Brand creates campaign without buying package: "+create.Message);createdCampaign=create.Id!.Value;
      Check(!await db.BrandPackagePurchases.AnyAsync(p=>p.CampaignId==createdCampaign),"new campaign does not consume or fabricate legacy purchase");
      var stored=await db.Campaigns.SingleAsync(c=>c.Id==createdCampaign);Check(stored.SelectionQuotaSnapshot==3&&!stored.BrandSelectionSnapshot&&stored.KocPlatformFeeRateSnapshot==5,"needs and KOC fee frozen on campaign");
      Check(!(await service.SubmitAsync(createdCampaign,brandUser)).Success,"unpaid new campaign cannot submit content for review");
      Check(!(await service.UpdateDraftAsync(createdCampaign,brandUser,request with{BudgetAmount=8000000})).Success,"content edit cannot silently change confirmed budget");
      var funds=new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance);
      Check(!(await funds.PrepareFullFundingAsync(brandUser,createdCampaign,default,1000)).Success,"new campaign cannot pay tiny partial budget to bypass full checkout");
      Check((await funds.PrepareFullFundingAsync(brandUser,createdCampaign)).Success,"full campaign budget prepared before content approval and before KOC selection");
      var quote=await BrandTaxPricing.QuoteAsync(db,"Funding",7100000,100000,default);
      Check(!(await funds.SubmitAsync(brandUser,createdCampaign,"BrandBalance",default,1)).Success,"tampered campaign payment total rejected");
      Check((await funds.SubmitAsync(brandUser,createdCampaign,"BrandBalance",default,quote.Total)).Success,"new creation checkout charges budget plus Brand fees and VAT once");
      var pending=await db.CampaignFundings.SingleAsync(f=>f.CampaignId==createdCampaign);
      Check(!(await funds.ConfirmPaidAsync(admin,pending.Id,"FAKE-BALANCE-RECEIPT","Cannot substitute bank receipt for actual wallet debit",default)).Success,"Admin cannot mark wallet payment paid without debiting balance");
    }
    await SeedBalance(20000000);
    Guid createdFunding;decimal createdTotal;
    await using(var db=Db()){var f=await db.CampaignFundings.SingleAsync(f=>f.CampaignId==createdCampaign);createdFunding=f.Id;createdTotal=f.TotalPayableSnapshot;}
    var payOperation=Guid.NewGuid();Check((await Balance(b=>b.PayFundingAsync(brandUser,createdFunding,createdTotal,payOperation,default))).Success,"Brand balance pays creation checkout before content review");
    Check((await Balance(b=>b.PayFundingAsync(brandUser,createdFunding,createdTotal,payOperation,default))).Success,"creation wallet payment replay is idempotent");
    await using(var db=Db()){
      var c=await db.Campaigns.SingleAsync(c=>c.Id==createdCampaign);Check(c.Status=="Submitted"&&!c.IsVisible,"verified payment automatically queues content review without publishing");
      Check(await db.CampaignStatusHistories.CountAsync(h=>h.CampaignId==createdCampaign&&h.ToStatus=="Submitted")==1,"payment retry cannot duplicate content submission");
      await db.Database.ExecuteSqlRawAsync("UPDATE dbo.CampaignCreationPolicies SET KocPlatformFeeRate=20 WHERE Id=1");
      var part=Guid.NewGuid();db.Participations.Add(new(){Id=part,CampaignId=createdCampaign,KocProfileId=koc,Status="Confirmed",ConfirmedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();
      var earning=new WalletTransactionEntity{Id=Guid.NewGuid(),CampaignId=createdCampaign,ParticipationId=part,KocProfileId=koc,TransactionType="CampaignEarning",Amount=6000000,Status="Pending",CreatedByUserId=admin,CreatedAtUtc=DateTime.UtcNow};db.WalletTransactions.Add(earning);await db.SaveChangesAsync();
      Check(earning.PlatformFeeRateSnapshot==5&&earning.PlatformFeeAmountSnapshot==300000&&earning.TaxAmountSnapshot==600000&&earning.Amount==5100000,"KOC pays campaign-snapshotted platform fee plus tax on gross remuneration");
      var display=await KocTaxPresentation.ReadAsync(db,part,null,default);Check(display?.PlatformFee==300000&&display.Net==5100000,"KOC offer and wallet summaries expose platform fee and net");
      earning.Status="Available";await db.SaveChangesAsync();Check(earning.Amount==5100000,"making KOC money withdrawable cannot deduct platform fee twice");
      var blocked=false;try{await db.Database.ExecuteSqlInterpolatedAsync($"UPDATE dbo.WalletTransactions SET PlatformFeeAmountSnapshot=0 WHERE Id={earning.Id}");}catch(SqlException){blocked=true;}Check(blocked,"database protects already settled KOC platform fee");
    }

    var offerFunding=await SeedFunding(7000000,"BankTransfer","Paid");
    Guid offerParticipation=Guid.NewGuid();
    await using(var db=Db()){
      var c=await db.Campaigns.SingleAsync(c=>c.Id==offerFunding.Campaign);c.CheckoutAtCreation=true;c.SelectionQuotaSnapshot=3;c.KocPlatformFeeRateSnapshot=5;
      var f=await db.CampaignFundings.SingleAsync(f=>f.Id==offerFunding.Funding);f.AllocationMode="Prepaid";
      db.Participations.Add(new(){Id=offerParticipation,CampaignId=c.Id,KocProfileId=koc,Status="Invited",ConfirmedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();
      var matching=new Topstar.Infrastructure.Matching.MatchingService(db);
      Check(!(await matching.AcceptInvitationAsync(kocUser,offerParticipation)).Success,"KOC cannot accept new job before Brand allocates remuneration");
      var funds=new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance);
      var summary=await funds.GetForBrandAsync(brandUser,c.Id);Check(summary?.IsAllocatingPrepaidReserve==true&&summary.Allocations.Any(a=>a.ParticipationId==offerParticipation),"paid reserve can allocate to invited KOC before acceptance");
      Check((await funds.SaveAllocationsAsync(brandUser,c.Id,"Custom",new Dictionary<Guid,decimal>{{offerParticipation,2000000}})).Success,"Brand allocates agreed custom remuneration from prepaid budget");
      var preview=await KocTaxPresentation.ReadAsync(db,offerParticipation,null,default);Check(preview?.Gross==2000000&&preview.PlatformFee==100000,"KOC sees actual allocated remuneration and fee before accepting");
      Check((await matching.AcceptInvitationAsync(kocUser,offerParticipation)).Success,"KOC may accept funded job after seeing remuneration");
      Check(!(await matching.AcceptInvitationAsync(kocUser,offerParticipation)).Success,"replayed acceptance cannot create duplicate work");
    }

    var creationOnline=await SeedFunding(7000000,"VnPay");
    await using(var db=Db()){
      var c=await db.Campaigns.SingleAsync(c=>c.Id==creationOnline.Campaign);c.Status="Draft";c.CheckoutAtCreation=true;c.IsVisible=false;await db.SaveChangesAsync();
      var url=await new VnPayService(db,protection,journal).CreateAsync(brandUser,creationOnline.Funding,true,"127.0.0.1",default);
      Check(url.Success&&url.RedirectUrl is not null,"online gateway accepts new creation checkout while content is still draft");
    }
    Check((await Process(Callback(creationOnline.Funding,7000000,"9876543"),false)).Success,"signed online result settles creation payment");
    Check((await Process(Callback(creationOnline.Funding,7000000,"9876543"),true)).Success,"creation online IPN replay is acknowledged without a second charge");
    await using(var db=Db()){var c=await db.Campaigns.SingleAsync(c=>c.Id==creationOnline.Campaign);Check(c.Status=="Submitted"&&!c.IsVisible,"online payment queues content review and does not auto-publish");}
    var creationBank=await SeedFunding(7000000,"BankTransfer");
    await using(var db=Db()){
      var c=await db.Campaigns.SingleAsync(c=>c.Id==creationBank.Campaign);c.Status="Draft";c.CheckoutAtCreation=true;c.IsVisible=false;await db.SaveChangesAsync();
      var result=await new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance).ConfirmPaidAsync(admin,creationBank.Funding,"TEST-BANK-PAID","Verified actual receipt on isolated bank statement",default);
      Check(result.Success&&c.Status=="Submitted","bank reconciliation queues content review as a separate step");
    }

    await using(var db=Db()){
      var rules=(await CampaignCreationRules.ReadAsync(db))!;
      var legacy=new Topstar.Application.Matching.PackagePolicyItem(Guid.NewGuid(),"LEGACY_REFERENCE","Legacy reference",0,null,false,3,true){SalePrice=990000};
      var suggested=CampaignCreationRules.ReferenceTemplate(rules,legacy);
      Check(suggested is null,"unconfigured legacy template is not given an invented budget");
      Check(legacy.SalePrice==990000&&legacy.SelectionQuota==3&&!legacy.BrandSelectionEnabled,"suggesting campaign budget preserves legacy price, KOC count and selection rights");
      var self=CampaignCreationRules.ReferenceTemplate(rules,legacy with{BrandSelectionEnabled=true});
      Check(self is null,"unconfigured self-selection template is not given an invented budget");
      var explicitBudget=CampaignCreationRules.ReferenceTemplate(rules,legacy with{ReferenceBudget=7000000});
      Check(explicitBudget?.ReferenceBudget==7000000&&!explicitBudget.ReferenceBudgetIsSuggested,"explicit Admin reference budget takes priority over compatibility suggestion");
      var policies=new Topstar.Infrastructure.Matching.PackagePolicyAdminService(db);
      var catalog=await policies.GetPublicAsync();
      Check(!catalog.Any(p=>p.Id==packagePolicy),"unconfigured packages are excluded from Brand catalog");
      Check((await db.CampaignPackagePolicies.AsNoTracking().SingleAsync(p=>p.Id==packagePolicy)).ReferenceBudget is null,"catalog compatibility does not write suggested prices to database");
      var configured=await db.CampaignPackagePolicies.SingleAsync(p=>p.Id==packagePolicy);
      configured.ReferenceBudget=7000000;configured.SelectionQuota=3;configured.BrandSelectionEnabled=false;await db.SaveChangesAsync();
      var published=(await policies.GetPublicAsync()).Single(p=>p.Id==packagePolicy);
      Check(published.ReferenceBudget==7000000&&published.SelectionQuota==3&&!published.BrandSelectionEnabled,"Brand catalog uses the exact Admin budget, KOC count and recruitment mode");
      Check(CampaignCreationRules.ReferenceTemplate(rules,legacy with{ReferenceBudget=7000000,BrandSelectionEnabled=true}) is null,"template cannot bypass self selection budget threshold");

    }
    await using(var db=Db()){
      if(!await db.PayoutPolicies.AnyAsync()){
       db.PayoutPolicies.Add(new(){Id=Guid.NewGuid(),Code="DEFAULT",MinimumAmount=50000,DailyRequestLimit=3,EffectiveAtUtc=DateTime.UtcNow,CreatedAtUtc=DateTime.UtcNow});await db.SaveChangesAsync();
      }
      var page=new Topstar.Web.Pages.Admin.Finance.TaxFeesModel(db,new CampaignFundingService(db,NullLogger<CampaignFundingService>.Instance),new WalletService(db,new EphemeralDataProtectionProvider()));
      page.PageContext=new Microsoft.AspNetCore.Mvc.RazorPages.PageContext{HttpContext=new DefaultHttpContext{User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,admin.ToString()),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,"Admin")},"test"))}};
      page.Request.Path="/Admin/Finance/TaxFees";page.Request.Method="GET";
      await page.OnGetAsync(default);
      Check(page.Vat.Ready&&page.Vat.Scope=="Funding"&&page.BrandFee.Policy is not null,"unified Brand tab loads effective VAT and platform fee together");
      page.Tab="Koc";await page.OnGetAsync(default);
      Check(page.KocTax.Ready&&page.FeeReady&&page.Payout.Policy is not null,"unified KOC tab loads PIT platform fees and withdrawal fees together");
      var before=await CampaignCreationRules.ReadAsync(db,default);
      page.Request.Method="POST";page.KocFeeRate=7;
      Check(await page.OnPostKocFeeAsync(default) is Microsoft.AspNetCore.Mvc.RedirectToPageResult,"unified KOC platform fee saves through audited handler");
      var after=await CampaignCreationRules.ReadAsync(db,default);
      Check(after!.KocPlatformFeeRate==7&&after.MinimumBudget==before!.MinimumBudget&&after.MaximumKocCount==before.MaximumKocCount,"saving KOC fee preserves campaign need thresholds");
      page.KocFeeRate=100;page.ModelState.Clear();
      Check(await page.OnPostKocFeeAsync(default) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult&&!page.ModelState.IsValid,"unified KOC fee rejects out of range rate");
      Check((await CampaignCreationRules.ReadAsync(db,default))!.KocPlatformFeeRate==7,"invalid fee submission cannot change saved configuration");
      page.ModelState.Clear();page.VatMode="Excluded";page.VatRate=101;page.TaxBase="ServiceFee";page.EffectiveAt=DateTime.UtcNow.AddHours(7);
      Check(await page.OnPostVatAsync(default) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult&&page.Vat.VatRate==101&&!page.ModelState.IsValid,"unified VAT invalid input stays visible with validation errors");
      page.ModelState.Clear();page.VatRate=8;
      Check(await page.OnPostVatAsync(default) is Microsoft.AspNetCore.Mvc.RedirectToPageResult,"unified VAT saves as current campaign policy");
      var unifiedQuote=await BrandTaxPricing.QuoteAsync(db,"Funding",5500000,500000,default);
      Check(unifiedQuote.Rate==8&&unifiedQuote.Vat==40000&&unifiedQuote.Total==5540000,"unified VAT configuration feeds campaign quote without mixing legacy scope");
      page.ModelState.Clear();page.Rate=100;page.Threshold=2000000;page.Enabled=true;
      Check(await page.OnPostKocTaxAsync(default) is Microsoft.AspNetCore.Mvc.RazorPages.PageResult&&page.KocTax.Rate==100&&!page.ModelState.IsValid,"unified PIT invalid input stays on KOC tab for correction");

    }
    // Render the actual compiled Razor pages against this disposable database only.
    var uiBuilder=WebApplication.CreateBuilder(new WebApplicationOptions{ApplicationName=typeof(Topstar.Web.Pages.Admin.Finance.TaxFeesModel).Assembly.FullName,ContentRootPath=Path.Combine(root,"backend","src","Topstar.Web"),WebRootPath=Path.Combine(root,"backend","src","Topstar.Web","wwwroot"),EnvironmentName="Development"});
    uiBuilder.Configuration.AddInMemoryCollection(new Dictionary<string,string?>{{"ConnectionStrings:TopstarDb",connection},{"BackgroundJobs:Enabled","false"}});
    uiBuilder.Logging.ClearProviders();uiBuilder.Services.AddTopstarInfrastructure(uiBuilder.Configuration);uiBuilder.Services.AddAuthentication();uiBuilder.Services.AddAuthorization();uiBuilder.Services.AddRazorPages();
    uiBuilder.Services.AddScoped<Topstar.Web.Services.Storage.IFileStorageService,Topstar.Web.Services.Storage.FileStorageService>();
    uiBuilder.Services.AddSingleton<Topstar.Application.Profiles.IProfileAccessService,PreparationTestAccess>();
    uiBuilder.WebHost.UseUrls("http://127.0.0.1:0");
    await using(var ui=uiBuilder.Build()){
      ui.Use(async(context,next)=>{context.User=new System.Security.Claims.ClaimsPrincipal(new System.Security.Claims.ClaimsIdentity(new[]{new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.NameIdentifier,context.Request.Path.StartsWithSegments("/Brand")?brandUser.ToString():admin.ToString()),new System.Security.Claims.Claim(System.Security.Claims.ClaimTypes.Role,context.Request.Path.StartsWithSegments("/Brand")?"Brand":"Admin")},"isolated-finance-fixture"));await next(context);});
      ui.UseStaticFiles();ui.UseRouting();ui.UseAuthorization();ui.MapRazorPages();await ui.StartAsync();
      using var client=new HttpClient{BaseAddress=new Uri(ui.Urls.Single())};
      var simpleHtml=await client.GetStringAsync("/Brand/CampaignEdit?budget=7000000&kocCount=3&selfSelect=false");
      await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"topstar-campaign-simple.html"),simpleHtml);
      Check(simpleHtml.Contains("id=\"campaignAdditional\"")&&simpleHtml.IndexOf("id=\"campaignAdditional\"",StringComparison.Ordinal)<simpleHtml.IndexOf("id=\"AvatarFile\"",StringComparison.Ordinal),"real Brand Razor places media and extended fields inside additional information");
      Check(simpleHtml.Contains("name=\"Input.AllocationMode\" type=\"hidden\"")||Regex.IsMatch(simpleHtml,"type=\"hidden\"[^>]*name=\"Input.AllocationMode\""),"new campaign form preserves allocation mode without asking Brand to configure it");
      var brandHtml=await client.GetStringAsync("/Admin/Finance/TaxFees?tab=Brand");
      var creationHtml=await client.GetStringAsync("/Admin/Finance/CampaignCreation");
      await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"topstar-creation-config.html"),creationHtml);
      Check(brandHtml.Contains("handler=Vat")&&brandHtml.Contains("/Admin/Finance/CampaignFeePolicy",StringComparison.OrdinalIgnoreCase),"actual Brand Razor renders VAT and fee forms with correct POST targets");
      var kocHtml=await client.GetStringAsync("/Admin/Finance/TaxFees?tab=Koc");
      Check(kocHtml.Contains("handler=KocFee")&&kocHtml.Contains("handler=KocTax")&&kocHtml.Contains("/Admin/Finance/PayoutPolicy",StringComparison.OrdinalIgnoreCase),"actual KOC Razor renders platform fee PIT and withdrawal policy editors");
      var token=System.Net.WebUtility.HtmlDecode(Regex.Match(kocHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
      var feePost=await client.PostAsync("/Admin/Finance/TaxFees?handler=KocFee",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",token},{"Tab","Koc"},{"KocFeeRate","6.5"}}));
      var feePostHtml=await feePost.Content.ReadAsStringAsync();
      await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"topstar-taxfees-post.html"),feePostHtml);
      Check(feePostHtml.Contains("data-ts-notification=\"success\""),"actual fee save displays shared success notification after redirect");
      await using(var verify=Db()){Check((await CampaignCreationRules.ReadAsync(verify,default))!.KocPlatformFeeRate==6.5m,"actual HTTP fee form saves using only fields present in KOC form");}
      kocHtml=await client.GetStringAsync("/Admin/Finance/TaxFees?tab=Koc");
      token=System.Net.WebUtility.HtmlDecode(Regex.Match(kocHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
      var taxPost=await client.PostAsync("/Admin/Finance/TaxFees?handler=KocTax",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",token},{"Enabled","true"},{"Rate","9"},{"Threshold","2000000"},{"EffectiveAt",DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-ddTHH:mm:ss")}}));
      await using(var verify=Db()){var current=await verify.Database.SqlQuery<KocIncomeTaxPolicy>($"SELECT * FROM dbo.KocIncomeTaxPolicies").OrderByDescending(p=>p.CreatedAtUtc).FirstAsync();Check(current.Rate==9&&current.Threshold==2000000,"actual HTTP PIT form saves without unrelated Brand fields");}
      var taxPostHtml=await taxPost.Content.ReadAsStringAsync();
      Check(taxPostHtml.Contains("data-ts-notification=\"success\""),"actual PIT save displays shared success notification");
      token=System.Net.WebUtility.HtmlDecode(Regex.Match(taxPostHtml,"name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"").Groups[1].Value);
      var invalidTax=await client.PostAsync("/Admin/Finance/TaxFees?handler=KocTax",new FormUrlEncodedContent(new Dictionary<string,string>{{"__RequestVerificationToken",token},{"Enabled","true"},{"Rate","100"},{"Threshold","2000000"},{"EffectiveAt",DateTime.UtcNow.AddHours(7).ToString("yyyy-MM-ddTHH:mm:ss")}}));
      var invalidHtml=await invalidTax.Content.ReadAsStringAsync();
      Check(invalidHtml.Contains("field-validation-error")&&invalidHtml.Contains("value=\"100\""),"actual invalid PIT form shows field error and preserves entered rate");
      await using(var verify=Db()){Check((await verify.Database.SqlQuery<KocIncomeTaxPolicy>($"SELECT * FROM dbo.KocIncomeTaxPolicies").OrderByDescending(p=>p.CreatedAtUtc).FirstAsync()).Rate==9,"invalid HTTP PIT submission cannot overwrite saved policy");}

      await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"topstar-taxfees-brand.html"),brandHtml);await File.WriteAllTextAsync(Path.Combine(Path.GetTempPath(),"topstar-taxfees-koc.html"),kocHtml);
      await ui.StopAsync();
    }
    System.Console.WriteLine($"Completed {count} integration checks on isolated database {database}.");
}
finally
{
    // Name is created locally above, never accepted from arguments or application configuration.
    SqlConnection.ClearAllPools();await using var cleanup=Db();await cleanup.Database.EnsureDeletedAsync();
    if(taxTestRoot.StartsWith(Path.Combine(Path.GetTempPath(),"TopstarTaxTest_"),StringComparison.OrdinalIgnoreCase)&&Directory.Exists(taxTestRoot))Directory.Delete(taxTestRoot,true);
}

sealed class TaxTestEnvironment:Microsoft.AspNetCore.Hosting.IWebHostEnvironment{
 public string ApplicationName{get;set;}="Test";public string EnvironmentName{get;set;}="Development";public string ContentRootPath{get;set;}="";public string WebRootPath{get;set;}="";
 public Microsoft.Extensions.FileProviders.IFileProvider ContentRootFileProvider{get;set;}=new Microsoft.Extensions.FileProviders.NullFileProvider();public Microsoft.Extensions.FileProviders.IFileProvider WebRootFileProvider{get;set;}=new Microsoft.Extensions.FileProviders.NullFileProvider();
}

sealed class PreparationTestAccess:Topstar.Application.Profiles.IProfileAccessService {
 public Task<Topstar.Application.Profiles.ProfileAccessResult> CheckBrandAsync(Guid user,CancellationToken ct=default)=>Task.FromResult(new Topstar.Application.Profiles.ProfileAccessResult(true,true,true,"Approved",[]));
 public Task<Topstar.Application.Profiles.ProfileAccessResult> CheckKocAsync(Guid user,CancellationToken ct=default)=>CheckBrandAsync(user,ct);
}
sealed class PreparationTestTempData:Microsoft.AspNetCore.Mvc.ViewFeatures.ITempDataProvider {
 public IDictionary<string,object> LoadTempData(HttpContext context)=>new Dictionary<string,object>();
 public void SaveTempData(HttpContext context,IDictionary<string,object> values){}
}

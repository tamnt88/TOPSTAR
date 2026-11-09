using System.Text.Json;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Primitives;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Web.Services.Payments;

public sealed class VnPayCallbackJournal(IServiceScopeFactory scopes,IHttpContextAccessor accessor,ILogger<VnPayCallbackJournal> logger)
{
    private static string? Clip(string? value,int length)=>value is null?null:value[..Math.Min(value.Length,length)];

    public async Task<Guid?> StartAsync(IReadOnlyDictionary<string,StringValues> query,bool ipn)
    {
        var context=accessor.HttpContext;
        var entry=new VnPayCallbackLogEntity
        {
            Id=Guid.NewGuid(),Source=ipn?"IPN":"Return",ReceivedAtUtc=DateTime.UtcNow,
            TargetReference=Clip(query.GetValueOrDefault("vnp_TxnRef").ToString(),100),
            TransactionNo=Clip(query.GetValueOrDefault("vnp_TransactionNo").ToString(),100),
            RawQuery=context?.Request.QueryString.Value??"",
            PayloadJson=JsonSerializer.Serialize(query.ToDictionary(x=>x.Key,x=>x.Value.ToArray())),
            RemoteIp=Clip(context?.Connection.RemoteIpAddress?.ToString(),100),
            UserAgent=Clip(context?.Request.Headers.UserAgent.ToString(),2000)
        };
        try
        {
            // A separate context/transaction preserves failed and rejected responses too.
            using var scope=scopes.CreateScope();
            var db=scope.ServiceProvider.GetRequiredService<TopstarDbContext>();
            db.VnPayCallbackLogs.Add(entry);
            await db.SaveChangesAsync(CancellationToken.None);
            return entry.Id;
        }
        catch(Exception ex){logger.LogError(ex,"Could not persist VNPay callback journal {LogId}. Apply SQL 160 if missing.",entry.Id);return null;}
    }

    public async Task FinishAsync(Guid? id,bool signatureValid,VnPayResult? result,Exception? error=null)
    {
        if(id is null)return;
        try
        {
            using var scope=scopes.CreateScope();
            var db=scope.ServiceProvider.GetRequiredService<TopstarDbContext>();
            var finished=DateTime.UtcNow;
            var detail=Clip(error?.GetBaseException().Message,4000);
            var message=Clip(result?.Message??"Lỗi xử lý giao dịch; chưa hoàn tất cập nhật.",2000);
            var succeeded=(bool?)(result?.Success??false);
            var responseCode=result?.RspCode??"99";
            await db.VnPayCallbackLogs.Where(x=>x.Id==id.Value).ExecuteUpdateAsync(setters=>setters
                .SetProperty(x=>x.SignatureValid,(bool?)signatureValid)
                .SetProperty(x=>x.Success,succeeded)
                .SetProperty(x=>x.ResponseCode,responseCode)
                .SetProperty(x=>x.ResultMessage,message).SetProperty(x=>x.ErrorDetail,detail)
                .SetProperty(x=>x.ProcessedAtUtc,(DateTime?)finished),CancellationToken.None);
        }
        catch(Exception ex){logger.LogError(ex,"Could not complete VNPay callback journal {LogId}.",id);}
    }

    public async Task<IReadOnlyDictionary<string,IReadOnlyList<VnPayCallbackLogEntity>>> ReadAsync(IEnumerable<string> targetReferences,CancellationToken ct)
    {
        var targets=targetReferences.Distinct().ToArray();
        if(targets.Length==0)return new Dictionary<string,IReadOnlyList<VnPayCallbackLogEntity>>();
        using var scope=scopes.CreateScope();
        var db=scope.ServiceProvider.GetRequiredService<TopstarDbContext>();
        try
        {
            var entries=await db.VnPayCallbackLogs.AsNoTracking().Where(x=>x.TargetReference!=null&&targets.Contains(x.TargetReference))
                .OrderByDescending(x=>x.ReceivedAtUtc).ToListAsync(ct);
            return entries.GroupBy(x=>x.TargetReference!).ToDictionary(x=>x.Key,x=>(IReadOnlyList<VnPayCallbackLogEntity>)x.ToList());
        }
        catch(SqlException ex) when(ex.Number==208){logger.LogWarning("VNPay callback logs table is missing. Apply SQL 160.");return new Dictionary<string,IReadOnlyList<VnPayCallbackLogEntity>>();}
    }
}

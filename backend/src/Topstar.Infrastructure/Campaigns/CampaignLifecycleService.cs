using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Finance;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Campaigns;

public sealed record CampaignLifecycleResult(bool Success, string Message, bool Changed=false);

public sealed class CampaignLifecycleService(TopstarDbContext db)
{
    public async Task<CampaignLifecycleResult> ChangeAsync(Guid user, Guid id, string action, string? reason, CancellationToken ct=default)
    {
        reason=(reason??"").Trim();
        if(action is not ("Pause" or "Resume" or "Cancel") || reason.Length is <3 or >500)
            return new(false,"Chọn thao tác và nhập ghi chú từ 3 đến 500 ký tự.");
        if(action=="Cancel"&&reason.Length<10)return new(false,"Nhập lý do hủy từ 10 đến 500 ký tự để các bên có đủ thông tin đối soát.");
        await using var tx=await db.Database.BeginTransactionAsync(IsolationLevel.Serializable,ct);
        var c=await db.Campaigns.FromSqlInterpolated($"SELECT * FROM dbo.Campaigns WITH(UPDLOCK,HOLDLOCK) WHERE Id={id}").SingleOrDefaultAsync(ct);
        if(c is null || !await db.BrandProfiles.AnyAsync(b=>b.Id==c.BrandProfileId&&b.UserId==user,ct))
            return new(false,"Không tìm thấy chiến dịch hoặc không có quyền thao tác.");
        if(action=="Cancel"&&c.Status==CampaignStatuses.Cancelled || action=="Pause"&&c.Status==CampaignStatuses.Paused)
            return new(true,"Trạng thái này đã được ghi nhận.");
        var participants=await db.Participations.Where(p=>p.CampaignId==id).ToListAsync(ct);
        var ids=participants.Select(p=>p.Id).ToArray();
        var started=participants.Any(p=>p.WorkOpenedAtUtc.HasValue)
            || await db.Schedules.AnyAsync(s=>ids.Contains(s.ParticipationId)&&s.Status!="Cancelled",ct)
            || await db.Shipments.AnyAsync(s=>ids.Contains(s.ParticipationId)&&s.Status!="NotCreated",ct)
            || await db.Submissions.AnyAsync(s=>ids.Contains(s.ParticipationId)&&s.Status!="NotSubmitted",ct)
            || await db.WalletTransactions.AnyAsync(w=>w.CampaignId==id,ct)
            || await db.DisputeCases.AnyAsync(d=>ids.Contains(d.ParticipationId)&&d.IsActive,ct);
        var from=c.Status;
        var now=DateTime.UtcNow;
        if(action=="Resume")
        {
            var previous=await db.CampaignStatusHistories.Where(h=>h.CampaignId==id&&h.ToStatus==CampaignStatuses.Paused)
                .OrderByDescending(h=>h.CreatedAtUtc).Select(h=>h.FromStatus).FirstOrDefaultAsync(ct);
            if(!CampaignLifecycleRules.CanResume(c.Status,previous))return new(false,"Chiến dịch không ở trạng thái có thể tiếp tục.");
            c.Status=previous!;
        }
        else if(action=="Pause")
        {
            if(!CampaignLifecycleRules.CanPause(c.Status,started))return new(false,"Chỉ tự tạm ngừng trước khi mở công việc, đặt lịch hoặc giao sản phẩm. Vui lòng liên hệ hỗ trợ để đối soát nếu đã triển khai.");
            c.Status=CampaignStatuses.Paused;
        }
        else
        {
            var funds=await db.CampaignFundings.FromSqlInterpolated($"SELECT * FROM dbo.CampaignFundings WITH(UPDLOCK,HOLDLOCK) WHERE CampaignId={id}").ToListAsync(ct);
            var pendingFundIds=funds.Where(f=>f.Status=="Pending").Select(f=>f.Id).ToArray();
            var uncertain=funds.Any(f=>f.Status=="Pending"&&(f.PaymentMethod=="OnePay"||f.OnlinePaymentStartedAtUtc.HasValue||f.PaymentProofSubmittedAtUtc.HasValue))
                || await db.CampaignFundingEvidences.AnyAsync(e=>pendingFundIds.Contains(e.FundingId)&&e.EvidenceType=="Reconciliation",ct);
            // Paid funding is already reconciled; only evidence for an unresolved request blocks cancellation.
            if(!funds.Any(f=>f.Status=="Pending"))uncertain=false;
            if(!CampaignLifecycleRules.CanCancel(c.Status,started,uncertain))return new(false,"Chưa thể tự hủy: chiến dịch đã triển khai hoặc có thanh toán đang đối soát. Vui lòng liên hệ hỗ trợ; các khoản ký quỹ và thù lao vẫn được giữ nguyên.");
            foreach(var f in funds.Where(f=>f.Status is "Draft" or "Pending"))
            {
                if(f.BalanceAppliedAmount>0)
                {
                    var account=await BrandBalanceService.LockAsync(db,c.BrandProfileId,ct);
                    await BrandBalanceService.WriteAsync(db,account,f.BalanceAppliedAmount,0,"FundingCancelled",f.Id,Guid.NewGuid(),"Trả số dư đã dùng cho yêu cầu chưa hoàn tất khi hủy chiến dịch.",user,ct);
                    db.CampaignFundLedgers.Add(new(){Id=Guid.NewGuid(),FundingId=f.Id,EntryType="BalancePaymentReversed",Amount=-f.BalanceAppliedAmount,ActorUserId=user,CreatedAtUtc=now});
                }
                f.Status="Cancelled";f.CancelledAtUtc=now;f.CancellationReason=reason;f.UpdatedAtUtc=now;f.UpdatedByUserId=user;
            }
            foreach(var p in participants.Where(p=>p.Status is "Invited" or "Confirmed"))
            {
                db.ParticipationStatusHistories.Add(new(){Id=Guid.NewGuid(),ParticipationId=p.Id,FromStatus=p.Status,ToStatus="Cancelled",Note=reason,ActorUserId=user,CreatedAtUtc=now});
                p.Status="Cancelled";p.UpdatedAtUtc=now;
            }
            var applications=await db.Applications.Where(a=>a.CampaignId==id&&(a.Status=="Applied"||a.Status=="Shortlisted"||a.Status=="Confirmed")).ToListAsync(ct);
            foreach(var a in applications)
            {
                db.ApplicationStatusHistories.Add(new(){Id=Guid.NewGuid(),ApplicationId=a.Id,FromStatus=a.Status,ToStatus="NotSelected",Note=reason,ActorUserId=user,CreatedAtUtc=now});
                a.Status="NotSelected";a.UpdatedAtUtc=now;
            }
            c.Status=CampaignStatuses.Cancelled;c.CancelledAtUtc=now;c.CancelledByUserId=user;c.CancellationReason=reason;
        }
        c.UpdatedAtUtc=now;
        db.CampaignStatusHistories.Add(new(){Id=Guid.NewGuid(),CampaignId=id,FromStatus=from,ToStatus=c.Status,Note=reason,ActorUserId=user,CreatedAtUtc=now});
        db.AuditLogs.Add(new(){ActorUserId=user,EntityName="Campaign",EntityId=id.ToString(),ActionName=action,PayloadJson=JsonSerializer.Serialize(new{From=from,To=c.Status,Reason=reason}),CreatedAtUtc=now});
        var title=action switch{"Pause"=>"Chiến dịch tạm ngừng","Resume"=>"Chiến dịch tiếp tục",_=>"Chiến dịch đã hủy"};
        var message=$"{c.Title}: {reason}"+(action=="Cancel"?" Tiền ký quỹ được giữ để Admin đối soát nghĩa vụ và duyệt hoàn vào số dư Brand.":" Tiền ký quỹ và xác nhận tham gia được giữ nguyên. Brand sẽ chủ động mở công việc khi tiếp tục.");
        var recipients=await db.KocProfiles.Where(k=>participants.Select(p=>p.KocProfileId).Contains(k.Id)).Select(k=>k.UserId).Distinct().ToListAsync(ct);
        foreach(var recipient in recipients)Notify(recipient,"KOC",title,message,id,now);
        Notify(user,"BRAND",title,message,id,now);
        var admins=await (from ur in db.UserRoles join r in db.Roles on ur.RoleId equals r.Id where r.Code=="Admin"||r.Code=="ADMIN" select ur.UserId).Distinct().ToListAsync(ct);
        foreach(var admin in admins)Notify(admin,"ADMIN",title,message,id,now);
        await db.SaveChangesAsync(ct);await tx.CommitAsync(ct);
        return new(true,action=="Cancel"?"Đã hủy chiến dịch. Khoản ký quỹ đã nhận sẽ được Admin đối soát trước khi hoàn vào số dư Brand.":action=="Pause"?"Đã tạm ngừng chiến dịch. Tiền ký quỹ được giữ nguyên.":"Đã tiếp tục chiến dịch. Công việc chỉ bắt đầu khi Brand chủ động mở.",true);
    }
    private void Notify(Guid user,string role,string title,string body,Guid campaign,DateTime now)
        =>db.Notifications.Add(new(){Id=Guid.NewGuid(),UserId=user,AudienceRole=role,Type="Campaign",Title=title,Body=body,SourceEntityType="Campaign",SourceEntityId=campaign,IsVisible=true,TargetUrl=role=="BRAND"?$"/Brand/CampaignWorkspace?id={campaign}":role=="ADMIN"?"/Admin/Finance/BrandBalances":"/Koc/Collaborations",CreatedAtUtc=now});
}

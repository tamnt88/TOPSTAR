using Microsoft.EntityFrameworkCore;
using Topstar.Application.Collaborations;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Collaborations;

public sealed class CollaborationHistoryService(TopstarDbContext db):ICollaborationHistoryService
{
    public Task<IReadOnlyList<CollaborationItem>> GetForBrandAsync(Guid userId,Guid? campaignId=null,CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null)=>LoadAsync("Brand",userId,campaignId,ct,window);
    public Task<IReadOnlyList<CollaborationItem>> GetForKocAsync(Guid userId,CancellationToken ct=default)=>LoadAsync("KOC",userId,null,ct);
    public Task<IReadOnlyList<CollaborationItem>> GetForAdminAsync(CancellationToken ct=default, Topstar.Application.Common.ListWindow? window = null)=>LoadAsync("Admin",null,null,ct,window);

    public async Task<CollaborationPage> GetKocPageAsync(
        Guid userId,string? search,string? status,int skip,int take,CancellationToken ct=default)
    {
        var baseQuery=BuildQuery("KOC",userId);
        var total=await baseQuery.CountAsync(ct);
        var filtered=baseQuery;
        if(!string.IsNullOrWhiteSpace(search))
        {
            var keyword=search.Trim();
            filtered=filtered.Where(x=>x.CampaignTitle.Contains(keyword)||x.BrandName.Contains(keyword));
        }
        if(!string.IsNullOrWhiteSpace(status))
        {
            filtered=filtered.Where(x=>x.Status==status);
        }
        var filteredCount=await filtered.CountAsync(ct);
        var rows=await filtered.OrderByDescending(x=>x.ConfirmedAtUtc)
            .Skip(Math.Max(0,skip)).Take(Math.Clamp(take,10,50)).ToListAsync(ct);
        return new CollaborationPage(total,filteredCount,await HydrateAsync(rows,ct));
    }

    private async Task<IReadOnlyList<CollaborationItem>> LoadAsync(string scope,Guid? userId,Guid? campaignId,CancellationToken ct,Topstar.Application.Common.ListWindow? window=null)
    {
        var query=BuildQuery(scope,userId);
        if(campaignId.HasValue)query=query.Where(x=>x.CampaignId==campaignId.Value);
        var rows=await query.OrderByDescending(x=>x.ConfirmedAtUtc).ThenBy(x=>x.Id).ToWindowAsync(window,ct,q=>q.Where(x=>(window!.Search==null||x.CampaignTitle.Contains(window.Search)||x.BrandName.Contains(window.Search)||x.KocName.Contains(window.Search))&&(window.Status==null||x.Status==window.Status)),x=>x.Status);
        return await HydrateAsync(rows,ct);
    }

    private IQueryable<CollaborationRow> BuildQuery(string scope,Guid? userId)
    {
        var query=from p in db.Participations.AsNoTracking()
            join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id
            join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
            join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id
            select new CollaborationRow
            {
                Id=p.Id,CampaignId=c.Id,CampaignTitle=c.Title,BrandName=b.CompanyName,BrandUserId=b.UserId,
                KocName=k.DisplayName,KocUserId=k.UserId,Status=p.Status,ConfirmedAtUtc=p.ConfirmedAtUtc
            };
        if(scope=="Brand")query=query.Where(x=>x.BrandUserId==userId);
        else if(scope=="KOC")query=query.Where(x=>x.KocUserId==userId);
        return query;
    }

    private async Task<IReadOnlyList<CollaborationItem>> HydrateAsync(IReadOnlyList<CollaborationRow> rows,CancellationToken ct)
    {
        var ids=rows.Select(x=>x.Id).ToArray();if(ids.Length==0)return[];
        var events=new List<RawEvent>();
        events.AddRange(await db.ParticipationStatusHistories.AsNoTracking().Where(x=>ids.Contains(x.ParticipationId)).Select(x=>new RawEvent(x.ParticipationId,"Tham gia",x.ToStatus,x.Note,x.ActorUserId,x.CreatedAtUtc)).ToListAsync(ct));
        events.AddRange(await(from h in db.ScheduleStatusHistories.AsNoTracking() join s in db.Schedules.AsNoTracking() on h.ScheduleId equals s.Id where ids.Contains(s.ParticipationId) select new RawEvent(s.ParticipationId,"Booking",h.ToStatus,h.Note,h.ActorUserId,h.CreatedAtUtc)).ToListAsync(ct));
        events.AddRange(await(from h in db.ShipmentStatusHistories.AsNoTracking() join s in db.Shipments.AsNoTracking() on h.ShipmentId equals s.Id where ids.Contains(s.ParticipationId) select new RawEvent(s.ParticipationId,"Giao hàng",h.ToStatus,h.Note,h.ActorUserId,h.CreatedAtUtc)).ToListAsync(ct));
        events.AddRange(await(from h in db.SubmissionHistories.AsNoTracking() join s in db.Submissions.AsNoTracking() on h.SubmissionId equals s.Id join d in db.CampaignDeliverables.AsNoTracking() on s.DeliverableId equals d.Id where ids.Contains(s.ParticipationId) select new RawEvent(s.ParticipationId,"Nội dung · "+d.DeliverableType,h.Status,h.ReviewComment,h.ActorUserId,h.CreatedAtUtc)).ToListAsync(ct));
        events.AddRange(await(from h in db.DisputeCaseStatusHistories.AsNoTracking() join d in db.DisputeCases.AsNoTracking() on h.DisputeCaseId equals d.Id where ids.Contains(d.ParticipationId) select new RawEvent(d.ParticipationId,"Tranh chấp",h.ToStatus,h.Note,h.ActorUserId,h.CreatedAtUtc)).ToListAsync(ct));
        var allocations=await db.CampaignFundingAllocations.AsNoTracking().Where(x=>ids.Contains(x.ParticipationId))
            .Select(x=>new{x.ParticipationId,x.Status,x.Amount,x.AcceptedAmount,x.CreatedAtUtc,x.ReleasedAtUtc,x.SettlementReason}).ToListAsync(ct);
        events.AddRange(allocations.Select(x=>new RawEvent(x.ParticipationId,"Ký quỹ","Funded",$"Thù lao đã chốt: {x.Amount:N0} đ",null,x.CreatedAtUtc)));
        events.AddRange(allocations.Where(x=>x.ReleasedAtUtc.HasValue).Select(x=>new RawEvent(x.ParticipationId,"Đối soát",x.Status,$"Thực nhận: {x.AcceptedAmount:N0} đ. {x.SettlementReason}".Trim(),null,x.ReleasedAtUtc!.Value)));
        var actorIds=events.Where(x=>x.ActorUserId.HasValue).Select(x=>x.ActorUserId!.Value).Distinct().ToArray();var actorRows=await(from ur in db.UserRoles.AsNoTracking() join role in db.Roles.AsNoTracking() on ur.RoleId equals role.Id where actorIds.Contains(ur.UserId) select new{ur.UserId,role.Code}).ToListAsync(ct);var actors=actorRows.GroupBy(x=>x.UserId).ToDictionary(x=>x.Key,x=>string.Join("/",x.Select(r=>r.Code).OrderBy(c=>c)));
        return rows.Select(row=>
        {
            var rowEvents=events.Where(x=>x.ParticipationId==row.Id).OrderByDescending(x=>x.OccurredAtUtc).ToList();
            var operation=ResolveOperation(row.Status,rowEvents);
            return new CollaborationItem(row.Id,row.CampaignTitle,row.BrandName,row.KocName,row.Status,row.ConfirmedAtUtc,rowEvents.Select(x=>new CollaborationEvent(x.Domain,x.Status,x.Note,x.ActorUserId.HasValue?actors.GetValueOrDefault(x.ActorUserId.Value):null,x.OccurredAtUtc)).ToList())
            { CurrentStep=operation.Step,WaitingFor=operation.WaitingFor,NextAction=operation.NextAction };
        }).ToList();
    }
    private static (string Step,string WaitingFor,string NextAction) ResolveOperation(string participationStatus,IReadOnlyList<RawEvent> events)
    {
        if(participationStatus is "Completed")return("Hoàn tất","Không còn hành động","Chiến dịch đã hoàn tất.");
        if(participationStatus is "Cancelled" or "Rejected")return("Đã dừng","Không còn hành động","Hành trình hợp tác đã kết thúc.");
        if(events.Any(x=>x.Domain=="Tranh chấp"&&x.Status is not ("Resolved" or "Closed" or "Cancelled")))return("Xử lý sự cố","Brand/Admin","Theo dõi phản hồi và bổ sung bằng chứng khi được yêu cầu.");
        if(events.Any(x=>x.Domain=="Đối soát"))return("Đối soát","Không còn hành động","Khoản thù lao đã được nghiệm thu.");
        var content=events.FirstOrDefault(x=>x.Domain.StartsWith("Nội dung"));
        if(content?.Status is "Submitted")return("Duyệt nội dung","Brand/Admin","Chờ Brand hoặc quản trị viên duyệt nội dung.");
        if(content?.Status is "NotSubmitted" or "NeedRevision" or "Rejected")return("Nội dung","KOC","Gửi hoặc chỉnh sửa nội dung theo phản hồi.");
        if(content?.Status is "Approved")return("Đăng bài & đối soát","KOC","Hoàn tất đăng bài và chờ nghiệm thu.");
        var booking=events.FirstOrDefault(x=>x.Domain=="Booking");
        if(booking?.Status is "Proposed" or "Rescheduled")return("Booking","KOC","Xác nhận hoặc đề xuất lại lịch booking.");
        if(booking?.Status is "Confirmed")return("Booking","KOC","Thực hiện booking đúng lịch đã xác nhận.");
        var shipment=events.FirstOrDefault(x=>x.Domain=="Giao hàng");
        if(shipment?.Status is "InTransit")return("Nhận hàng","KOC","Kiểm tra và xác nhận khi đã nhận hàng.");
        if(shipment?.Status is "Issue" or "Returned")return("Xử lý giao hàng","Brand/Admin","Chờ xử lý sự cố hoặc phương án giao lại.");
        if(shipment?.Status is "Created")return("Giao hàng","Brand/Admin","Chờ Brand bàn giao sản phẩm cho đơn vị vận chuyển.");
        if(events.Any(x=>x.Domain=="Ký quỹ"))return("Chuẩn bị thực hiện","Brand/Admin","Chờ lịch booking, giao hàng hoặc đầu việc nội dung.");
        return("Ký quỹ","Brand/Admin","Chờ Brand hoàn tất chốt thù lao và ký quỹ.");
    }
    private sealed class CollaborationRow
    {
        public Guid Id{get;init;}
        public Guid CampaignId{get;init;}
        public string CampaignTitle{get;init;}=string.Empty;
        public string BrandName{get;init;}=string.Empty;
        public Guid BrandUserId{get;init;}
        public string KocName{get;init;}=string.Empty;
        public Guid KocUserId{get;init;}
        public string Status{get;init;}=string.Empty;
        public DateTime ConfirmedAtUtc{get;init;}
    }
    private sealed record RawEvent(Guid ParticipationId,string Domain,string Status,string? Note,Guid? ActorUserId,DateTime OccurredAtUtc);
}

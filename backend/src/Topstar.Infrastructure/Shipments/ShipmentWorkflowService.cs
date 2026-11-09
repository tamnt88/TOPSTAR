using System.Data;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Campaigns;
using Topstar.Application.Shipments;
using Topstar.Application.Billing;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Notifications;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Shipments;

public sealed class ShipmentWorkflowService(TopstarDbContext db) : IShipmentWorkflowService
{
    public Task<IReadOnlyList<ShipmentWorkItem>> GetBrandItemsForCampaignAsync(Guid brandUserId, Guid campaignId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null)
        => LoadBrandAsync(brandUserId, campaignId, cancellationToken, window);

    public async Task<IReadOnlyList<ShipmentWorkItem>> GetBrandItemsAsync(Guid brandUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null)
        => await LoadBrandAsync(brandUserId, null, cancellationToken, window);

    private async Task<IReadOnlyList<ShipmentWorkItem>> LoadBrandAsync(Guid brandUserId, Guid? campaignId, CancellationToken cancellationToken, Topstar.Application.Common.ListWindow? window = null)
    {
        var rows = await (from p in db.Participations.AsNoTracking()
                          join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id
                          join b in db.BrandProfiles.AsNoTracking() on c.BrandProfileId equals b.Id
                          join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id
                          join s0 in db.Shipments.AsNoTracking() on p.Id equals s0.ParticipationId into shipments
                          from s in shipments.DefaultIfEmpty()
                          where b.UserId == brandUserId && (!campaignId.HasValue || c.Id == campaignId.Value) && c.RequiresShipment
                              && (p.Status == "Confirmed" || (p.Status == "Completed" && s != null))
                          orderby s == null ? 0 : s.Status == ShipmentStatuses.Issue ? 0 : 1, c.Title, k.DisplayName
                          select new { Shipment = s, ParticipationId = p.Id, c.Title, KocName = k.DisplayName, k.UserId, k.StreetAddress, k.AddressV2, k.WardId, k.ProvinceId }).ToWindowAsync(window, cancellationToken, q => q.Where(x => (window!.Search == null || x.Title.Contains(window.Search) || x.KocName.Contains(window.Search) || (x.Shipment != null && ((x.Shipment.Carrier != null && x.Shipment.Carrier.Contains(window.Search)) || (x.Shipment.TrackingCode != null && x.Shipment.TrackingCode.Contains(window.Search))))) && (window.Status == null || (x.Shipment == null ? "NotCreated" : x.Shipment.Status) == window.Status || (window.Status == "preparation" && (x.Shipment == null || x.Shipment.Status == "Created")) || (window.Status == "exception" && x.Shipment != null && (x.Shipment.Status == "Issue" || x.Shipment.Status == "Returned")))), x => x.Shipment == null ? "NotCreated" : x.Shipment.Status);
        return await MapAsync(rows.Select(x => new Raw(x.Shipment, x.ParticipationId, x.Title, x.KocName, x.UserId, x.StreetAddress, x.AddressV2, x.WardId, x.ProvinceId)).ToList(), cancellationToken);
    }

    public async Task<IReadOnlyList<ShipmentWorkItem>> GetKocItemsAsync(Guid kocUserId, CancellationToken cancellationToken = default, Topstar.Application.Common.ListWindow? window = null)
    {
        var rows = await (from p in db.Participations.AsNoTracking()
                          join c in db.Campaigns.AsNoTracking() on p.CampaignId equals c.Id
                          join k in db.KocProfiles.AsNoTracking() on p.KocProfileId equals k.Id
                          join s in db.Shipments.AsNoTracking() on p.Id equals s.ParticipationId
                          where k.UserId == kocUserId && c.RequiresShipment
                          orderby s.Status == ShipmentStatuses.Issue ? 0 : 1, s.CreatedAtUtc descending
                          select new { Shipment = s, ParticipationId = p.Id, c.Title, KocName = k.DisplayName, k.UserId, k.StreetAddress, k.AddressV2, k.WardId, k.ProvinceId }).ToWindowAsync(window, cancellationToken, q => q.Where(x => (window!.Search == null || x.Title.Contains(window.Search) || x.KocName.Contains(window.Search) || (x.Shipment != null && ((x.Shipment.Carrier != null && x.Shipment.Carrier.Contains(window.Search)) || (x.Shipment.TrackingCode != null && x.Shipment.TrackingCode.Contains(window.Search))))) && (window.Status == null || (x.Shipment == null ? "NotCreated" : x.Shipment.Status) == window.Status || (window.Status == "preparation" && (x.Shipment == null || x.Shipment.Status == "Created")) || (window.Status == "exception" && x.Shipment != null && (x.Shipment.Status == "Issue" || x.Shipment.Status == "Returned")))), x => x.Shipment == null ? "NotCreated" : x.Shipment.Status);
        return await MapAsync(rows.Select(x => new Raw(x.Shipment, x.ParticipationId, x.Title, x.KocName, x.UserId, x.StreetAddress, x.AddressV2, x.WardId, x.ProvinceId)).ToList(), cancellationToken);
    }

    public async Task<ShipmentResult> CreateAsync(Guid brandUserId, Guid participationId, CancellationToken cancellationToken = default)
    {
        var row = await (from p in db.Participations
                         join c in db.Campaigns on p.CampaignId equals c.Id
                         join b in db.BrandProfiles on c.BrandProfileId equals b.Id
                         join k in db.KocProfiles on p.KocProfileId equals k.Id
                         join u in db.Users on k.UserId equals u.Id
                         where p.Id == participationId && b.UserId == brandUserId
                         select new { Participation = p, Campaign = c, Koc = k, User = u }).FirstOrDefaultAsync(cancellationToken);
        if (row is null) return Fail("Không tìm thấy lượt tham gia hoặc bạn không có quyền tạo vận đơn.");
        if (!row.Campaign.RequiresShipment || row.Participation.Status != "Confirmed") return Fail("Chỉ tạo vận đơn cho KOC đã xác nhận trong campaign có giao sản phẩm.");
        if (!await (from allocation in db.CampaignFundingAllocations.AsNoTracking()
                    join funding in db.CampaignFundings.AsNoTracking() on allocation.FundingId equals funding.Id
                    where allocation.ParticipationId == participationId
                       && (funding.Status == CampaignFundingStatuses.Paid
                           || funding.Status == CampaignFundingStatuses.PartiallyReleased
                           || funding.Status == CampaignFundingStatuses.Released)
                    select allocation.Id).AnyAsync(cancellationToken))
            return Fail("Khoản thù lao của KOC này chưa được ký quỹ nên chưa thể tạo vận đơn.");
        if (await db.Shipments.AnyAsync(x => x.ParticipationId == participationId, cancellationToken)) return Fail("Lượt tham gia này đã có vận đơn.");
        var address = Clean(row.Koc.AddressV2) ?? Clean(row.Koc.StreetAddress);
        if (address is null || string.IsNullOrWhiteSpace(row.User.Phone)) return Fail("KOC cần hoàn thiện địa chỉ và số điện thoại trước khi tạo vận đơn.");

        var now = DateTime.UtcNow;
        var shipment = new ShipmentEntity { Id = Guid.NewGuid(), ParticipationId = participationId, ReceiverName = row.Koc.DisplayName, ReceiverPhone = row.User.Phone, StreetAddress = row.Koc.StreetAddress, AddressV2 = row.Koc.AddressV2, WardId = row.Koc.WardId, ProvinceId = row.Koc.ProvinceId, IsNewAddress = !string.IsNullOrWhiteSpace(row.Koc.AddressV2), Status = ShipmentStatuses.Created, CreatedAtUtc = now };
        db.Shipments.Add(shipment);
        History(shipment.Id, null, ShipmentStatuses.Created, "Đã chốt thông tin nhận hàng", brandUserId, now);
        Notify(row.Koc.UserId, "KOC", "Shipment", "Brand đã tạo vận đơn", $"Kiểm tra thông tin nhận hàng cho chiến dịch {row.Campaign.Title}.", shipment.Id, now);
        Audit(brandUserId, shipment.Id, "Create", new { participationId }, now);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Fail("Vận đơn vừa được tạo ở phiên khác. Vui lòng tải lại trang."); }
        return Ok("Đã tạo vận đơn và lưu snapshot địa chỉ nhận hàng.");
    }

    public Task<ShipmentResult> DispatchAsync(Guid brandUserId, Guid shipmentId, string carrier, string trackingCode, CancellationToken cancellationToken = default)
        => BrandTransitionAsync(brandUserId, shipmentId, ShipmentStatuses.Created, ShipmentStatuses.InTransit, Clean(carrier), Clean(trackingCode), null, cancellationToken);

    public async Task<ShipmentResult> ReportIssueAsync(Guid kocUserId, Guid shipmentId, string issueNote, CancellationToken cancellationToken = default)
    {
        var note = Clean(issueNote);
        if (!ShipmentEvidenceRules.IsValidIssueNote(note)) return Fail("Vui lòng mô tả sự cố từ 10 đến 1.000 ký tự.");
        var row = await OwnedByKoc(shipmentId, kocUserId, cancellationToken);
        if (row is null) return Fail("Không tìm thấy vận đơn hoặc bạn không có quyền cập nhật.");
        if (!ShipmentTransitions.CanReportIssue(row.Shipment.Status)) return Fail("Trạng thái hiện tại không cho phép báo sự cố.");
        await Transition(row.Shipment, ShipmentStatuses.Issue, note!, kocUserId, row.BrandUserId, "BRAND", "KOC báo sự cố giao hàng", cancellationToken);
        return Ok("Đã gửi sự cố cho Brand xử lý.");
    }

    public async Task<ShipmentResult> ConfirmDeliveredAsync(Guid kocUserId, Guid shipmentId, string evidenceUrl, string receiptNote, CancellationToken cancellationToken = default)
    {
        evidenceUrl = Clean(evidenceUrl) ?? string.Empty;
        receiptNote = Clean(receiptNote) ?? string.Empty;
        if (!ShipmentEvidenceRules.IsValidStoredUrl(evidenceUrl))
            return Fail("Vui lòng tải ảnh nhận hàng hợp lệ.");
        if (!ShipmentEvidenceRules.IsValidReceiptNote(receiptNote))
            return Fail("Vui lòng nhập ghi chú nhận hàng từ 10 đến 500 ký tự.");
        var row = await OwnedByKoc(shipmentId, kocUserId, cancellationToken);
        if (row is null) return Fail("Không tìm thấy vận đơn hoặc bạn không có quyền cập nhật.");
        if (!ShipmentTransitions.CanConfirmDelivered(row.Shipment.Status)) return Fail("Chỉ xác nhận nhận hàng khi vận đơn đang vận chuyển.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var evidenceCount = await db.ShipmentEvidence.CountAsync(x => x.ShipmentId == shipmentId && x.ActorRole == "KOC", cancellationToken);
        if (!ShipmentEvidenceRules.CanAdd(evidenceCount)) return Fail($"Bạn đã lưu tối đa {ShipmentEvidenceRules.MaxPerRole} ảnh cho vận đơn.");
        var now = DateTime.UtcNow;
        db.ShipmentEvidence.Add(new()
        {
            Id = Guid.NewGuid(), ShipmentId = shipmentId, EvidenceType = "KocReceiptProof",
            FileUrl = evidenceUrl, Caption = receiptNote, UploadedByUserId = kocUserId,
            ActorRole = "KOC", CreatedAtUtc = now
        });
        await Transition(row.Shipment, ShipmentStatuses.Delivered, $"KOC xác nhận đã nhận hàng · {receiptNote}", kocUserId, row.BrandUserId, "BRAND", "KOC đã nhận hàng", cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok("Đã xác nhận nhận hàng.");
    }

    public async Task<ShipmentResult> ResolveIssueAsync(Guid brandUserId, Guid shipmentId, string decision, string note, CancellationToken cancellationToken = default)
    {
        decision = Clean(decision) ?? string.Empty; var cleanNote = Clean(note);
        if (!ShipmentTransitions.CanResolveIssue(ShipmentStatuses.Issue, decision) || !ShipmentEvidenceRules.IsValidIssueNote(cleanNote)) return Fail("Chọn tiếp tục vận chuyển hoặc hoàn hàng và nhập cách xử lý từ 10 đến 1.000 ký tự.");
        var row = await OwnedByBrand(shipmentId, brandUserId, cancellationToken);
        if (row is null) return Fail("Không tìm thấy vận đơn hoặc bạn không có quyền xử lý.");
        if (!ShipmentTransitions.CanResolveIssue(row.Shipment.Status, decision)) return Fail("Vận đơn không còn ở trạng thái sự cố.");
        await Transition(row.Shipment, decision, cleanNote!, brandUserId, row.KocUserId, "KOC", "Brand đã xử lý sự cố giao hàng", cancellationToken);
        return Ok(decision == ShipmentStatuses.Returned ? "Đã ghi nhận hoàn hàng." : "Đã đưa vận đơn về trạng thái đang vận chuyển.");
    }

    public async Task<ShipmentResult> AddEvidenceAsync(Guid userId, Guid shipmentId, string actorRole, string fileUrl, string? caption, CancellationToken cancellationToken = default)
    {
        actorRole = actorRole?.Trim().ToUpperInvariant() ?? string.Empty;
        caption = Clean(caption);
        if (!ShipmentEvidenceRules.IsAllowedRole(actorRole) || !ShipmentEvidenceRules.IsValidStoredUrl(fileUrl))
            return Fail("Ảnh bằng chứng không hợp lệ.");
        if (!ShipmentEvidenceRules.IsValidEvidenceCaption(caption))
            return Fail("Vui lòng nhập ghi chú ảnh từ 10 đến 500 ký tự.");
        var owned = actorRole == "BRAND"
            ? await OwnedByBrand(shipmentId, userId, cancellationToken)
            : await OwnedByKoc(shipmentId, userId, cancellationToken);
        if (owned is null) return Fail("Không tìm thấy vận đơn hoặc bạn không có quyền thêm bằng chứng.");
        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, cancellationToken);
        var count = await db.ShipmentEvidence.CountAsync(x => x.ShipmentId == shipmentId && x.ActorRole == actorRole, cancellationToken);
        if (!ShipmentEvidenceRules.CanAdd(count)) return Fail($"Mỗi bên chỉ được lưu tối đa {ShipmentEvidenceRules.MaxPerRole} ảnh cho một vận đơn.");
        var now = DateTime.UtcNow;
        var type = actorRole == "BRAND" ? "BrandShipmentProof"
            : owned.Shipment.Status == ShipmentStatuses.Issue ? "KocIssueProof" : "KocReceiptProof";
        var evidence = new ShipmentEvidenceEntity { Id = Guid.NewGuid(), ShipmentId = shipmentId, EvidenceType = type, FileUrl = fileUrl, Caption = caption, UploadedByUserId = userId, ActorRole = actorRole, CreatedAtUtc = now };
        db.ShipmentEvidence.Add(evidence);
        Audit(userId, shipmentId, "AddEvidence", new { EvidenceId = evidence.Id, type, HasCaption = caption is not null }, now);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return Ok("Đã lưu ảnh bằng chứng vào lịch sử vận đơn.");
    }

    public async Task<ShipmentResult> AddTrackingEventAsync(Guid brandUserId, Guid shipmentId, string eventCode, string description, string? location, DateTime occurredAtLocal, CancellationToken cancellationToken = default)
    {
        eventCode = Clean(eventCode) ?? string.Empty;
        description = Clean(description) ?? string.Empty;
        location = Clean(location);
        if (!await db.OperationalCatalogItems.AsNoTracking().AnyAsync(x=>x.CatalogType==Topstar.Application.Settings.OperationalCatalogTypes.ShipmentCheckpoint&&x.Code==eventCode&&x.IsActive,cancellationToken)) return Fail("Loại checkpoint không có trong cấu hình hoặc đang tạm ẩn.");
        if (description.Length is < 2 or > 1000 || location?.Length > 300) return Fail("Mô tả từ 2–1.000 ký tự; địa điểm tối đa 300 ký tự.");
        var row = await OwnedByBrand(shipmentId, brandUserId, cancellationToken);
        if (row is null) return Fail("Không tìm thấy vận đơn hoặc bạn không có quyền cập nhật tracking.");
        if (!ShipmentTrackingRules.CanAdd(row.Shipment.Status)) return Fail("Chỉ thêm checkpoint khi vận đơn đang giao hoặc đang có sự cố.");
        var occurredAtUtc = DateTime.SpecifyKind(occurredAtLocal, DateTimeKind.Unspecified).AddHours(-7);
        var now = DateTime.UtcNow;
        if (occurredAtUtc < row.Shipment.CreatedAtUtc || occurredAtUtc > now.AddMinutes(10))
            return Fail("Thời điểm checkpoint phải sau khi tạo vận đơn và không được ở tương lai.");
        var tracking = new ShipmentTrackingEventEntity { Id = Guid.NewGuid(), ShipmentId = shipmentId, EventCode = eventCode, Description = description, Location = location, OccurredAtUtc = occurredAtUtc, Source = "ManualBrand", CreatedByUserId = brandUserId, CreatedAtUtc = now };
        db.ShipmentTrackingEvents.Add(tracking);
        Notify(row.KocUserId, "KOC", "ShipmentTracking", "Vận đơn có cập nhật mới", description, shipmentId, now);
        Audit(brandUserId, shipmentId, "AddTrackingEvent", new { TrackingEventId = tracking.Id, eventCode, occurredAtUtc, HasLocation = location is not null }, now);
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException) { return Fail("Checkpoint này đã được ghi nhận hoặc dữ liệu không còn hợp lệ."); }
        return Ok("Đã thêm checkpoint vào hành trình vận đơn.");
    }

    private async Task<ShipmentResult> BrandTransitionAsync(Guid actor, Guid id, string from, string to, string? carrier, string? tracking, string? note, CancellationToken ct)
    {
        if (carrier is null || tracking is null || carrier.Length > 100 || tracking.Length > 100) return Fail("Nhập nhà vận chuyển và mã vận đơn, tối đa 100 ký tự.");
        var row = await OwnedByBrand(id, actor, ct); if (row is null) return Fail("Không tìm thấy vận đơn hoặc bạn không có quyền cập nhật.");
        if (!ShipmentTransitions.CanDispatch(row.Shipment.Status)) return Fail("Chỉ xuất kho vận đơn mới tạo hoặc vận đơn cần gửi lại.");
        row.Shipment.Carrier = carrier; row.Shipment.TrackingCode = tracking;
        await Transition(row.Shipment, to, note ?? $"{carrier} · {tracking}", actor, row.KocUserId, "KOC", "Đơn hàng đang được vận chuyển", ct);
        return Ok("Đã lưu mã vận đơn và chuyển sang đang vận chuyển.");
    }

    private async Task Transition(ShipmentEntity shipment, string to, string note, Guid actor, Guid notifyUser, string audienceRole, string title, CancellationToken ct)
    {
        var now = DateTime.UtcNow; var from = shipment.Status; shipment.Status = to; shipment.IssueNote = to == ShipmentStatuses.Issue ? note : null; shipment.UpdatedAtUtc = now;
        History(shipment.Id, from, to, note, actor, now); Notify(notifyUser, audienceRole, "Shipment", title, note, shipment.Id, now); Audit(actor, shipment.Id, to, new { FromStatus = from, Note = note }, now);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new ShipmentConcurrencyException(); }
    }

    private async Task<Owned?> OwnedByBrand(Guid id, Guid userId, CancellationToken ct) => await (from s in db.Shipments join p in db.Participations on s.ParticipationId equals p.Id join c in db.Campaigns on p.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id join k in db.KocProfiles on p.KocProfileId equals k.Id where s.Id == id && b.UserId == userId select new Owned(s, k.UserId, b.UserId)).FirstOrDefaultAsync(ct);
    private async Task<Owned?> OwnedByKoc(Guid id, Guid userId, CancellationToken ct) => await (from s in db.Shipments join p in db.Participations on s.ParticipationId equals p.Id join c in db.Campaigns on p.CampaignId equals c.Id join b in db.BrandProfiles on c.BrandProfileId equals b.Id join k in db.KocProfiles on p.KocProfileId equals k.Id where s.Id == id && k.UserId == userId select new Owned(s, k.UserId, b.UserId)).FirstOrDefaultAsync(ct);

    private async Task<IReadOnlyList<ShipmentWorkItem>> MapAsync(List<Raw> rows, CancellationToken ct)
    {
        var ids = rows.Where(x => x.Shipment is not null).Select(x => x.Shipment!.Id).ToArray();
        var histories = await db.ShipmentStatusHistories.AsNoTracking().Where(x => ids.Contains(x.ShipmentId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        var map = histories.GroupBy(x => x.ShipmentId).ToDictionary(x => x.Key, x => (IReadOnlyList<ShipmentHistoryItem>)x.Select(h => new ShipmentHistoryItem(h.FromStatus, h.ToStatus, h.Note, h.CreatedAtUtc)).ToList());
        var evidenceRows = await db.ShipmentEvidence.AsNoTracking().Where(x => ids.Contains(x.ShipmentId)).OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        var evidenceMap = evidenceRows.GroupBy(x => x.ShipmentId).ToDictionary(x => x.Key, x => (IReadOnlyList<ShipmentEvidenceItem>)x.Select(e => new ShipmentEvidenceItem(e.Id, e.EvidenceType, e.FileUrl, e.Caption, e.ActorRole, e.CreatedAtUtc)).ToList());
        var trackingRows = await db.ShipmentTrackingEvents.AsNoTracking().Where(x => ids.Contains(x.ShipmentId)).OrderByDescending(x => x.OccurredAtUtc).ToListAsync(ct);
        var trackingMap = trackingRows.GroupBy(x => x.ShipmentId).ToDictionary(x => x.Key, x => (IReadOnlyList<ShipmentTrackingItem>)x.Select(e => new ShipmentTrackingItem(e.Id, e.EventCode, e.Description, e.Location, e.OccurredAtUtc, e.Source, e.CreatedAtUtc)).ToList());
        return rows.Select(x => new ShipmentWorkItem(x.Shipment?.Id, x.ParticipationId, x.Title, x.KocName, x.Shipment?.ReceiverName ?? x.KocName, x.Shipment?.ReceiverPhone ?? "", x.Shipment is null ? (Clean(x.AddressV2) ?? Clean(x.StreetAddress) ?? "Chưa có địa chỉ") : (Clean(x.Shipment.AddressV2) ?? Clean(x.Shipment.StreetAddress) ?? "Chưa có địa chỉ"), x.Shipment?.Carrier, x.Shipment?.TrackingCode, x.Shipment?.Status ?? ShipmentStatuses.NotCreated, x.Shipment?.IssueNote, true, x.Shipment is null ? [] : map.GetValueOrDefault(x.Shipment.Id, []), x.Shipment is null ? [] : evidenceMap.GetValueOrDefault(x.Shipment.Id, []), x.Shipment is null ? [] : trackingMap.GetValueOrDefault(x.Shipment.Id, []))).ToList();
    }

    private void History(Guid id, string? from, string to, string? note, Guid actor, DateTime now) => db.ShipmentStatusHistories.Add(new() { Id = Guid.NewGuid(), ShipmentId = id, FromStatus = from, ToStatus = to, Note = note, ActorUserId = actor, CreatedAtUtc = now });
    private void Notify(Guid user, string audienceRole, string type, string title, string body, Guid sourceId, DateTime now) => db.Notifications.Add(NotificationFactory.Create(user,type,title,body,now,"Shipment",sourceId,audienceRole:audienceRole));
    private void Audit(Guid actor, Guid id, string action, object payload, DateTime now) => db.AuditLogs.Add(new() { ActorUserId = actor, EntityName = "Shipment", EntityId = id.ToString(), ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = now });
    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static ShipmentResult Ok(string message) => new(true, message); private static ShipmentResult Fail(string message) => new(false, message);
    private sealed record Owned(ShipmentEntity Shipment, Guid KocUserId, Guid BrandUserId);
    private sealed record Raw(ShipmentEntity? Shipment, Guid ParticipationId, string Title, string KocName, Guid KocUserId, string? StreetAddress, string? AddressV2, Guid? WardId, Guid? ProvinceId);
}

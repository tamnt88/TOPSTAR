using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Profiles;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Profiles;

public sealed class ProfileReviewService(TopstarDbContext db, IProfileAccessService profileAccessService) : IProfileReviewService
{
    public async Task<IReadOnlyList<ProfileReviewListItem>> GetQueueAsync(string? profileType, CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(profileType);
        var result = new List<ProfileReviewListItem>();
        if (type is null or "BRAND")
        {
            result.AddRange(await (from p in db.BrandProfiles.AsNoTracking()
                                   join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                                   where p.Status == ProfileStatuses.PendingReview
                                   select new ProfileReviewListItem(p.Id, "Brand", p.CompanyName, u.Email ?? "", u.Phone ?? "", p.Status, p.CreatedAtUtc))
                .ToListAsync(cancellationToken));
        }
        if (type is null or "KOC")
        {
            result.AddRange(await (from p in db.KocProfiles.AsNoTracking()
                                   join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                                   where p.Status == ProfileStatuses.PendingReview
                                   select new ProfileReviewListItem(p.Id, "KOC", p.DisplayName, u.Email ?? "", u.Phone ?? "", p.Status, p.CreatedAtUtc))
                .ToListAsync(cancellationToken));
        }
        return result.OrderBy(x => x.CreatedAtUtc).ToList();
    }

    public async Task<ProfileReviewDetail?> GetDetailAsync(string profileType, Guid profileId, CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(profileType);
        if (type == "BRAND")
        {
            var row = await (from p in db.BrandProfiles.AsNoTracking()
                          join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                          join province in db.Provinces.AsNoTracking() on p.ProvinceId equals province.Id into provinces
                          from province in provinces.DefaultIfEmpty()
                          join ward in db.Wards.AsNoTracking() on p.WardId equals ward.Id into wards
                          from ward in wards.DefaultIfEmpty()
                          where p.Id == profileId
                          select new { p.Id, p.CompanyName, u.Email, u.Phone, p.Status, p.ReviewNote, p.StreetAddress, ProvinceName = province != null ? province.Name : null, WardName = ward != null ? ward.Name : null, p.ContactName, p.TaxCode, p.Industry, p.CreatedAtUtc }).FirstOrDefaultAsync(cancellationToken);
            return row is null ? null : new(row.Id, "Brand", row.CompanyName, row.Email ?? "", row.Phone ?? "", row.Status, row.ReviewNote, row.StreetAddress, row.ProvinceName, row.WardName,
                $"Người liên hệ: {row.ContactName} · MST: {row.TaxCode} · Ngành: {row.Industry}",
                await db.BrandProfiles.AsNoTracking().Where(x => x.Id == profileId).Select(x => x.BusinessLicenseFileUrl).FirstOrDefaultAsync(cancellationToken),
                [$"Người liên hệ: {row.ContactName}", $"Mã số thuế: {row.TaxCode}", $"Ngành: {row.Industry}"], row.CreatedAtUtc);
        }
        if (type == "KOC")
        {
            var row = await (from p in db.KocProfiles.AsNoTracking()
                          join u in db.Users.AsNoTracking() on p.UserId equals u.Id
                          join province in db.Provinces.AsNoTracking() on p.ProvinceId equals province.Id into provinces
                          from province in provinces.DefaultIfEmpty()
                          join ward in db.Wards.AsNoTracking() on p.WardId equals ward.Id into wards
                          from ward in wards.DefaultIfEmpty()
                          where p.Id == profileId
                          select new { p.Id, p.UserId, p.DisplayName, p.AvatarUrl, p.Gender, u.Email, u.Phone, p.Status, p.ReviewNote, p.StreetAddress, ProvinceName = province != null ? province.Name : null, WardName = ward != null ? ward.Name : null, p.Followers, p.Score, p.Region, p.ViewCount, p.IsVisible, p.CreatedAtUtc }).FirstOrDefaultAsync(cancellationToken);
            if (row is null) return null;
            var body = await db.KocBodyProfiles.AsNoTracking().FirstOrDefaultAsync(x => x.KocProfileId == profileId, cancellationToken);
            var children = body is null
                ? []
                : await db.KocChildren.AsNoTracking().Where(x => x.KocBodyProfileId == body.Id)
                    .OrderByDescending(x => x.BirthYear).ThenBy(x => x.CreatedAtUtc)
                    .Select(x => new KocChildAdminDetail(x.AgeYears, x.Gender, x.BirthYear))
                    .ToListAsync(cancellationToken);
            var socialRows = await db.KocSocialAccounts.AsNoTracking().Where(x => x.KocProfileId == profileId)
                .OrderByDescending(x => x.IsPrimary).ThenByDescending(x => x.Followers)
                .Select(x => new KocSocialAdminDetail(x.Platform, x.ChannelUrl, x.Handle, x.Followers, x.AvgViews, x.AvgEngagementRate, x.IsPrimary, x.IsVerified, x.IsVisible))
                .ToListAsync(cancellationToken);
            var socials = socialRows.Select(x => $"{x.Platform}: {x.Handle} ({x.Followers:N0} followers)").ToList();
            var categoryNames = await (from pc in db.KocProfileCategories.AsNoTracking()
                                       join category in db.KocCategories.AsNoTracking() on pc.CategoryId equals category.Id
                                       where pc.KocProfileId == profileId select category.Name).Distinct().ToListAsync(cancellationToken);
            if (categoryNames.Count == 0)
            {
                categoryNames = await (from social in db.KocSocialAccounts.AsNoTracking()
                                       join sc in db.KocSocialAccountCategories.AsNoTracking() on social.Id equals sc.SocialAccountId
                                       join category in db.KocCategories.AsNoTracking() on sc.CategoryId equals category.Id
                                       where social.KocProfileId == profileId select category.Name).Distinct().ToListAsync(cancellationToken);
            }
            var documentUrl = await db.KocMediaAssets.AsNoTracking().Where(x => x.KocProfileId == profileId && x.MediaType == "ProfileDocument" && x.IsVisible)
                .OrderByDescending(x => x.CreatedAtUtc).Select(x => x.FileUrl).FirstOrDefaultAsync(cancellationToken);
            var experience = await db.KocExperiences.AsNoTracking().Where(x => x.KocProfileId == profileId)
                .Select(x => new KocExperienceAdminDetail(x.YearsOfExperience, x.WorkedBrands, x.CaseStudyUrl)).FirstOrDefaultAsync(cancellationToken);
            var production = await db.KocProductionCapabilities.AsNoTracking().Where(x => x.KocProfileId == profileId)
                .Select(x => new KocProductionAdminDetail(x.CanScript, x.CanShoot, x.CanEdit, x.CanLivestream, x.Devices)).FirstOrDefaultAsync(cancellationToken);
            var preference = await db.KocJobPreferences.AsNoTracking().Where(x => x.KocProfileId == profileId)
                .Select(x => new KocPreferenceAdminDetail(x.MinBudgetAmount, x.PreferredRegions, x.AvailabilityNote)).FirstOrDefaultAsync(cancellationToken);
            var portfolio = await db.KocPortfolios.AsNoTracking().Where(x => x.KocProfileId == profileId && x.IsVisible)
                .OrderByDescending(x => x.Views).Take(12)
                .Select(x => new KocPortfolioAdminDetail(x.Title, x.BrandName, x.CampaignName, x.PostUrl, x.Platform, x.Views, x.Likes, x.Comments, x.Shares))
                .ToListAsync(cancellationToken);
            var media = await db.KocMediaAssets.AsNoTracking().Where(x => x.KocProfileId == profileId && x.IsVisible && x.MediaType != "ProfileDocument")
                .OrderBy(x => x.SortOrder).ThenByDescending(x => x.CreatedAtUtc).Take(12)
                .Select(x => new KocMediaAdminDetail(x.MediaType, x.FileUrl, x.SortOrder)).ToListAsync(cancellationToken);
            var collaborations = await (from participation in db.Participations.AsNoTracking()
                                        join campaign in db.Campaigns.AsNoTracking() on participation.CampaignId equals campaign.Id
                                        join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                                        where participation.KocProfileId == profileId
                                        orderby participation.CreatedAtUtc descending
                                        select new KocHistoryItem(campaign.Title, brand.CompanyName, participation.Status, null, participation.CreatedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var applications = await (from application in db.Applications.AsNoTracking()
                                      join campaign in db.Campaigns.AsNoTracking() on application.CampaignId equals campaign.Id
                                      join brand in db.BrandProfiles.AsNoTracking() on campaign.BrandProfileId equals brand.Id
                                      where application.KocProfileId == profileId
                                      orderby application.AppliedAtUtc descending
                                      select new KocHistoryItem(campaign.Title, brand.CompanyName, application.Status, null, application.AppliedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var reviews = await (from review in db.Reviews.AsNoTracking()
                                 join campaign in db.Campaigns.AsNoTracking() on review.CampaignId equals campaign.Id
                                 where review.RevieweeUserId == row.UserId
                                 orderby review.CreatedAtUtc descending
                                 select new KocHistoryItem(campaign.Title, review.Comment, review.Status + " · " + review.Rating + "/5", null, review.CreatedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var finance = await (from transaction in db.WalletTransactions.AsNoTracking()
                                 join campaign in db.Campaigns.AsNoTracking() on transaction.CampaignId equals campaign.Id
                                 where transaction.KocProfileId == profileId
                                 orderby transaction.CreatedAtUtc descending
                                 select new KocHistoryItem(campaign.Title, transaction.Description ?? transaction.HoldReason, transaction.Status, transaction.Amount, transaction.CreatedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var violations = await db.UserViolations.AsNoTracking().Where(x => x.UserId == row.UserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new KocHistoryItem(x.Category, x.Reason, x.Status + " · " + x.Severity, null, x.CreatedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var accountEvents = await db.AuthEvents.AsNoTracking().Where(x => x.UserId == row.UserId)
                .OrderByDescending(x => x.OccurredAtUtc)
                .Select(x => new KocHistoryItem(x.EventType, x.FailureReason ?? x.IpAddress, x.EventStatus, null, x.OccurredAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var accountStatusEvents = await db.UserAccountStatusHistories.AsNoTracking().Where(x => x.UserId == row.UserId)
                .OrderByDescending(x => x.CreatedAtUtc)
                .Select(x => new KocHistoryItem("Trạng thái tài khoản", x.Reason, x.FromStatus + " → " + x.ToStatus, null, x.CreatedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var account = accountEvents.Concat(accountStatusEvents).OrderByDescending(x => x.OccurredAtUtc).Take(20).ToList();
            var socialSync = await db.SocialSyncJobs.AsNoTracking().Where(x => x.KocProfileId == profileId)
                .OrderByDescending(x => x.RequestedAtUtc)
                .Select(x => new KocHistoryItem(x.Scope, x.ErrorMessage, x.Status, null, x.RequestedAtUtc))
                .Take(20).ToListAsync(cancellationToken);
            var highlights = new List<string> { $"Followers tổng: {row.Followers:N0}", $"Điểm: {row.Score:N2}", $"Khu vực: {row.Region}" };
            if (body is not null) highlights.Add($"Thể trạng: {body.HeightCm} cm / {body.WeightKg} kg · {body.MaritalStatus}");
            highlights.AddRange(socials);
            if (categoryNames.Count > 0) highlights.Add($"Lĩnh vực: {string.Join(", ", categoryNames)}");
            var bodyDetail = body is null ? null : new KocBodyAdminDetail(body.HeightCm, body.WeightKg, body.BustCm, body.WaistCm, body.HipCm, body.MaritalStatus, body.HasChildren, body.ChildrenCount, children);
            var effectiveFollowers = row.Followers > 0 ? row.Followers : socialRows.Sum(x => x.Followers);
            var history = new KocHistoryAdminDetail(collaborations, applications, reviews, finance, violations, account, socialSync);
            var kocDetail = new KocAdminProfileDetail(
                row.AvatarUrl, row.Gender, row.Region, effectiveFollowers, row.Score, row.ViewCount, row.IsVisible,
                bodyDetail, categoryNames, socialRows, experience, production, preference, portfolio, media, history);
            return new(row.Id, "KOC", row.DisplayName, row.Email ?? "", row.Phone ?? "", row.Status, row.ReviewNote, row.StreetAddress, row.ProvinceName, row.WardName,
                $"Followers: {row.Followers:N0} · Điểm: {row.Score:N2} · Khu vực: {row.Region}", documentUrl, highlights, row.CreatedAtUtc, kocDetail);
        }
        return null;
    }

    public async Task<IReadOnlyList<ProfileStatusEvent>> GetHistoryAsync(string profileType, Guid profileId, CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(profileType);
        if (type == "BRAND")
            return await db.BrandProfileStatusHistories.AsNoTracking().Where(x => x.BrandProfileId == profileId).OrderBy(x => x.CreatedAtUtc)
                .Select(x => new ProfileStatusEvent(x.FromStatus, x.ToStatus, x.ReviewNote, x.CreatedAtUtc)).ToListAsync(cancellationToken);
        if (type == "KOC")
            return await db.KocProfileStatusHistories.AsNoTracking().Where(x => x.KocProfileId == profileId).OrderBy(x => x.CreatedAtUtc)
                .Select(x => new ProfileStatusEvent(x.FromStatus, x.ToStatus, x.ReviewNote, x.CreatedAtUtc)).ToListAsync(cancellationToken);
        return [];
    }

    public async Task<ProfileReviewResult> ReviewAsync(Guid adminUserId, string profileType, Guid profileId, string decision, string? reviewNote, CancellationToken cancellationToken = default)
    {
        var type = NormalizeType(profileType);
        var normalizedDecision = NormalizeDecision(decision);
        if (type is null || normalizedDecision is null || !ProfileTransitions.IsValidDecision(normalizedDecision))
            return new(false, "Yêu cầu duyệt không hợp lệ.");
        var cleanedReviewNote = reviewNote?.Trim();
        if (string.IsNullOrWhiteSpace(cleanedReviewNote) || cleanedReviewNote.Length is < 10 or > 1000)
            return new(false, "Vui lòng nhập thông tin và căn cứ xử lý từ 10 đến 1.000 ký tự.");

        string currentStatus;
        IReadOnlyList<string> approvedWithMissingFields = [];
        if (type == "BRAND")
        {
            var profile = await db.BrandProfiles.FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
            if (profile is null) return new(false, "Không tìm thấy hồ sơ Brand.");
            currentStatus = profile.Status;
            if (!ProfileTransitions.CanReview(currentStatus)) return InvalidState(currentStatus);
            if (normalizedDecision == ProfileStatuses.Approved)
            {
                var access = await profileAccessService.CheckBrandAsync(profile.UserId, cancellationToken);
                if (!access.IsComplete) return Incomplete(access.MissingFields);
            }
            profile.Status = normalizedDecision; profile.ReviewNote = cleanedReviewNote; profile.UpdatedAtUtc = DateTime.UtcNow;
        }
        else
        {
            var profile = await db.KocProfiles.FirstOrDefaultAsync(x => x.Id == profileId, cancellationToken);
            if (profile is null) return new(false, "Không tìm thấy hồ sơ KOC.");
            currentStatus = profile.Status;
            if (!ProfileTransitions.CanReview(currentStatus)) return InvalidState(currentStatus);
            if (normalizedDecision == ProfileStatuses.Approved)
            {
                var access = await profileAccessService.CheckKocAsync(profile.UserId, cancellationToken);
                if (!access.IsComplete) approvedWithMissingFields = access.MissingFields;
                profile.IsVisible = true;
            }
            profile.Status = normalizedDecision; profile.ReviewNote = cleanedReviewNote; profile.UpdatedAtUtc = DateTime.UtcNow;
        }

        db.AuditLogs.Add(new AuditLogEntity
        {
            ActorUserId = adminUserId,
            EntityName = $"{type}Profile",
            EntityId = profileId.ToString(),
            ActionName = normalizedDecision,
            PayloadJson = JsonSerializer.Serialize(new { PreviousStatus = currentStatus, ReviewNote = cleanedReviewNote, ApprovedWithMissingFields = approvedWithMissingFields }),
            CreatedAtUtc = DateTime.UtcNow
        });
        await db.SaveChangesAsync(cancellationToken);
        if (normalizedDecision == ProfileStatuses.Approved && approvedWithMissingFields.Count > 0)
            return new(true, $"Đã duyệt hồ sơ KOC theo quyết định của Admin. Hồ sơ vẫn còn thiếu: {string.Join("; ", MissingLabels(approvedWithMissingFields))}. Danh sách này đã được lưu vào nhật ký kiểm toán.");
        return new(true, normalizedDecision == ProfileStatuses.Approved ? "Đã duyệt hồ sơ." : "Đã cập nhật kết quả duyệt hồ sơ.");
    }

    private static string? NormalizeType(string? value) => value?.Trim().ToUpperInvariant() switch { "BRAND" => "BRAND", "KOC" => "KOC", _ => null };
    private static string? NormalizeDecision(string? value) => value?.Trim().ToUpperInvariant() switch { "APPROVED" => ProfileStatuses.Approved, "NEEDUPDATE" => ProfileStatuses.NeedUpdate, "REJECTED" => ProfileStatuses.Rejected, _ => null };
    private static ProfileReviewResult InvalidState(string status) => new(false, $"Không thể điều chỉnh hồ sơ ở trạng thái hiện tại: {status}.");
    private static string[] MissingLabels(IReadOnlyList<string> fields)
    {
        static string Label(string field) => field switch
        {
            "BodyProfile" => "thông tin cá nhân (chiều cao, cân nặng và tình trạng hôn nhân)",
            "SocialAccount" => "ít nhất một kênh mạng xã hội",
            "Categories" => "ít nhất một nhóm nội dung phù hợp",
            "Email" => "email",
            "Phone" => "số điện thoại",
            "Gender" => "giới tính",
            "DisplayName" => "tên hiển thị",
            "StreetAddress" => "địa chỉ liên hệ",
            "CompanyName" => "tên doanh nghiệp",
            "ContactName" => "người liên hệ",
            "ContactPhone" => "số điện thoại liên hệ",
            "TaxCode" => "mã số thuế",
            "TaxCompanyName" => "tên đơn vị xuất hóa đơn",
            "TaxInvoiceAddress" => "địa chỉ xuất hóa đơn",
            "BusinessLicenseFileUrl" => "giấy phép kinh doanh",
            _ => "thông tin bắt buộc khác"
        };
        return fields.Select(Label).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
    }
    private static ProfileReviewResult Incomplete(IReadOnlyList<string> fields)
    {
        var missing = MissingLabels(fields);
        return new(false, $"Chưa thể duyệt hồ sơ vì còn thiếu {missing.Length} mục bắt buộc: {string.Join("; ", missing)}. Hãy chọn “Cần bổ sung” và ghi rõ yêu cầu để người dùng cập nhật.");
    }
}

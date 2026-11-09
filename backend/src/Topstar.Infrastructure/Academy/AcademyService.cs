using System.Text.Json;
using System.Data;
using Microsoft.EntityFrameworkCore;
using Topstar.Application.Academy;
using Topstar.Infrastructure.Entities;
using Topstar.Infrastructure.Persistence;

namespace Topstar.Infrastructure.Academy;

public sealed class AcademyService(TopstarDbContext db) : IAcademyService
{
    public Task<IReadOnlyList<AcademyCourseItem>> GetAdminAsync(CancellationToken ct = default) => GetAsync(null, true, ct);
    public Task<IReadOnlyList<AcademyCourseItem>> GetPublishedAsync(CancellationToken ct = default) => GetAsync("Published", false, ct);

    private async Task<IReadOnlyList<AcademyCourseItem>> GetAsync(string? status, bool history, CancellationToken ct)
    {
        var items = await db.AcademyCourses.AsNoTracking()
            .Where(x => status == null || x.Status == status)
            .OrderBy(x => x.OrderNo).ThenBy(x => x.Title)
            .Select(x => new AcademyCourseItem(x.Id, x.Title, x.Level, x.DurationWeeks, x.Summary, x.Syllabus,
                x.Status, x.OrderNo, x.CreatedAtUtc, x.UpdatedAtUtc))
            .ToListAsync(ct);
        if (items.Count == 0) return items;
        var ids = items.Select(x => x.Id).ToArray();
        var lessons = await db.AcademyLessons.AsNoTracking().Where(x => ids.Contains(x.CourseId))
            .OrderBy(x => x.OrderNo).ThenBy(x => x.Title)
            .Select(x => new { x.CourseId, Item = new AcademyLessonItem(x.Id, x.Title, x.ContentUrl, x.OrderNo, x.IsActive, false) })
            .ToListAsync(ct);
        items = items.Select(x => x with
            { Lessons = lessons.Where(l => l.CourseId == x.Id).Select(l => l.Item).ToList() }).ToList();
        if (!history) return items;
        var events = await db.AcademyCourseHistories.AsNoTracking().Where(x => ids.Contains(x.CourseId))
            .OrderBy(x => x.CreatedAtUtc).ToListAsync(ct);
        return items.Select(x => x with
        {
            History = events.Where(e => e.CourseId == x.Id)
                .Select(e => new AcademyCourseHistoryItem(e.FromStatus, e.ToStatus, e.ActionName, e.Note, e.CreatedAtUtc)).ToList()
        }).ToList();
    }

    public async Task<AcademyResult> CreateAsync(Guid actor, string title, string level, int weeks,
        string summary, string? syllabus, int orderNo, CancellationToken ct = default)
    {
        Normalize(ref title, ref level, ref summary, ref syllabus);
        if (!AcademyCourseRules.IsValid(title, level, weeks, summary) || orderNo is < 0 or > 10000 || syllabus?.Length > 4000)
            return Fail("Kiểm tra tiêu đề, cấp độ, thời lượng, thứ tự và mô tả khóa học.");
        var now = DateTime.UtcNow;
        var item = new AcademyCourseEntity { Id = Guid.NewGuid(), Title = title, Level = level, DurationWeeks = weeks,
            Summary = summary, Syllabus = syllabus, Status = "Draft", OrderNo = orderNo, CreatedByUserId = actor, CreatedAtUtc = now };
        db.AcademyCourses.Add(item);
        AddHistory(item, null, "Draft", "Create", "Tạo khóa học ở trạng thái nháp.", actor, now);
        Audit(actor, item.Id, "Create", new { title, level, weeks, orderNo }, now);
        await db.SaveChangesAsync(ct);
        return Ok("Đã tạo khóa học nháp.");
    }

    public async Task<AcademyResult> UpdateAsync(Guid actor, Guid id, string title, string level, int weeks,
        string summary, string? syllabus, int orderNo, string reason, CancellationToken ct = default)
    {
        Normalize(ref title, ref level, ref summary, ref syllabus); reason = reason?.Trim() ?? "";
        if (!AcademyCourseRules.IsValid(title, level, weeks, summary) || orderNo is < 0 or > 10000 ||
            syllabus?.Length > 4000 || reason.Length is < 5 or > 1000) return Fail("Dữ liệu chưa hợp lệ; lý do sửa cần 5–1.000 ký tự.");
        var item = await db.AcademyCourses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Fail("Không tìm thấy khóa học.");
        item.Title = title; item.Level = level; item.DurationWeeks = weeks; item.Summary = summary;
        item.Syllabus = syllabus; item.OrderNo = orderNo; item.UpdatedByUserId = actor; item.UpdatedAtUtc = DateTime.UtcNow;
        AddHistory(item, item.Status, item.Status, "Update", reason, actor, item.UpdatedAtUtc.Value);
        Audit(actor, id, "Update", new { reason }, item.UpdatedAtUtc.Value);
        await db.SaveChangesAsync(ct);
        return Ok("Đã cập nhật khóa học và lưu lịch sử.");
    }

    public async Task<AcademyResult> ChangeStatusAsync(Guid actor, Guid id, string status, string reason, CancellationToken ct = default)
    {
        status = status?.Trim() ?? ""; reason = reason?.Trim() ?? "";
        if (!AcademyCourseRules.IsValidStatus(status) || reason.Length is < 5 or > 1000) return Fail("Trạng thái hoặc lý do thay đổi chưa hợp lệ.");
        var item = await db.AcademyCourses.FirstOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Fail("Không tìm thấy khóa học.");
        if (item.Status == status) return Fail("Khóa học đã ở trạng thái này.");
        if (item.Status == "Archived" && status == "Published") return Fail("Khóa đã lưu trữ phải chuyển về Draft trước khi xuất bản lại.");
        var from = item.Status; var now = DateTime.UtcNow;
        item.Status = status; item.UpdatedByUserId = actor; item.UpdatedAtUtc = now;
        AddHistory(item, from, status, "ChangeStatus", reason, actor, now);
        Audit(actor, id, "ChangeStatus", new { from, status, reason }, now);
        await db.SaveChangesAsync(ct);
        return Ok("Đã đổi trạng thái khóa học.");
    }

    public async Task<AcademyResult> SaveLessonAsync(Guid actor, Guid courseId, Guid? lessonId, string title,
        string? contentUrl, int orderNo, bool isActive, string reason, CancellationToken ct = default)
    {
        title = title?.Trim() ?? "";
        contentUrl = string.IsNullOrWhiteSpace(contentUrl) ? null : contentUrl.Trim();
        reason = reason?.Trim() ?? "";
        if (!AcademyCourseRules.IsValidLesson(title, contentUrl, orderNo) || reason.Length is < 5 or > 1000)
            return Fail("Bài học chưa hợp lệ; URL chỉ nhận HTTP/HTTPS và lý do cần 5–1.000 ký tự.");
        if (!await db.AcademyCourses.AnyAsync(x => x.Id == courseId, ct))
            return Fail("Không tìm thấy khóa học.");
        var now = DateTime.UtcNow;
        AcademyLessonEntity item;
        if (lessonId.HasValue)
        {
            item = await db.AcademyLessons.FirstOrDefaultAsync(x => x.Id == lessonId && x.CourseId == courseId, ct)
                ?? null!;
            if (item is null) return Fail("Không tìm thấy bài học trong khóa này.");
            item.Title = title; item.ContentUrl = contentUrl; item.OrderNo = orderNo; item.IsActive = isActive;
            item.UpdatedByUserId = actor; item.UpdatedAtUtc = now;
        }
        else
        {
            item = new AcademyLessonEntity { Id = Guid.NewGuid(), CourseId = courseId, Title = title,
                ContentUrl = contentUrl, OrderNo = orderNo, IsActive = isActive, CreatedByUserId = actor,
                CreatedAtUtc = now };
            db.AcademyLessons.Add(item);
        }
        db.AcademyLessonHistories.Add(new AcademyLessonHistoryEntity { Id = Guid.NewGuid(), LessonId = item.Id,
            TitleSnapshot = title, ContentUrlSnapshot = contentUrl, OrderNoSnapshot = orderNo,
            IsActiveSnapshot = isActive, Reason = reason, ActorUserId = actor, CreatedAtUtc = now });
        Audit(actor, item.Id, lessonId.HasValue ? "UpdateLesson" : "CreateLesson", new { courseId, reason }, now);
        await db.SaveChangesAsync(ct);
        return Ok(lessonId.HasValue ? "Đã cập nhật bài học và lưu phiên bản." : "Đã thêm bài học.");
    }

    public async Task<IReadOnlyList<AcademyCourseItem>> GetForKocAsync(Guid kocUserId, CancellationToken ct = default)
    {
        var enrollments = await db.AcademyEnrollments.AsNoTracking()
            .Where(x => x.KocUserId == kocUserId).ToListAsync(ct);
        var enrolledCourseIds = enrollments.Select(x => x.CourseId).ToHashSet();
        var items = (await GetAsync(null, false, ct))
            .Where(x => x.Status == "Published" || enrolledCourseIds.Contains(x.Id))
            .Select(x => x with { Lessons = x.Lessons.Where(l => l.IsActive).ToList() }).ToList();
        if (items.Count == 0) return items;
        var enrollmentIds = enrollments.Select(x => x.Id).ToArray();
        var required = await db.AcademyEnrollmentLessons.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .OrderBy(x => x.OrderNoSnapshot)
            .Select(x => new { x.EnrollmentId, x.LessonId, x.TitleSnapshot, x.OrderNoSnapshot })
            .ToListAsync(ct);
        var completed = await db.AcademyLessonCompletions.AsNoTracking()
            .Where(x => enrollmentIds.Contains(x.EnrollmentId))
            .Select(x => new { x.EnrollmentId, x.LessonId }).ToListAsync(ct);
        return items.Select(course =>
        {
            var enrollment = enrollments.FirstOrDefault(x => x.CourseId == course.Id);
            if (enrollment is null) return course;
            var requiredLessons = required.Where(x => x.EnrollmentId == enrollment.Id).ToList();
            var lessonIds = requiredLessons.Select(x => x.LessonId).ToHashSet();
            var done = completed.Count(x => x.EnrollmentId == enrollment.Id && lessonIds.Contains(x.LessonId));
            var total = lessonIds.Count;
            var doneIds = completed.Where(x => x.EnrollmentId == enrollment.Id).Select(x => x.LessonId).ToHashSet();
            var currentById = course.Lessons.ToDictionary(x => x.Id);
            return course with {
                Lessons = requiredLessons.Select(x => new AcademyLessonItem(x.LessonId, x.TitleSnapshot,
                    currentById.GetValueOrDefault(x.LessonId)?.ContentUrl, x.OrderNoSnapshot, true,
                    doneIds.Contains(x.LessonId))).ToList(),
                Enrollment = new AcademyEnrollmentItem(enrollment.Id, enrollment.EnrolledAtUtc,
                    done, total, total == 0 ? 0 : Math.Round(done * 100m / total, 2),
                    enrollment.CompletedAtUtc, enrollment.CertificateCode)
            };
        }).ToList();
    }

    public async Task<AcademyResult> EnrollAsync(Guid kocUserId, Guid courseId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var course = await db.AcademyCourses.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == courseId && x.Status == "Published", ct);
        if (course is null) return Fail("Khóa học không còn mở đăng ký.");
        var lessons = await db.AcademyLessons.AsNoTracking()
            .Where(x => x.CourseId == courseId && x.IsActive)
            .OrderBy(x => x.OrderNo).ThenBy(x => x.Id).ToListAsync(ct);
        if (lessons.Count == 0)
            return Fail("Khóa học chưa có bài học được xuất bản.");
        if (await db.AcademyEnrollments.AnyAsync(x => x.CourseId == courseId && x.KocUserId == kocUserId, ct))
            return Fail("Bạn đã đăng ký khóa học này.");
        var kocName = await db.KocProfiles.AsNoTracking().Where(x => x.UserId == kocUserId)
            .Select(x => x.DisplayName).FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(kocName)) return Fail("Không tìm thấy hồ sơ KOC hợp lệ.");
        var now = DateTime.UtcNow;
        var enrollmentId = Guid.NewGuid();
        db.AcademyEnrollments.Add(new AcademyEnrollmentEntity { Id = enrollmentId, CourseId = courseId,
            KocUserId = kocUserId, CourseTitleSnapshot = course.Title,
            KocDisplayNameSnapshot = kocName, EnrolledAtUtc = now });
        db.AcademyEnrollmentLessons.AddRange(lessons.Select(x => new AcademyEnrollmentLessonEntity
            { EnrollmentId = enrollmentId, LessonId = x.Id, TitleSnapshot = x.Title, OrderNoSnapshot = x.OrderNo }));
        Audit(kocUserId, courseId, "Enroll", new { courseId }, now);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok("Đã đăng ký khóa học. Bạn có thể bắt đầu từng bài.");
    }

    public async Task<AcademyResult> CompleteLessonAsync(Guid kocUserId, Guid lessonId, CancellationToken ct = default)
    {
        await using var tx = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var lesson = await db.AcademyLessons.AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == lessonId, ct);
        if (lesson is null) return Fail("Bài học không còn khả dụng.");
        var enrollment = await db.AcademyEnrollments.FirstOrDefaultAsync(
            x => x.CourseId == lesson.CourseId && x.KocUserId == kocUserId, ct);
        if (enrollment is null) return Fail("Bạn cần đăng ký khóa học trước.");
        if (!await db.AcademyEnrollmentLessons.AnyAsync(
                x => x.EnrollmentId == enrollment.Id && x.LessonId == lessonId, ct))
            return Fail("Bài học không thuộc lộ trình đã chụp khi bạn đăng ký.");
        if (await db.AcademyLessonCompletions.AnyAsync(
                x => x.EnrollmentId == enrollment.Id && x.LessonId == lessonId, ct))
            return Fail("Bài học này đã được hoàn thành.");
        var now = DateTime.UtcNow;
        db.AcademyLessonCompletions.Add(new AcademyLessonCompletionEntity
            { Id = Guid.NewGuid(), EnrollmentId = enrollment.Id, LessonId = lessonId, CompletedAtUtc = now });
        var total = await db.AcademyEnrollmentLessons.CountAsync(x => x.EnrollmentId == enrollment.Id, ct);
        var completedBefore = await db.AcademyLessonCompletions
            .Join(db.AcademyEnrollmentLessons.Where(x => x.EnrollmentId == enrollment.Id),
                completion => new { completion.EnrollmentId, completion.LessonId },
                requiredLesson => new { requiredLesson.EnrollmentId, requiredLesson.LessonId },
                (completion, _) => completion)
            .CountAsync(x => x.EnrollmentId == enrollment.Id, ct);
        if (total > 0 && completedBefore + 1 >= total && enrollment.CompletedAtUtc is null)
        {
            enrollment.CompletedAtUtc = now;
            enrollment.CertificateCode = $"TS-{now:yyyyMMdd}-{enrollment.Id.ToString("N")[..20].ToUpperInvariant()}";
        }
        Audit(kocUserId, lessonId, "CompleteLesson", new { enrollment.Id, lessonId }, now);
        await db.SaveChangesAsync(ct);
        await tx.CommitAsync(ct);
        return Ok(enrollment.CompletedAtUtc.HasValue
            ? $"Chúc mừng! Bạn đã hoàn tất khóa học. Mã chứng nhận: {enrollment.CertificateCode}."
            : "Đã ghi nhận hoàn thành bài học.");
    }

    public async Task<AcademyCertificateItem?> VerifyCertificateAsync(string certificateCode,
        CancellationToken ct = default)
    {
        certificateCode = certificateCode?.Trim().ToUpperInvariant() ?? "";
        if (certificateCode.Length is < 10 or > 40) return null;
        return await db.AcademyEnrollments.AsNoTracking()
            .Where(x => x.CertificateCode == certificateCode && x.CompletedAtUtc != null)
            .Select(x => new AcademyCertificateItem(x.CertificateCode!, x.KocDisplayNameSnapshot,
                x.CourseTitleSnapshot, x.EnrolledAtUtc, x.CompletedAtUtc!.Value,
                db.AcademyEnrollmentLessons.Count(l => l.EnrollmentId == x.Id)))
            .FirstOrDefaultAsync(ct);
    }

    public async Task<IReadOnlyList<AcademyAdminEnrollmentItem>> GetAdminEnrollmentsAsync(
        CancellationToken ct = default)
    {
        return await db.AcademyEnrollments.AsNoTracking()
            .OrderByDescending(x => x.EnrolledAtUtc)
            .Take(100)
            .Join(db.Users.AsNoTracking(), x => x.KocUserId, x => x.Id, (enrollment, user) => new { enrollment, user })
            .Select(x => new AcademyAdminEnrollmentItem(
                x.enrollment.Id,
                x.enrollment.KocDisplayNameSnapshot,
                x.user.Email,
                x.enrollment.CourseTitleSnapshot,
                x.enrollment.EnrolledAtUtc,
                db.AcademyLessonCompletions.Count(c => c.EnrollmentId == x.enrollment.Id),
                db.AcademyEnrollmentLessons.Count(l => l.EnrollmentId == x.enrollment.Id),
                db.AcademyEnrollmentLessons.Count(l => l.EnrollmentId == x.enrollment.Id) == 0 ? 0 :
                    Math.Round(
                        db.AcademyLessonCompletions.Count(c => c.EnrollmentId == x.enrollment.Id) * 100m /
                        db.AcademyEnrollmentLessons.Count(l => l.EnrollmentId == x.enrollment.Id), 2),
                x.enrollment.CompletedAtUtc,
                x.enrollment.CertificateCode))
            .ToListAsync(ct);
    }

    private void AddHistory(AcademyCourseEntity item, string? from, string to, string action, string note, Guid actor, DateTime now)
        => db.AcademyCourseHistories.Add(new() { Id = Guid.NewGuid(), CourseId = item.Id, FromStatus = from, ToStatus = to,
            ActionName = action, Note = note, ActorUserId = actor, CreatedAtUtc = now });
    private void Audit(Guid actor, Guid id, string action, object payload, DateTime now)
        => db.AuditLogs.Add(new() { ActorUserId = actor, EntityName = "AcademyCourse", EntityId = id.ToString(),
            ActionName = action, PayloadJson = JsonSerializer.Serialize(payload), CreatedAtUtc = now });
    private static void Normalize(ref string title, ref string level, ref string summary, ref string? syllabus)
    { title = title?.Trim() ?? ""; level = level?.Trim() ?? ""; summary = summary?.Trim() ?? ""; syllabus = string.IsNullOrWhiteSpace(syllabus) ? null : syllabus.Trim(); }
    private static AcademyResult Ok(string message) => new(true, message);
    private static AcademyResult Fail(string message) => new(false, message);
}

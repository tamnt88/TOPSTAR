namespace Topstar.Application.Academy;

public static class AcademyCourseRules
{
    public static readonly string[] Statuses = ["Draft", "Published", "Archived"];
    public static bool IsValidStatus(string? value) => value is not null && Statuses.Contains(value, StringComparer.Ordinal);
    public static bool IsValid(string? title, string? level, int durationWeeks, string? summary)
        => title?.Trim().Length is >= 3 and <= 160
           && level?.Trim().Length is >= 2 and <= 50
           && durationWeeks is >= 1 and <= 104
           && summary?.Trim().Length is >= 10 and <= 1000;
    public static bool IsValidLesson(string? title, string? contentUrl, int orderNo)
    {
        if (title?.Trim().Length is not (>= 3 and <= 200) || orderNo is < 0 or > 10000 ||
            contentUrl?.Trim().Length > 1000) return false;
        if (string.IsNullOrWhiteSpace(contentUrl)) return true;
        return Uri.TryCreate(contentUrl.Trim(), UriKind.Absolute, out var uri) && uri.Scheme is "http" or "https";
    }
}

public sealed record AcademyCourseHistoryItem(
    string? FromStatus, string ToStatus, string ActionName, string Note, DateTime CreatedAtUtc);

public sealed record AcademyCourseItem(
    Guid Id, string Title, string Level, int DurationWeeks, string Summary, string? Syllabus,
    string Status, int OrderNo, DateTime CreatedAtUtc, DateTime? UpdatedAtUtc)
{
    public IReadOnlyList<AcademyCourseHistoryItem> History { get; init; } = [];
    public IReadOnlyList<AcademyLessonItem> Lessons { get; init; } = [];
    public AcademyEnrollmentItem? Enrollment { get; init; }
}

public sealed record AcademyLessonItem(Guid Id, string Title, string? ContentUrl, int OrderNo, bool IsActive, bool IsCompleted = false);
public sealed record AcademyEnrollmentItem(
    Guid Id, DateTime EnrolledAtUtc, int CompletedLessons, int TotalLessons, decimal ProgressPercent,
    DateTime? CompletedAtUtc, string? CertificateCode);
public sealed record AcademyCertificateItem(
    string CertificateCode, string KocDisplayName, string CourseTitle, DateTime EnrolledAtUtc,
    DateTime CompletedAtUtc, int RequiredLessonCount);
public sealed record AcademyAdminEnrollmentItem(
    Guid EnrollmentId, string KocDisplayName, string KocEmail, string CourseTitle,
    DateTime EnrolledAtUtc, int CompletedLessons, int RequiredLessons, decimal ProgressPercent,
    DateTime? CompletedAtUtc, string? CertificateCode);

public sealed record AcademyResult(bool Success, string Message);

public interface IAcademyService
{
    Task<IReadOnlyList<AcademyCourseItem>> GetAdminAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademyCourseItem>> GetPublishedAsync(CancellationToken cancellationToken = default);
    Task<AcademyResult> CreateAsync(Guid actorUserId, string title, string level, int durationWeeks,
        string summary, string? syllabus, int orderNo, CancellationToken cancellationToken = default);
    Task<AcademyResult> UpdateAsync(Guid actorUserId, Guid id, string title, string level, int durationWeeks,
        string summary, string? syllabus, int orderNo, string reason, CancellationToken cancellationToken = default);
    Task<AcademyResult> ChangeStatusAsync(Guid actorUserId, Guid id, string status, string reason,
        CancellationToken cancellationToken = default);
    Task<AcademyResult> SaveLessonAsync(Guid actorUserId, Guid courseId, Guid? lessonId, string title,
        string? contentUrl, int orderNo, bool isActive, string reason, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademyCourseItem>> GetForKocAsync(Guid kocUserId, CancellationToken cancellationToken = default);
    Task<AcademyResult> EnrollAsync(Guid kocUserId, Guid courseId, CancellationToken cancellationToken = default);
    Task<AcademyResult> CompleteLessonAsync(Guid kocUserId, Guid lessonId, CancellationToken cancellationToken = default);
    Task<AcademyCertificateItem?> VerifyCertificateAsync(string certificateCode,
        CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AcademyAdminEnrollmentItem>> GetAdminEnrollmentsAsync(
        CancellationToken cancellationToken = default);
}

namespace Topstar.Infrastructure.Entities;

public sealed class AcademyCourseEntity
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Level { get; set; } = string.Empty;
    public int DurationWeeks { get; set; }
    public string Summary { get; set; } = string.Empty;
    public string? Syllabus { get; set; }
    public string Status { get; set; } = string.Empty;
    public int OrderNo { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class AcademyCourseHistoryEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string ActionName { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AcademyLessonEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? ContentUrl { get; set; }
    public int OrderNo { get; set; }
    public bool IsActive { get; set; }
    public Guid CreatedByUserId { get; set; }
    public Guid? UpdatedByUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? UpdatedAtUtc { get; set; }
}

public sealed class AcademyLessonHistoryEntity
{
    public Guid Id { get; set; }
    public Guid LessonId { get; set; }
    public string TitleSnapshot { get; set; } = string.Empty;
    public string? ContentUrlSnapshot { get; set; }
    public int OrderNoSnapshot { get; set; }
    public bool IsActiveSnapshot { get; set; }
    public string Reason { get; set; } = string.Empty;
    public Guid ActorUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}

public sealed class AcademyEnrollmentEntity
{
    public Guid Id { get; set; }
    public Guid CourseId { get; set; }
    public Guid KocUserId { get; set; }
    public string CourseTitleSnapshot { get; set; } = string.Empty;
    public string KocDisplayNameSnapshot { get; set; } = string.Empty;
    public DateTime EnrolledAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? CertificateCode { get; set; }
}

public sealed class AcademyLessonCompletionEntity
{
    public Guid Id { get; set; }
    public Guid EnrollmentId { get; set; }
    public Guid LessonId { get; set; }
    public DateTime CompletedAtUtc { get; set; }
}

public sealed class AcademyEnrollmentLessonEntity
{
    public Guid EnrollmentId { get; set; }
    public Guid LessonId { get; set; }
    public string TitleSnapshot { get; set; } = string.Empty;
    public int OrderNoSnapshot { get; set; }
}

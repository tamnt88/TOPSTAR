SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() NOT IN (N'TopstarDb', N'TopstarDb_Dev', N'TopstarDb_Test')
    THROW 51141, N'File seed Academy chỉ được phép chạy trên cơ sở dữ liệu DEV/QA.', 1;

IF OBJECT_ID(N'dbo.AcademyCourses', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AcademyCourseHistories', N'U') IS NULL
    THROW 51142, N'Chưa có schema Academy.', 1;

DECLARE @AdminUserId UNIQUEIDENTIFIER =
(
    SELECT TOP (1) u.Id
    FROM dbo.Users u
    INNER JOIN dbo.UserRoles ur ON ur.UserId = u.Id
    INNER JOIN dbo.Roles r ON r.Id = ur.RoleId
    WHERE UPPER(r.Code) = 'ADMIN'
    ORDER BY u.CreatedAtUtc, u.Id
);

IF @AdminUserId IS NULL
    THROW 51143, N'Không tìm thấy tài khoản Admin để ghi lịch sử.', 1;

IF NOT EXISTS (SELECT 1 FROM dbo.AcademyCourses WHERE Title = N'Đo lường hiệu quả và phát triển kênh')
BEGIN
    DECLARE @CourseId UNIQUEIDENTIFIER = 'A1410004-0000-4000-8000-000000000004';
    DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();

    BEGIN TRANSACTION;

    INSERT dbo.AcademyCourses
        (Id, Title, Level, DurationWeeks, Summary, Syllabus, Status, OrderNo, CreatedByUserId, UpdatedByUserId, CreatedAtUtc, UpdatedAtUtc)
    VALUES
        (@CourseId, N'Đo lường hiệu quả và phát triển kênh', N'Chuyên sâu', 4,
         N'Đọc dữ liệu nội dung, đánh giá hiệu suất và xây dựng kế hoạch phát triển kênh KOC bền vững.',
         N'Chọn chỉ số phù hợp; đọc báo cáo; phân tích nội dung hiệu quả; tối ưu hồ sơ; lập kế hoạch phát triển kênh.',
         'Published', 40, @AdminUserId, @AdminUserId, @Now, @Now);

    INSERT dbo.AcademyCourseHistories
        (Id, CourseId, FromStatus, ToStatus, ActionName, Note, ActorUserId, CreatedAtUtc)
    VALUES
        ('A1411004-0000-4000-8000-000000000004', @CourseId, NULL, 'Published', 'Create',
         N'Bổ sung khóa học thứ tư để hoàn thiện lộ trình và cân đối danh mục công khai.', @AdminUserId, @Now);

    COMMIT TRANSACTION;
END;

SELECT Title, Level, DurationWeeks, Status, OrderNo
FROM dbo.AcademyCourses
WHERE Status = 'Published'
ORDER BY OrderNo, Title;

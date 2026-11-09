SET NOCOUNT ON;
SET XACT_ABORT ON;

/*
  Dữ liệu mẫu DEV/QA cho mục "Các khóa học đang mở" tại /hoc-vien-koc.
  - Chạy lặp lại an toàn: không cập nhật/ghi đè nội dung Admin đã chỉnh sửa.
  - Khóa học được xuất bản để hiển thị công khai.
  - Mỗi bản ghi đều có lịch sử audit tương ứng.
*/

IF DB_NAME() NOT IN (N'TopstarDb', N'TopstarDb_Dev', N'TopstarDb_Test')
    THROW 51131, N'File seed Academy chỉ được phép chạy trên cơ sở dữ liệu DEV/QA.', 1;

IF OBJECT_ID(N'dbo.AcademyCourses', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AcademyCourseHistories', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AcademyLessons', N'U') IS NULL
   OR OBJECT_ID(N'dbo.AcademyLessonHistories', N'U') IS NULL
    THROW 51132, N'Chưa có schema Academy. Hãy chạy các file SQL 66 và 71 trước.', 1;

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
    THROW 51133, N'Không tìm thấy tài khoản Admin để ghi nhận người tạo và lịch sử audit.', 1;

DECLARE @Now DATETIME2(7) = SYSUTCDATETIME();
DECLARE @Course1 UNIQUEIDENTIFIER;
DECLARE @Course2 UNIQUEIDENTIFIER;
DECLARE @Course3 UNIQUEIDENTIFIER;

BEGIN TRY
    BEGIN TRANSACTION;

    SELECT TOP (1) @Course1 = Id FROM dbo.AcademyCourses WHERE Title = N'Nền tảng KOC chuyên nghiệp' ORDER BY CreatedAtUtc;
    IF @Course1 IS NULL
    BEGIN
        SET @Course1 = 'A1310001-0000-4000-8000-000000000001';
        INSERT dbo.AcademyCourses
            (Id, Title, Level, DurationWeeks, Summary, Syllabus, Status, OrderNo, CreatedByUserId, UpdatedByUserId, CreatedAtUtc, UpdatedAtUtc)
        VALUES
            (@Course1, N'Nền tảng KOC chuyên nghiệp', N'Cơ bản', 4,
             N'Xây dựng tư duy nghề nghiệp, hồ sơ đáng tin cậy và quy trình làm việc chuẩn khi bắt đầu với vai trò KOC.',
             N'Định vị cá nhân; hoàn thiện hồ sơ; đọc brief; giao tiếp với Brand; nguyên tắc minh bạch và lưu vết công việc.',
             'Published', 10, @AdminUserId, @AdminUserId, DATEADD(MINUTE, -30, @Now), @Now);

        INSERT dbo.AcademyCourseHistories
            (Id, CourseId, FromStatus, ToStatus, ActionName, Note, ActorUserId, CreatedAtUtc)
        VALUES
            ('A1311001-0000-4000-8000-000000000001', @Course1, NULL, 'Published', 'Create',
             N'Tạo và xuất bản khóa học mẫu phục vụ kiểm thử trang Học viện KOC.', @AdminUserId, @Now);
    END;

    SELECT TOP (1) @Course2 = Id FROM dbo.AcademyCourses WHERE Title = N'Sản xuất nội dung ngắn tạo chuyển đổi' ORDER BY CreatedAtUtc;
    IF @Course2 IS NULL
    BEGIN
        SET @Course2 = 'A1310002-0000-4000-8000-000000000002';
        INSERT dbo.AcademyCourses
            (Id, Title, Level, DurationWeeks, Summary, Syllabus, Status, OrderNo, CreatedByUserId, UpdatedByUserId, CreatedAtUtc, UpdatedAtUtc)
        VALUES
            (@Course2, N'Sản xuất nội dung ngắn tạo chuyển đổi', N'Trung cấp', 6,
             N'Biến brief thành kịch bản, hình ảnh và video ngắn đúng thông điệp, tự nhiên với người xem và có thể đo lường.',
             N'Phân tích brief; phát triển ý tưởng; viết hook và kịch bản; quay dựng; tối ưu CTA; đọc chỉ số nội dung.',
             'Published', 20, @AdminUserId, @AdminUserId, DATEADD(MINUTE, -20, @Now), @Now);

        INSERT dbo.AcademyCourseHistories
            (Id, CourseId, FromStatus, ToStatus, ActionName, Note, ActorUserId, CreatedAtUtc)
        VALUES
            ('A1311002-0000-4000-8000-000000000002', @Course2, NULL, 'Published', 'Create',
             N'Tạo và xuất bản khóa học mẫu phục vụ kiểm thử trang Học viện KOC.', @AdminUserId, @Now);
    END;

    SELECT TOP (1) @Course3 = Id FROM dbo.AcademyCourses WHERE Title = N'Làm việc với Brand và vận hành chiến dịch' ORDER BY CreatedAtUtc;
    IF @Course3 IS NULL
    BEGIN
        SET @Course3 = 'A1310003-0000-4000-8000-000000000003';
        INSERT dbo.AcademyCourses
            (Id, Title, Level, DurationWeeks, Summary, Syllabus, Status, OrderNo, CreatedByUserId, UpdatedByUserId, CreatedAtUtc, UpdatedAtUtc)
        VALUES
            (@Course3, N'Làm việc với Brand và vận hành chiến dịch', N'Nâng cao', 5,
             N'Nắm vững booking, giao nhận, duyệt nội dung, nghiệm thu và đối soát để hợp tác với Brand chuyên nghiệp hơn.',
             N'Thỏa thuận phạm vi; booking; nhận và bảo quản sản phẩm; duyệt nội dung; xử lý thay đổi; nghiệm thu và đối soát.',
             'Published', 30, @AdminUserId, @AdminUserId, DATEADD(MINUTE, -10, @Now), @Now);

        INSERT dbo.AcademyCourseHistories
            (Id, CourseId, FromStatus, ToStatus, ActionName, Note, ActorUserId, CreatedAtUtc)
        VALUES
            ('A1311003-0000-4000-8000-000000000003', @Course3, NULL, 'Published', 'Create',
             N'Tạo và xuất bản khóa học mẫu phục vụ kiểm thử trang Học viện KOC.', @AdminUserId, @Now);
    END;

    DECLARE @Lessons TABLE
    (
        Id UNIQUEIDENTIFIER NOT NULL,
        HistoryId UNIQUEIDENTIFIER NOT NULL,
        CourseId UNIQUEIDENTIFIER NOT NULL,
        Title NVARCHAR(200) NOT NULL,
        ContentUrl NVARCHAR(1000) NULL,
        OrderNo INT NOT NULL
    );

    INSERT @Lessons (Id, HistoryId, CourseId, Title, ContentUrl, OrderNo)
    VALUES
      ('A1312001-0000-4000-8000-000000000001','A1313001-0000-4000-8000-000000000001',@Course1,N'Định vị vai trò và chuẩn nghề nghiệp KOC',NULL,10),
      ('A1312002-0000-4000-8000-000000000002','A1313002-0000-4000-8000-000000000002',@Course1,N'Hoàn thiện hồ sơ năng lực đáng tin cậy',NULL,20),
      ('A1312003-0000-4000-8000-000000000003','A1313003-0000-4000-8000-000000000003',@Course1,N'Đọc brief và trao đổi chuyên nghiệp với Brand',NULL,30),
      ('A1312101-0000-4000-8000-000000000101','A1313101-0000-4000-8000-000000000101',@Course2,N'Từ mục tiêu chiến dịch đến insight nội dung',NULL,10),
      ('A1312102-0000-4000-8000-000000000102','A1313102-0000-4000-8000-000000000102',@Course2,N'Viết hook và kịch bản video ngắn',NULL,20),
      ('A1312103-0000-4000-8000-000000000103','A1313103-0000-4000-8000-000000000103',@Course2,N'Quay dựng gọn nhẹ nhưng đúng tiêu chuẩn',NULL,30),
      ('A1312104-0000-4000-8000-000000000104','A1313104-0000-4000-8000-000000000104',@Course2,N'Đọc chỉ số và tối ưu nội dung sau đăng',NULL,40),
      ('A1312201-0000-4000-8000-000000000201','A1313201-0000-4000-8000-000000000201',@Course3,N'Chốt phạm vi, lịch làm việc và đầu việc',NULL,10),
      ('A1312202-0000-4000-8000-000000000202','A1313202-0000-4000-8000-000000000202',@Course3,N'Theo dõi giao nhận và bằng chứng thực hiện',NULL,20),
      ('A1312203-0000-4000-8000-000000000203','A1313203-0000-4000-8000-000000000203',@Course3,N'Duyệt nội dung và xử lý yêu cầu chỉnh sửa',NULL,30),
      ('A1312204-0000-4000-8000-000000000204','A1313204-0000-4000-8000-000000000204',@Course3,N'Nghiệm thu, đánh giá và đối soát chiến dịch',NULL,40);

    INSERT dbo.AcademyLessons
        (Id, CourseId, Title, ContentUrl, OrderNo, IsActive, CreatedByUserId, UpdatedByUserId, CreatedAtUtc, UpdatedAtUtc)
    SELECT l.Id, l.CourseId, l.Title, l.ContentUrl, l.OrderNo, 1, @AdminUserId, NULL, @Now, NULL
    FROM @Lessons l
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.AcademyLessons existing
        WHERE existing.CourseId = l.CourseId AND existing.Title = l.Title
    );

    INSERT dbo.AcademyLessonHistories
        (Id, LessonId, TitleSnapshot, ContentUrlSnapshot, OrderNoSnapshot, IsActiveSnapshot, Reason, ActorUserId, CreatedAtUtc)
    SELECT l.HistoryId, l.Id, l.Title, l.ContentUrl, l.OrderNo, 1,
           N'Tạo bài học mẫu phục vụ kiểm thử chương trình đào tạo.', @AdminUserId, @Now
    FROM @Lessons l
    WHERE EXISTS (SELECT 1 FROM dbo.AcademyLessons saved WHERE saved.Id = l.Id)
      AND NOT EXISTS (SELECT 1 FROM dbo.AcademyLessonHistories h WHERE h.Id = l.HistoryId);

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT N'OK' AS Result,
       (SELECT COUNT(*) FROM dbo.AcademyCourses WHERE Status = 'Published') AS PublishedCourses,
       (SELECT COUNT(*) FROM dbo.AcademyLessons l INNER JOIN dbo.AcademyCourses c ON c.Id = l.CourseId WHERE c.Status = 'Published' AND l.IsActive = 1) AS PublishedLessons;

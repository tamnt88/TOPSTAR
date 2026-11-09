SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;

 IF OBJECT_ID('dbo.AcademyLessons','U') IS NULL
 BEGIN
  CREATE TABLE dbo.AcademyLessons(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AcademyLessons PRIMARY KEY,
   CourseId UNIQUEIDENTIFIER NOT NULL,
   Title NVARCHAR(200) NOT NULL,
   ContentUrl NVARCHAR(1000) NULL,
   OrderNo INT NOT NULL,
   IsActive BIT NOT NULL,
   CreatedByUserId UNIQUEIDENTIFIER NOT NULL,
   UpdatedByUserId UNIQUEIDENTIFIER NULL,
   CreatedAtUtc DATETIME2(7) NOT NULL,
   UpdatedAtUtc DATETIME2(7) NULL,
   CONSTRAINT FK_AcademyLessons_Course FOREIGN KEY(CourseId) REFERENCES dbo.AcademyCourses(Id),
   CONSTRAINT FK_AcademyLessons_CreatedBy FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT FK_AcademyLessons_UpdatedBy FOREIGN KEY(UpdatedByUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_AcademyLessons_Title CHECK(LEN(LTRIM(RTRIM(Title))) BETWEEN 3 AND 200),
   CONSTRAINT CK_AcademyLessons_Order CHECK(OrderNo BETWEEN 0 AND 10000),
   CONSTRAINT CK_AcademyLessons_Url CHECK(ContentUrl IS NULL OR ContentUrl LIKE 'http://%' OR ContentUrl LIKE 'https://%')
  );
  CREATE INDEX IX_AcademyLessons_Course_Order ON dbo.AcademyLessons(CourseId,OrderNo,Title);
 END;

 IF OBJECT_ID('dbo.AcademyLessonHistories','U') IS NULL
 BEGIN
  CREATE TABLE dbo.AcademyLessonHistories(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AcademyLessonHistories PRIMARY KEY,
   LessonId UNIQUEIDENTIFIER NOT NULL,
   TitleSnapshot NVARCHAR(200) NOT NULL,
   ContentUrlSnapshot NVARCHAR(1000) NULL,
   OrderNoSnapshot INT NOT NULL,
   IsActiveSnapshot BIT NOT NULL,
   Reason NVARCHAR(1000) NOT NULL,
   ActorUserId UNIQUEIDENTIFIER NOT NULL,
   CreatedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_AcademyLessonHistories_Lesson FOREIGN KEY(LessonId) REFERENCES dbo.AcademyLessons(Id),
   CONSTRAINT FK_AcademyLessonHistories_Actor FOREIGN KEY(ActorUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT CK_AcademyLessonHistories_Reason CHECK(LEN(LTRIM(RTRIM(Reason))) BETWEEN 5 AND 1000)
  );
  CREATE INDEX IX_AcademyLessonHistories_Timeline ON dbo.AcademyLessonHistories(LessonId,CreatedAtUtc DESC);
 END;

 IF OBJECT_ID('dbo.AcademyEnrollments','U') IS NULL
 BEGIN
  CREATE TABLE dbo.AcademyEnrollments(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AcademyEnrollments PRIMARY KEY,
   CourseId UNIQUEIDENTIFIER NOT NULL,
   KocUserId UNIQUEIDENTIFIER NOT NULL,
   CourseTitleSnapshot NVARCHAR(160) NOT NULL,
   KocDisplayNameSnapshot NVARCHAR(200) NOT NULL,
   EnrolledAtUtc DATETIME2(7) NOT NULL,
   CompletedAtUtc DATETIME2(7) NULL,
   CertificateCode VARCHAR(40) NULL,
   CONSTRAINT FK_AcademyEnrollments_Course FOREIGN KEY(CourseId) REFERENCES dbo.AcademyCourses(Id),
   CONSTRAINT FK_AcademyEnrollments_Koc FOREIGN KEY(KocUserId) REFERENCES dbo.Users(Id),
   CONSTRAINT UQ_AcademyEnrollments_Course_Koc UNIQUE(CourseId,KocUserId),
   CONSTRAINT CK_AcademyEnrollments_Snapshots CHECK(
    LEN(LTRIM(RTRIM(CourseTitleSnapshot))) BETWEEN 3 AND 160 AND
    LEN(LTRIM(RTRIM(KocDisplayNameSnapshot))) BETWEEN 1 AND 200),
   CONSTRAINT CK_AcademyEnrollments_Completion CHECK(
    (CompletedAtUtc IS NULL AND CertificateCode IS NULL) OR
    (CompletedAtUtc IS NOT NULL AND CompletedAtUtc >= EnrolledAtUtc AND CertificateCode IS NOT NULL))
  );
  CREATE UNIQUE INDEX UX_AcademyEnrollments_Certificate ON dbo.AcademyEnrollments(CertificateCode) WHERE CertificateCode IS NOT NULL;
  CREATE INDEX IX_AcademyEnrollments_Koc ON dbo.AcademyEnrollments(KocUserId,EnrolledAtUtc DESC);
 END;

 IF OBJECT_ID('dbo.AcademyEnrollmentLessons','U') IS NULL
 BEGIN
  CREATE TABLE dbo.AcademyEnrollmentLessons(
   EnrollmentId UNIQUEIDENTIFIER NOT NULL,
   LessonId UNIQUEIDENTIFIER NOT NULL,
   TitleSnapshot NVARCHAR(200) NOT NULL,
   OrderNoSnapshot INT NOT NULL,
   CONSTRAINT PK_AcademyEnrollmentLessons PRIMARY KEY(EnrollmentId,LessonId),
   CONSTRAINT FK_AcademyEnrollmentLessons_Enrollment FOREIGN KEY(EnrollmentId) REFERENCES dbo.AcademyEnrollments(Id),
   CONSTRAINT FK_AcademyEnrollmentLessons_Lesson FOREIGN KEY(LessonId) REFERENCES dbo.AcademyLessons(Id),
   CONSTRAINT CK_AcademyEnrollmentLessons_Title CHECK(LEN(LTRIM(RTRIM(TitleSnapshot))) BETWEEN 3 AND 200),
   CONSTRAINT CK_AcademyEnrollmentLessons_Order CHECK(OrderNoSnapshot BETWEEN 0 AND 10000)
  );
  CREATE INDEX IX_AcademyEnrollmentLessons_Order ON dbo.AcademyEnrollmentLessons(EnrollmentId,OrderNoSnapshot);
 END;

 IF OBJECT_ID('dbo.AcademyLessonCompletions','U') IS NULL
 BEGIN
  CREATE TABLE dbo.AcademyLessonCompletions(
   Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_AcademyLessonCompletions PRIMARY KEY,
   EnrollmentId UNIQUEIDENTIFIER NOT NULL,
   LessonId UNIQUEIDENTIFIER NOT NULL,
   CompletedAtUtc DATETIME2(7) NOT NULL,
   CONSTRAINT FK_AcademyLessonCompletions_Enrollment FOREIGN KEY(EnrollmentId) REFERENCES dbo.AcademyEnrollments(Id),
   CONSTRAINT FK_AcademyLessonCompletions_Lesson FOREIGN KEY(LessonId) REFERENCES dbo.AcademyLessons(Id),
   CONSTRAINT UQ_AcademyLessonCompletions_Enrollment_Lesson UNIQUE(EnrollmentId,LessonId)
  );
  CREATE INDEX IX_AcademyLessonCompletions_Timeline ON dbo.AcademyLessonCompletions(EnrollmentId,CompletedAtUtc);
 END;

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyLessonHistories_AppendOnly ON dbo.AcademyLessonHistories
 AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51071,''Academy lesson history is append-only.'',1; END');

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyLessonCompletions_Protect ON dbo.AcademyLessonCompletions
 AFTER INSERT,UPDATE,DELETE AS
 BEGIN
  SET NOCOUNT ON;
  IF EXISTS(SELECT 1 FROM deleted) THROW 51072,''Academy lesson completion is append-only.'',1;
  IF EXISTS(
   SELECT 1 FROM inserted i
   JOIN dbo.AcademyEnrollments e ON e.Id=i.EnrollmentId
   LEFT JOIN dbo.AcademyEnrollmentLessons required ON required.EnrollmentId=i.EnrollmentId AND required.LessonId=i.LessonId
   WHERE required.LessonId IS NULL OR i.CompletedAtUtc<e.EnrolledAtUtc
  ) THROW 51073,''Lesson does not belong to enrollment course or completion time is invalid.'',1;
 END');

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyEnrollmentLessons_AppendOnly ON dbo.AcademyEnrollmentLessons
 AFTER INSERT,UPDATE,DELETE AS
 BEGIN
  SET NOCOUNT ON;
  IF EXISTS(SELECT 1 FROM deleted) THROW 51077,''Enrollment lesson snapshot is append-only.'',1;
  IF EXISTS(
   SELECT 1 FROM inserted i
   JOIN dbo.AcademyEnrollments e ON e.Id=i.EnrollmentId
   JOIN dbo.AcademyLessons l ON l.Id=i.LessonId
   WHERE e.CourseId<>l.CourseId
  ) THROW 51078,''Enrollment lesson must belong to the enrolled course.'',1;
 END');

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyEnrollments_Protect ON dbo.AcademyEnrollments
 AFTER UPDATE,DELETE AS
 BEGIN
  SET NOCOUNT ON;
  IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
   THROW 51074,''Academy enrollment cannot be deleted.'',1;
  IF EXISTS(
   SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
   WHERE i.CourseId<>d.CourseId OR i.KocUserId<>d.KocUserId OR i.EnrolledAtUtc<>d.EnrolledAtUtc
      OR i.CourseTitleSnapshot<>d.CourseTitleSnapshot OR i.KocDisplayNameSnapshot<>d.KocDisplayNameSnapshot
      OR d.CompletedAtUtc IS NOT NULL AND (i.CompletedAtUtc<>d.CompletedAtUtc OR i.CertificateCode<>d.CertificateCode)
  ) THROW 51075,''Enrollment identity and issued certificate are immutable.'',1;
  IF EXISTS(
   SELECT 1 FROM inserted i
   WHERE i.CompletedAtUtc IS NOT NULL AND (
    NOT EXISTS(SELECT 1 FROM dbo.AcademyEnrollmentLessons required WHERE required.EnrollmentId=i.Id)
    OR EXISTS(
     SELECT 1 FROM dbo.AcademyEnrollmentLessons required
     WHERE required.EnrollmentId=i.Id AND NOT EXISTS(
      SELECT 1 FROM dbo.AcademyLessonCompletions done
      WHERE done.EnrollmentId=required.EnrollmentId AND done.LessonId=required.LessonId
     )
    )
   )
  ) THROW 51079,''Certificate requires every lesson in the enrollment snapshot to be completed.'',1;
 END');

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyLessons_NoDelete ON dbo.AcademyLessons
 INSTEAD OF DELETE AS BEGIN SET NOCOUNT ON; THROW 51076,''Lessons cannot be deleted; set IsActive=0.'',1; END');

 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

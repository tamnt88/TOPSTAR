SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
 BEGIN TRANSACTION;

 IF OBJECT_ID('dbo.AcademyEnrollments','U') IS NULL OR OBJECT_ID('dbo.AcademyLessons','U') IS NULL
  THROW 51080, 'Run migration 71 before migration 72.', 1;

 IF (COL_LENGTH('dbo.AcademyEnrollments','CourseTitleSnapshot') IS NULL
     OR COL_LENGTH('dbo.AcademyEnrollments','KocDisplayNameSnapshot') IS NULL
     OR OBJECT_ID('dbo.AcademyEnrollmentLessons','U') IS NULL)
    AND EXISTS(SELECT 1 FROM dbo.AcademyEnrollments)
  THROW 51081, 'Existing academy enrollments require manual historical snapshot reconciliation; migration did not infer old facts.', 1;

 IF COL_LENGTH('dbo.AcademyEnrollments','CourseTitleSnapshot') IS NULL
  ALTER TABLE dbo.AcademyEnrollments ADD CourseTitleSnapshot NVARCHAR(160) NOT NULL
   CONSTRAINT DF_AcademyEnrollments_CourseTitleSnapshot DEFAULT(N'') WITH VALUES;
 IF COL_LENGTH('dbo.AcademyEnrollments','KocDisplayNameSnapshot') IS NULL
  ALTER TABLE dbo.AcademyEnrollments ADD KocDisplayNameSnapshot NVARCHAR(200) NOT NULL
   CONSTRAINT DF_AcademyEnrollments_KocNameSnapshot DEFAULT(N'') WITH VALUES;

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

 IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.AcademyEnrollments') AND name='CK_AcademyEnrollments_Snapshots')
  EXEC(N'ALTER TABLE dbo.AcademyEnrollments WITH CHECK
   ADD CONSTRAINT CK_AcademyEnrollments_Snapshots CHECK(
    LEN(LTRIM(RTRIM(CourseTitleSnapshot))) BETWEEN 3 AND 160 AND
    LEN(LTRIM(RTRIM(KocDisplayNameSnapshot))) BETWEEN 1 AND 200);
   ALTER TABLE dbo.AcademyEnrollments CHECK CONSTRAINT CK_AcademyEnrollments_Snapshots;');

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyEnrollmentLessons_AppendOnly ON dbo.AcademyEnrollmentLessons
 AFTER INSERT,UPDATE,DELETE AS BEGIN SET NOCOUNT ON;
  IF EXISTS(SELECT 1 FROM deleted) THROW 51077,''Enrollment lesson snapshot is append-only.'',1;
  IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.AcademyEnrollments e ON e.Id=i.EnrollmentId
   JOIN dbo.AcademyLessons l ON l.Id=i.LessonId WHERE e.CourseId<>l.CourseId)
   THROW 51078,''Enrollment lesson must belong to the enrolled course.'',1;
 END');

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyLessonCompletions_Protect ON dbo.AcademyLessonCompletions
 AFTER INSERT,UPDATE,DELETE AS BEGIN SET NOCOUNT ON;
  IF EXISTS(SELECT 1 FROM deleted) THROW 51072,''Academy lesson completion is append-only.'',1;
  IF EXISTS(SELECT 1 FROM inserted i JOIN dbo.AcademyEnrollments e ON e.Id=i.EnrollmentId
   LEFT JOIN dbo.AcademyEnrollmentLessons r ON r.EnrollmentId=i.EnrollmentId AND r.LessonId=i.LessonId
   WHERE r.LessonId IS NULL OR i.CompletedAtUtc<e.EnrolledAtUtc)
   THROW 51073,''Lesson is outside the enrollment snapshot or completion time is invalid.'',1;
 END');

 EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_AcademyEnrollments_Protect ON dbo.AcademyEnrollments
 AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON;
  IF EXISTS(SELECT 1 FROM deleted d LEFT JOIN inserted i ON i.Id=d.Id WHERE i.Id IS NULL)
   THROW 51074,''Academy enrollment cannot be deleted.'',1;
  IF EXISTS(SELECT 1 FROM inserted i JOIN deleted d ON d.Id=i.Id
   WHERE i.CourseId<>d.CourseId OR i.KocUserId<>d.KocUserId OR i.EnrolledAtUtc<>d.EnrolledAtUtc
    OR i.CourseTitleSnapshot<>d.CourseTitleSnapshot OR i.KocDisplayNameSnapshot<>d.KocDisplayNameSnapshot
    OR d.CompletedAtUtc IS NOT NULL AND (i.CompletedAtUtc<>d.CompletedAtUtc OR i.CertificateCode<>d.CertificateCode))
   THROW 51075,''Enrollment identity and issued certificate are immutable.'',1;
  IF EXISTS(SELECT 1 FROM inserted i WHERE i.CompletedAtUtc IS NOT NULL AND (
   NOT EXISTS(SELECT 1 FROM dbo.AcademyEnrollmentLessons r WHERE r.EnrollmentId=i.Id)
   OR EXISTS(SELECT 1 FROM dbo.AcademyEnrollmentLessons r WHERE r.EnrollmentId=i.Id
    AND NOT EXISTS(SELECT 1 FROM dbo.AcademyLessonCompletions d
     WHERE d.EnrollmentId=r.EnrollmentId AND d.LessonId=r.LessonId))))
   THROW 51079,''Certificate requires all snapshotted lessons to be completed.'',1;
 END');

 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;

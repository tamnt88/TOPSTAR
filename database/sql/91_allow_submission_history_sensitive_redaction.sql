SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS
    (
        SELECT 1
        FROM sys.check_constraints
        WHERE parent_object_id = OBJECT_ID(N'dbo.SubmissionHistories')
          AND name = N'CK_SubmissionHistories_EventType'
    )
        ALTER TABLE dbo.SubmissionHistories DROP CONSTRAINT CK_SubmissionHistories_EventType;

    ALTER TABLE dbo.SubmissionHistories WITH CHECK ADD CONSTRAINT CK_SubmissionHistories_EventType
        CHECK (EventType IN
        (
            'MigratedSnapshot',
            'Submitted',
            'Reviewed',
            'Replaced',
            'SensitiveDataRedacted'
        ));

    EXEC(N'
CREATE OR ALTER TRIGGER dbo.TR_SubmissionHistories_AppendOnly
ON dbo.SubmissionHistories
AFTER UPDATE, DELETE
AS
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM deleted d
        LEFT JOIN inserted i ON i.Id = d.Id
        WHERE i.Id IS NULL
    )
        THROW 51010, ''SubmissionHistories is append-only; delete is forbidden.'', 1;

    IF EXISTS
    (
        SELECT 1
        FROM deleted d
        JOIN inserted i ON i.Id = d.Id
        WHERE
            i.SubmissionId <> d.SubmissionId OR
            i.EventType <> d.EventType OR
            i.RevisionNo <> d.RevisionNo OR
            i.Status <> d.Status OR
            ISNULL(i.ReviewComment, N'''') <> ISNULL(d.ReviewComment, N'''') OR
            ISNULL(i.ActorUserId, ''00000000-0000-0000-0000-000000000000'') <>
                ISNULL(d.ActorUserId, ''00000000-0000-0000-0000-000000000000'') OR
            i.CreatedAtUtc <> d.CreatedAtUtc OR
            (i.SubmissionUrl IS NOT NULL AND ISNULL(i.SubmissionUrl, N'''') <> ISNULL(d.SubmissionUrl, N'''')) OR
            (i.SubmissionNote IS NOT NULL AND ISNULL(i.SubmissionNote, N'''') <> ISNULL(d.SubmissionNote, N'''')) OR
            (i.AttachmentUrl IS NOT NULL AND ISNULL(i.AttachmentUrl, N'''') <> ISNULL(d.AttachmentUrl, N'''')) OR
            (i.AttachmentName IS NOT NULL AND ISNULL(i.AttachmentName, N'''') <> ISNULL(d.AttachmentName, N''''))
    )
        THROW 51011, ''SubmissionHistories only permits nullification of sensitive snapshot fields.'', 1;
END;
');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    c.name AS ConstraintName,
    c.definition AS ConstraintDefinition,
    OBJECT_NAME(t.object_id) AS TriggerName,
    t.is_disabled AS TriggerDisabled
FROM sys.check_constraints c
CROSS JOIN sys.triggers t
WHERE c.parent_object_id = OBJECT_ID(N'dbo.SubmissionHistories')
  AND c.name = N'CK_SubmissionHistories_EventType'
  AND t.object_id = OBJECT_ID(N'dbo.TR_SubmissionHistories_AppendOnly');

SET NOCOUNT ON;
SET XACT_ABORT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.Submissions','SubmissionNote') IS NULL
    ALTER TABLE dbo.Submissions ADD SubmissionNote NVARCHAR(2000) NULL;
IF COL_LENGTH('dbo.Submissions','AttachmentUrl') IS NULL
    ALTER TABLE dbo.Submissions ADD AttachmentUrl NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.Submissions','AttachmentName') IS NULL
    ALTER TABLE dbo.Submissions ADD AttachmentName NVARCHAR(260) NULL;

IF COL_LENGTH('dbo.SubmissionHistories','SubmissionNote') IS NULL
    ALTER TABLE dbo.SubmissionHistories ADD SubmissionNote NVARCHAR(2000) NULL;
IF COL_LENGTH('dbo.SubmissionHistories','AttachmentUrl') IS NULL
    ALTER TABLE dbo.SubmissionHistories ADD AttachmentUrl NVARCHAR(1000) NULL;
IF COL_LENGTH('dbo.SubmissionHistories','AttachmentName') IS NULL
    ALTER TABLE dbo.SubmissionHistories ADD AttachmentName NVARCHAR(260) NULL;

COMMIT TRANSACTION;

SELECT
    COL_LENGTH('dbo.Submissions','SubmissionNote') SubmissionNoteBytes,
    COL_LENGTH('dbo.Submissions','AttachmentUrl') AttachmentUrlBytes,
    COL_LENGTH('dbo.SubmissionHistories','SubmissionNote') HistoryNoteBytes,
    COL_LENGTH('dbo.SubmissionHistories','AttachmentUrl') HistoryAttachmentUrlBytes;

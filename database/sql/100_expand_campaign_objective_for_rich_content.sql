SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Campaigns', 'Objective') IS NULL
        THROW 51000, N'Không tìm thấy cột dbo.Campaigns.Objective.', 1;

    ALTER TABLE dbo.Campaigns ALTER COLUMN Objective NVARCHAR(MAX) NULL;

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT
    c.name AS ColumnName,
    t.name AS DataType,
    c.max_length AS MaxLength
FROM sys.columns c
JOIN sys.types t ON t.user_type_id = c.user_type_id
WHERE c.object_id = OBJECT_ID('dbo.Campaigns')
  AND c.name = 'Objective';

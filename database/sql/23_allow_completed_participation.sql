SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @ConstraintName SYSNAME;
SELECT TOP 1 @ConstraintName = cc.[name]
FROM sys.check_constraints cc
WHERE cc.[parent_object_id] = OBJECT_ID('dbo.Participations')
  AND cc.[definition] LIKE '%Status%';

IF @ConstraintName IS NOT NULL
BEGIN
    DECLARE @DropSql NVARCHAR(500) = N'ALTER TABLE [dbo].[Participations] DROP CONSTRAINT ' + QUOTENAME(@ConstraintName) + N';';
    EXEC sp_executesql @DropSql;
END;

IF NOT EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE [parent_object_id] = OBJECT_ID('dbo.Participations')
      AND [name] = 'CK_Participations_Status'
)
BEGIN
    ALTER TABLE [dbo].[Participations] WITH CHECK
    ADD CONSTRAINT [CK_Participations_Status]
        CHECK ([Status] IN ('Confirmed','Completed','Cancelled'));
END;

COMMIT TRANSACTION;

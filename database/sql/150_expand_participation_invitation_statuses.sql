IF EXISTS (
    SELECT 1 FROM sys.check_constraints
    WHERE name = 'CK_Participations_Status'
      AND parent_object_id = OBJECT_ID('dbo.Participations')
)
    ALTER TABLE dbo.Participations DROP CONSTRAINT CK_Participations_Status;
GO

ALTER TABLE dbo.Participations WITH CHECK ADD CONSTRAINT CK_Participations_Status
CHECK (Status IN ('Invited', 'Confirmed', 'Declined', 'Removed', 'Completed', 'Cancelled'));
GO

ALTER TABLE dbo.Participations CHECK CONSTRAINT CK_Participations_Status;
GO

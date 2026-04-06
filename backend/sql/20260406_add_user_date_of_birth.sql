-- 20260406_add_user_date_of_birth.sql
-- Add date of birth for KOC profile basic info

IF COL_LENGTH('Users', 'DateOfBirth') IS NULL
BEGIN
    ALTER TABLE [dbo].[Users]
    ADD [DateOfBirth] DATETIME2 NULL;
END

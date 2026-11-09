SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF COL_LENGTH('dbo.Schedules','IsLateCancellation') IS NULL
        EXEC(N'ALTER TABLE dbo.Schedules ADD IsLateCancellation BIT NOT NULL CONSTRAINT DF_Schedules_IsLateCancellation DEFAULT(0);');
    IF COL_LENGTH('dbo.Schedules','LateCancellationThresholdHours') IS NULL
        EXEC(N'ALTER TABLE dbo.Schedules ADD LateCancellationThresholdHours INT NULL;');
    IF COL_LENGTH('dbo.Schedules','CancelledAtUtc') IS NULL
        EXEC(N'ALTER TABLE dbo.Schedules ADD CancelledAtUtc DATETIME2(7) NULL;');
    IF COL_LENGTH('dbo.Schedules','CancelledByUserId') IS NULL
        EXEC(N'ALTER TABLE dbo.Schedules ADD CancelledByUserId UNIQUEIDENTIFIER NULL;');

    EXEC(N'UPDATE dbo.Schedules SET CancelledAtUtc=COALESCE(CancelledAtUtc,UpdatedAtUtc,CreatedAtUtc) WHERE Status=''Cancelled'' AND CancelledAtUtc IS NULL;');

    IF NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE parent_object_id=OBJECT_ID('dbo.Schedules') AND name='FK_Schedules_CancelledByUser')
        EXEC(N'ALTER TABLE dbo.Schedules ADD CONSTRAINT FK_Schedules_CancelledByUser FOREIGN KEY(CancelledByUserId) REFERENCES dbo.Users(Id);');
    IF NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE parent_object_id=OBJECT_ID('dbo.Schedules') AND name='CK_Schedules_CancellationMetadata')
        EXEC(N'ALTER TABLE dbo.Schedules WITH CHECK ADD CONSTRAINT CK_Schedules_CancellationMetadata CHECK((Status<>''Cancelled'' AND IsLateCancellation=0 AND LateCancellationThresholdHours IS NULL AND CancelledAtUtc IS NULL AND CancelledByUserId IS NULL) OR (Status=''Cancelled'' AND CancelledAtUtc IS NOT NULL AND ((IsLateCancellation=0 AND LateCancellationThresholdHours IS NULL) OR (IsLateCancellation=1 AND LateCancellationThresholdHours BETWEEN 1 AND 168))));');

    IF OBJECT_ID('dbo.ScheduleReminderEvents','U') IS NULL
    BEGIN
        CREATE TABLE dbo.ScheduleReminderEvents(
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ScheduleReminderEvents PRIMARY KEY,
            ScheduleId UNIQUEIDENTIFIER NOT NULL,
            ReminderCode NVARCHAR(20) NOT NULL,
            StartsAtUtcSnapshot DATETIME2(7) NOT NULL,
            SentAtUtc DATETIME2(7) NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_ScheduleReminderEvents_Schedule FOREIGN KEY(ScheduleId) REFERENCES dbo.Schedules(Id),
            CONSTRAINT CK_ScheduleReminderEvents_Code CHECK(ReminderCode IN('REMINDER_24H','REMINDER_2H')),
            CONSTRAINT CK_ScheduleReminderEvents_Time CHECK(SentAtUtc=CreatedAtUtc AND SentAtUtc<=StartsAtUtcSnapshot),
            CONSTRAINT UX_ScheduleReminderEvents_Schedule_Code_Start UNIQUE(ScheduleId,ReminderCode,StartsAtUtcSnapshot));
        CREATE INDEX IX_ScheduleReminderEvents_SentAt ON dbo.ScheduleReminderEvents(SentAtUtc DESC);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ScheduleReminderEvents_AppendOnly ON dbo.ScheduleReminderEvents AFTER UPDATE,DELETE AS BEGIN SET NOCOUNT ON; THROW 51000, ''Schedule reminder history is append-only.'', 1; END;');
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

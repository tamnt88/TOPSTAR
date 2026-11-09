SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.ShipmentTrackingEvents', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ShipmentTrackingEvents
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ShipmentTrackingEvents PRIMARY KEY,
            ShipmentId UNIQUEIDENTIFIER NOT NULL,
            EventCode VARCHAR(30) NOT NULL,
            Description NVARCHAR(1000) NOT NULL,
            Location NVARCHAR(300) NULL,
            OccurredAtUtc DATETIME2(7) NOT NULL,
            Source VARCHAR(30) NOT NULL,
            CreatedByUserId UNIQUEIDENTIFIER NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_ShipmentTrackingEvents_Shipment FOREIGN KEY(ShipmentId) REFERENCES dbo.Shipments(Id),
            CONSTRAINT FK_ShipmentTrackingEvents_Creator FOREIGN KEY(CreatedByUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_ShipmentTrackingEvents_Code CHECK(EventCode IN ('PickedUp','AtHub','OutForDelivery','DeliveryAttempt','Exception','Other')),
            CONSTRAINT CK_ShipmentTrackingEvents_Description CHECK(LEN(LTRIM(RTRIM(Description))) BETWEEN 2 AND 1000),
            CONSTRAINT CK_ShipmentTrackingEvents_Source CHECK(Source IN ('ManualBrand','CarrierWebhook')),
            CONSTRAINT CK_ShipmentTrackingEvents_Creator CHECK(
                (Source='ManualBrand' AND CreatedByUserId IS NOT NULL)
                OR (Source='CarrierWebhook'))
        );
        CREATE INDEX IX_ShipmentTrackingEvents_Shipment_Occurred
            ON dbo.ShipmentTrackingEvents(ShipmentId, OccurredAtUtc DESC)
            INCLUDE(EventCode, Location, Source);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ShipmentTrackingEvents_AppendOnly
    ON dbo.ShipmentTrackingEvents AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N''Checkpoint vận chuyển chỉ được phép ghi thêm.'', 1;
    END;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ShipmentTrackingEvents_ValidateInsert
    ON dbo.ShipmentTrackingEvents AFTER INSERT AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS(
            SELECT 1 FROM inserted i
            JOIN dbo.Shipments s ON s.Id=i.ShipmentId
            JOIN dbo.Participations p ON p.Id=s.ParticipationId
            JOIN dbo.Campaigns c ON c.Id=p.CampaignId
            JOIN dbo.BrandProfiles b ON b.Id=c.BrandProfileId
            WHERE i.OccurredAtUtc<s.CreatedAtUtc OR i.OccurredAtUtc>DATEADD(MINUTE,10,SYSUTCDATETIME())
               OR (i.Source=''ManualBrand'' AND (i.CreatedByUserId<>b.UserId OR s.Status NOT IN (''InTransit'',''Issue'')))
        )
            THROW 51000, N''Checkpoint sai quyền, trạng thái hoặc thời điểm.'', 1;
    END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

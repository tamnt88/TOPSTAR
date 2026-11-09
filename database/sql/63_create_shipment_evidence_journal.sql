SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.ShipmentEvidence', 'U') IS NULL
    BEGIN
        CREATE TABLE dbo.ShipmentEvidence
        (
            Id UNIQUEIDENTIFIER NOT NULL CONSTRAINT PK_ShipmentEvidence PRIMARY KEY,
            ShipmentId UNIQUEIDENTIFIER NOT NULL,
            EvidenceType VARCHAR(30) NOT NULL,
            FileUrl NVARCHAR(1000) NOT NULL,
            Caption NVARCHAR(500) NULL,
            UploadedByUserId UNIQUEIDENTIFIER NOT NULL,
            ActorRole VARCHAR(10) NOT NULL,
            CreatedAtUtc DATETIME2(7) NOT NULL,
            CONSTRAINT FK_ShipmentEvidence_Shipment FOREIGN KEY(ShipmentId) REFERENCES dbo.Shipments(Id),
            CONSTRAINT FK_ShipmentEvidence_Uploader FOREIGN KEY(UploadedByUserId) REFERENCES dbo.Users(Id),
            CONSTRAINT CK_ShipmentEvidence_Role CHECK(ActorRole IN ('BRAND','KOC')),
            CONSTRAINT CK_ShipmentEvidence_Type CHECK(EvidenceType IN ('BrandShipmentProof','KocReceiptProof','KocIssueProof')),
            CONSTRAINT CK_ShipmentEvidence_RoleType CHECK(
                (ActorRole='BRAND' AND EvidenceType='BrandShipmentProof')
                OR (ActorRole='KOC' AND EvidenceType IN ('KocReceiptProof','KocIssueProof'))),
            CONSTRAINT CK_ShipmentEvidence_FileUrl CHECK(
                FileUrl LIKE '/uploads/shipment-evidence/%' AND LEN(FileUrl) <= 1000)
        );
        CREATE INDEX IX_ShipmentEvidence_Shipment_Created
            ON dbo.ShipmentEvidence(ShipmentId, CreatedAtUtc)
            INCLUDE(ActorRole, EvidenceType, FileUrl);
    END;

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ShipmentEvidence_AppendOnly
    ON dbo.ShipmentEvidence AFTER UPDATE, DELETE AS
    BEGIN
        SET NOCOUNT ON;
        THROW 51000, N''Bằng chứng vận chuyển chỉ được phép ghi thêm.'', 1;
    END;');

    EXEC(N'CREATE OR ALTER TRIGGER dbo.TR_ShipmentEvidence_ValidateOwner
    ON dbo.ShipmentEvidence AFTER INSERT AS
    BEGIN
        SET NOCOUNT ON;
        IF EXISTS(
            SELECT 1 FROM inserted i
            JOIN dbo.Shipments s ON s.Id=i.ShipmentId
            JOIN dbo.Participations p ON p.Id=s.ParticipationId
            JOIN dbo.Campaigns c ON c.Id=p.CampaignId
            JOIN dbo.BrandProfiles b ON b.Id=c.BrandProfileId
            JOIN dbo.KocProfiles k ON k.Id=p.KocProfileId
            WHERE (i.ActorRole=''BRAND'' AND i.UploadedByUserId<>b.UserId)
               OR (i.ActorRole=''KOC'' AND i.UploadedByUserId<>k.UserId)
        )
            THROW 51000, N''Người tải ảnh không thuộc vận đơn.'', 1;
        IF EXISTS(
            SELECT 1 FROM dbo.ShipmentEvidence e
            JOIN inserted i ON i.ShipmentId=e.ShipmentId AND i.ActorRole=e.ActorRole
            GROUP BY e.ShipmentId,e.ActorRole HAVING COUNT(*)>10
        )
            THROW 51000, N''Mỗi bên chỉ được lưu tối đa 10 ảnh cho một vận đơn.'', 1;
    END;');

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

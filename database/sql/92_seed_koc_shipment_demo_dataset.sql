/*
  Seed bộ dữ liệu vận đơn dành cho màn hình /Koc/Shipments.
  - Mặc định nhắm KOC seed.koc.0001@seed.topstar.local.
  - Idempotent: toàn bộ khóa được tạo ổn định bằng MD5; có thể chạy lại.
  - Không sửa/xóa journal cũ; chỉ ghi thêm các aggregate seed chưa tồn tại.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID('dbo.Shipments','U') IS NULL
       OR OBJECT_ID('dbo.ShipmentStatusHistories','U') IS NULL
       OR OBJECT_ID('dbo.ShipmentTrackingEvents','U') IS NULL
       OR OBJECT_ID('dbo.ShipmentEvidence','U') IS NULL
        THROW 51092, N'Chưa đủ schema shipment. Hãy chạy migration 31, 63 và 64 trước.', 1;

    DECLARE @Now DATETIME2(7)=SYSUTCDATETIME();
    DECLARE @KocUserId UNIQUEIDENTIFIER, @KocProfileId UNIQUEIDENTIFIER;
    SELECT TOP(1) @KocUserId=u.Id,@KocProfileId=k.Id
    FROM dbo.Users u JOIN dbo.KocProfiles k ON k.UserId=u.Id
    WHERE u.Email='seed.koc.0001@seed.topstar.local';
    IF @KocUserId IS NULL
        THROW 51092, N'Không tìm thấy seed.koc.0001@seed.topstar.local. Hãy chạy 84 trước.', 1;

    CREATE TABLE #ShipmentSeed
    (
        RowNo INT NOT NULL PRIMARY KEY,
        CampaignId UNIQUEIDENTIFIER NOT NULL,
        BrandUserId UNIQUEIDENTIFIER NOT NULL,
        ParticipationId UNIQUEIDENTIFIER NOT NULL,
        ShipmentId UNIQUEIDENTIFIER NOT NULL,
        FinalStatus VARCHAR(30) NOT NULL
    );

    ;WITH Candidate AS
    (
        SELECT TOP(6) c.Id CampaignId,b.UserId BrandUserId,
               ROW_NUMBER() OVER(ORDER BY c.CreatedAtUtc DESC,c.Id) RowNo
        FROM dbo.Campaigns c
        JOIN dbo.BrandProfiles b ON b.Id=c.BrandProfileId
        WHERE c.Title LIKE N'Chiến dịch Seed %'
          AND NOT EXISTS(SELECT 1 FROM dbo.Participations p WHERE p.CampaignId=c.Id AND p.KocProfileId=@KocProfileId)
        ORDER BY c.CreatedAtUtc DESC,c.Id
    )
    INSERT #ShipmentSeed(RowNo,CampaignId,BrandUserId,ParticipationId,ShipmentId,FinalStatus)
    SELECT RowNo,CampaignId,BrandUserId,
           CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIPMENT-PART-',CONVERT(VARCHAR(36),@KocProfileId),'-',RowNo))),
           CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIPMENT-DEMO-',CONVERT(VARCHAR(36),@KocProfileId),'-',RowNo))),
           CASE RowNo WHEN 1 THEN 'InTransit' WHEN 2 THEN 'Delivered' WHEN 3 THEN 'Issue'
                      WHEN 4 THEN 'Created' WHEN 5 THEN 'Returned' ELSE 'InTransit' END
    FROM Candidate;

    IF (SELECT COUNT(*) FROM #ShipmentSeed)<6
        THROW 51092, N'Không đủ 6 campaign seed chưa tham gia để tạo bộ vận đơn. Kiểm tra dữ liệu migration 84.', 1;

    UPDATE c SET RequiresShipment=1
    FROM dbo.Campaigns c JOIN #ShipmentSeed s ON s.CampaignId=c.Id
    WHERE c.RequiresShipment=0;

    INSERT dbo.Participations(Id,CampaignId,KocProfileId,Status,ConfirmedAtUtc,CreatedAtUtc)
    SELECT s.ParticipationId,s.CampaignId,@KocProfileId,'Confirmed',
           DATEADD(DAY,-(10-s.RowNo),@Now),DATEADD(DAY,-(10-s.RowNo),@Now)
    FROM #ShipmentSeed s
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Participations p WHERE p.Id=s.ParticipationId);

    INSERT dbo.ParticipationStatusHistories(Id,ParticipationId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIPMENT-PART-HISTORY-',s.RowNo))),
           s.ParticipationId,NULL,'Confirmed',N'Brand xác nhận KOC nhận sản phẩm mẫu.',s.BrandUserId,
           DATEADD(DAY,-(10-s.RowNo),@Now)
    FROM #ShipmentSeed s
    WHERE NOT EXISTS(SELECT 1 FROM dbo.ParticipationStatusHistories h
                     WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIPMENT-PART-HISTORY-',s.RowNo))));

    INSERT dbo.Shipments
    (Id,ParticipationId,ReceiverName,ReceiverPhone,StreetAddress,WardId,ProvinceId,AddressV2,WardCodeV2,IsNewAddress,
     TrackingCode,Carrier,Status,IssueNote,CreatedAtUtc,UpdatedAtUtc)
    SELECT s.ShipmentId,s.ParticipationId,k.DisplayName,u.Phone,
           COALESCE(NULLIF(k.StreetAddress,''),N'11 Đường Seed, Phường Bảy Hiền'),
           k.WardId,k.ProvinceId,COALESCE(NULLIF(k.AddressV2,''),NULLIF(k.StreetAddress,''),N'11 Đường Seed, Phường Bảy Hiền, Hồ Chí Minh'),
           k.WardCodeV2,1,
           CASE WHEN s.FinalStatus='Created' THEN NULL ELSE CONCAT('TSP',FORMAT(s.RowNo,'00000000')) END,
           CASE WHEN s.FinalStatus='Created' THEN NULL WHEN s.RowNo%2=0 THEN N'GHN' ELSE N'Giao Hàng Tiết Kiệm' END,
           s.FinalStatus,
           CASE WHEN s.FinalStatus='Issue' THEN N'Kiện hàng có dấu hiệu móp góc, KOC đang chờ Brand hướng dẫn xử lý.'
                WHEN s.FinalStatus='Returned' THEN NULL ELSE NULL END,
           DATEADD(DAY,-(9-s.RowNo),@Now),DATEADD(HOUR,-s.RowNo,@Now)
    FROM #ShipmentSeed s
    JOIN dbo.KocProfiles k ON k.Id=@KocProfileId JOIN dbo.Users u ON u.Id=k.UserId
    WHERE NOT EXISTS(SELECT 1 FROM dbo.Shipments x WHERE x.Id=s.ShipmentId);

    /* Journal trạng thái: Created -> InTransit -> trạng thái cuối, không ghi đè lịch sử. */
    INSERT dbo.ShipmentStatusHistories(Id,ShipmentId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-HISTORY-CREATED-',s.RowNo))),
           s.ShipmentId,NULL,'Created',N'Brand đã chốt thông tin nhận sản phẩm.',s.BrandUserId,
           DATEADD(DAY,-(9-s.RowNo),@Now)
    FROM #ShipmentSeed s
    WHERE NOT EXISTS(SELECT 1 FROM dbo.ShipmentStatusHistories h WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-HISTORY-CREATED-',s.RowNo))));

    INSERT dbo.ShipmentStatusHistories(Id,ShipmentId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-HISTORY-TRANSIT-',s.RowNo))),
           s.ShipmentId,'Created','InTransit',CONCAT(N'Đã bàn giao cho đơn vị vận chuyển · TSP',FORMAT(s.RowNo,'00000000')),
           s.BrandUserId,DATEADD(DAY,-(7-s.RowNo),@Now)
    FROM #ShipmentSeed s WHERE s.FinalStatus<>'Created'
      AND NOT EXISTS(SELECT 1 FROM dbo.ShipmentStatusHistories h WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-HISTORY-TRANSIT-',s.RowNo))));

    INSERT dbo.ShipmentStatusHistories(Id,ShipmentId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-HISTORY-FINAL-',s.RowNo))),
           s.ShipmentId,'InTransit',s.FinalStatus,
           CASE s.FinalStatus WHEN 'Delivered' THEN N'KOC xác nhận đã nhận đủ sản phẩm.'
                WHEN 'Issue' THEN N'Kiện hàng có dấu hiệu móp góc, KOC đang chờ Brand hướng dẫn xử lý.'
                WHEN 'Returned' THEN N'Brand xác nhận hoàn hàng sau khi xử lý sự cố.' END,
           CASE WHEN s.FinalStatus IN('Delivered','Issue') THEN @KocUserId ELSE s.BrandUserId END,
           DATEADD(HOUR,-s.RowNo,@Now)
    FROM #ShipmentSeed s WHERE s.FinalStatus IN('Delivered','Issue','Returned')
      AND NOT EXISTS(SELECT 1 FROM dbo.ShipmentStatusHistories h WHERE h.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-HISTORY-FINAL-',s.RowNo))));

    /* Tracking dùng nguồn webhook để phản ánh dữ liệu từ hãng vận chuyển và hợp lệ với trạng thái cuối. */
    INSERT dbo.ShipmentTrackingEvents(Id,ShipmentId,EventCode,Description,Location,OccurredAtUtc,Source,CreatedByUserId,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-TRACK-PICKUP-',s.RowNo))),
           s.ShipmentId,'PickedUp',N'Đơn vị vận chuyển đã lấy kiện hàng từ Brand.',N'Kho Brand · Hồ Chí Minh',
           DATEADD(DAY,-(7-s.RowNo),@Now),'CarrierWebhook',NULL,DATEADD(DAY,-(7-s.RowNo),@Now)
    FROM #ShipmentSeed s WHERE s.FinalStatus<>'Created'
      AND NOT EXISTS(SELECT 1 FROM dbo.ShipmentTrackingEvents t WHERE t.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-TRACK-PICKUP-',s.RowNo))));

    INSERT dbo.ShipmentTrackingEvents(Id,ShipmentId,EventCode,Description,Location,OccurredAtUtc,Source,CreatedByUserId,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-TRACK-HUB-',s.RowNo))),
           s.ShipmentId,CASE WHEN s.FinalStatus='Issue' THEN 'Exception' ELSE 'AtHub' END,
           CASE WHEN s.FinalStatus='Issue' THEN N'Kiện hàng tạm dừng để kiểm tra ngoại quan.' ELSE N'Kiện hàng đã đến kho trung chuyển gần người nhận.' END,
           N'Kho phân loại Tân Bình',DATEADD(HOUR,-(30+s.RowNo),@Now),'CarrierWebhook',NULL,DATEADD(HOUR,-(30+s.RowNo),@Now)
    FROM #ShipmentSeed s WHERE s.FinalStatus<>'Created'
      AND NOT EXISTS(SELECT 1 FROM dbo.ShipmentTrackingEvents t WHERE t.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-TRACK-HUB-',s.RowNo))));

    INSERT dbo.ShipmentTrackingEvents(Id,ShipmentId,EventCode,Description,Location,OccurredAtUtc,Source,CreatedByUserId,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-TRACK-LAST-',s.RowNo))),
           s.ShipmentId,'OutForDelivery',N'Nhân viên giao hàng đang giao kiện đến địa chỉ nhận.',N'Quận Tân Bình, Hồ Chí Minh',
           DATEADD(HOUR,-(8+s.RowNo),@Now),'CarrierWebhook',NULL,DATEADD(HOUR,-(8+s.RowNo),@Now)
    FROM #ShipmentSeed s WHERE s.FinalStatus IN('InTransit','Delivered','Returned')
      AND NOT EXISTS(SELECT 1 FROM dbo.ShipmentTrackingEvents t WHERE t.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-TRACK-LAST-',s.RowNo))));

    /* Ảnh seed có file tĩnh đi kèm source để không tạo liên kết hỏng. */
    INSERT dbo.ShipmentEvidence(Id,ShipmentId,EvidenceType,FileUrl,Caption,UploadedByUserId,ActorRole,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-EVIDENCE-BRAND-',s.RowNo))),
           s.ShipmentId,'BrandShipmentProof','/uploads/shipment-evidence/seed-brand-package.svg',
           N'Brand chụp kiện hàng trước khi bàn giao vận chuyển.',s.BrandUserId,'BRAND',DATEADD(DAY,-(7-s.RowNo),@Now)
    FROM #ShipmentSeed s WHERE s.FinalStatus<>'Created'
      AND NOT EXISTS(SELECT 1 FROM dbo.ShipmentEvidence e WHERE e.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-EVIDENCE-BRAND-',s.RowNo))));

    INSERT dbo.ShipmentEvidence(Id,ShipmentId,EvidenceType,FileUrl,Caption,UploadedByUserId,ActorRole,CreatedAtUtc)
    SELECT CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-EVIDENCE-KOC-',s.RowNo))),
           s.ShipmentId,CASE WHEN s.FinalStatus='Issue' THEN 'KocIssueProof' ELSE 'KocReceiptProof' END,
           CASE WHEN s.FinalStatus='Issue' THEN '/uploads/shipment-evidence/seed-koc-issue.svg' ELSE '/uploads/shipment-evidence/seed-koc-received.svg' END,
           CASE WHEN s.FinalStatus='Issue' THEN N'KOC ghi nhận tình trạng kiện hàng khi nhận.' ELSE N'KOC xác nhận kiện hàng đã được nhận.' END,
           @KocUserId,'KOC',DATEADD(HOUR,-s.RowNo,@Now)
    FROM #ShipmentSeed s WHERE s.FinalStatus IN('Delivered','Issue')
      AND NOT EXISTS(SELECT 1 FROM dbo.ShipmentEvidence e WHERE e.Id=CONVERT(UNIQUEIDENTIFIER,HASHBYTES('MD5',CONCAT('TOPSTAR-SHIP-EVIDENCE-KOC-',s.RowNo))));

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

SELECT s.Status,COUNT(*) AS ShipmentCount
FROM dbo.Shipments s JOIN dbo.Participations p ON p.Id=s.ParticipationId
JOIN dbo.KocProfiles k ON k.Id=p.KocProfileId JOIN dbo.Users u ON u.Id=k.UserId
WHERE u.Email='seed.koc.0001@seed.topstar.local'
GROUP BY s.Status ORDER BY s.Status;

/*
 TOPSTAR - Brand portal QA dataset
 Target account: brand@topstar.local / Topstar@123

 Safety:
 - Idempotent deterministic IDs.
 - Does not delete or replace existing business rows.
 - Only unlocks/completes the dedicated demo Brand account.
 - Seed campaigns are inserted only when their deterministic IDs do not exist.
*/
SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET ARITHABORT ON;
SET NUMERIC_ROUNDABORT OFF;

BEGIN TRY
    BEGIN TRANSACTION;

    DECLARE @Now datetime2 = SYSUTCDATETIME();
    DECLARE @BrandUserId uniqueidentifier = '465397EA-DDBD-47DC-AF44-BF9575B4D08E';
    DECLARE @BrandProfileId uniqueidentifier = 'A3C37ED9-F881-4FC8-AB2D-C18FFAA5E7F2';
    DECLARE @ProvinceId uniqueidentifier;
    DECLARE @WardId uniqueidentifier;

    SELECT TOP (1)
        @ProvinceId = p.Id,
        @WardId = w.Id
    FROM dbo.Provinces p
    JOIN dbo.Wards w ON w.ProvinceId = p.Id AND w.IsActive = 1
    WHERE p.IsActive = 1
    ORDER BY CASE WHEN p.Name LIKE N'%Hồ Chí Minh%' THEN 0 ELSE 1 END, p.Name, w.Name;

    IF NOT EXISTS (SELECT 1 FROM dbo.Users WHERE Id = @BrandUserId AND Email = N'brand@topstar.local')
        THROW 51000, N'Không tìm thấy tài khoản demo brand@topstar.local. Hãy chạy seed nền trước.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.BrandProfiles WHERE Id = @BrandProfileId AND UserId = @BrandUserId)
        THROW 51001, N'Không tìm thấy BrandProfile demo. Hãy chạy seed nền trước.', 1;

    UPDATE dbo.Users
    SET Status = N'Active',
        FailedLoginCount = 0,
        LockoutEndAtUtc = NULL,
        Phone = COALESCE(NULLIF(Phone, N''), N'0900000002'),
        EmailConfirmed = 1,
        PhoneConfirmed = 1,
        UpdatedAtUtc = @Now
    WHERE Id = @BrandUserId;

    UPDATE dbo.BrandProfiles
    SET CompanyName = COALESCE(NULLIF(CompanyName, N''), N'Topstar Brand Demo'),
        ContactName = COALESCE(NULLIF(ContactName, N''), N'Ms. Brand Owner'),
        ContactPhone = COALESCE(NULLIF(ContactPhone, N''), N'0900000002'),
        StreetAddress = COALESCE(NULLIF(StreetAddress, N''), N'77 Tạ Hiện'),
        ProvinceId = COALESCE(ProvinceId, @ProvinceId),
        WardId = COALESCE(WardId, @WardId),
        TaxCode = COALESCE(NULLIF(TaxCode, N''), N'0304995131'),
        TaxCompanyName = COALESCE(NULLIF(TaxCompanyName, N''), N'CÔNG TY TNHH TOPSTAR BRAND DEMO'),
        TaxInvoiceAddress = COALESCE(NULLIF(TaxInvoiceAddress, N''), N'77 Tạ Hiện, Thành phố Hồ Chí Minh'),
        BusinessLicenseFileUrl = COALESCE(NULLIF(BusinessLicenseFileUrl, N''), N'/uploads/brand-license/demo-brand-license.pdf'),
        Status = N'Approved',
        ReviewNote = NULL,
        IsVisible = 1,
        UpdatedAtUtc = @Now
    WHERE Id = @BrandProfileId;

    IF EXISTS
    (
        SELECT 1
        FROM dbo.CampaignPackagePolicies
        WHERE IsDeleted = 0 AND IsActive = 1 AND SalePrice <= 0
          AND Code IN
          (
              N'ASSIGNED_UNDER_5M', N'ASSIGNED_5_TO_20M', N'ASSIGNED_UNDER_20M',
              N'SELECT_20M', N'SELECT_50M', N'SELECT_100M'
          )
    )
    BEGIN
        UPDATE p
        SET SalePrice = CASE p.Code
            WHEN N'ASSIGNED_UNDER_5M' THEN 990000
            WHEN N'ASSIGNED_5_TO_20M' THEN 2490000
            WHEN N'ASSIGNED_UNDER_20M' THEN 1990000
            WHEN N'SELECT_20M' THEN 4990000
            WHEN N'SELECT_50M' THEN 8990000
            WHEN N'SELECT_100M' THEN 14990000
            ELSE p.SalePrice END,
        Description = CASE p.Code
            WHEN N'ASSIGNED_UNDER_5M' THEN N'Gói khởi động dành cho chiến dịch nhỏ, Topstar phân công KOC phù hợp.'
            WHEN N'ASSIGNED_5_TO_20M' THEN N'Gói tăng trưởng dành cho Brand cần triển khai nhanh với ngân sách vừa.'
            WHEN N'ASSIGNED_UNDER_20M' THEN N'Gói tiêu chuẩn, Topstar tuyển chọn và phân công KOC theo brief.'
            WHEN N'SELECT_20M' THEN N'Brand chủ động lựa chọn tối đa 3 KOC từ shortlist chất lượng.'
            WHEN N'SELECT_50M' THEN N'Gói mở rộng cho chiến dịch đa nội dung với tối đa 5 KOC.'
            WHEN N'SELECT_100M' THEN N'Gói quy mô lớn, Brand lựa chọn tối đa 10 KOC.'
            ELSE p.Description END,
        Benefits = CASE WHEN NULLIF(p.Benefits, N'') IS NULL THEN
            N'<ul><li>Tư vấn brief chiến dịch</li><li>Matching KOC theo tiêu chí</li><li>Theo dõi tiến độ và báo cáo</li></ul>'
            ELSE p.Benefits END,
        ValidityDays = COALESCE(p.ValidityDays, 90),
        IsFeatured = CASE WHEN p.Code = N'SELECT_20M' THEN 1 ELSE p.IsFeatured END,
            UpdatedAtUtc = @Now
        FROM dbo.CampaignPackagePolicies p
        WHERE p.IsDeleted = 0
          AND p.IsActive = 1
          AND p.SalePrice <= 0
          AND p.Code IN
          (
              N'ASSIGNED_UNDER_5M', N'ASSIGNED_5_TO_20M', N'ASSIGNED_UNDER_20M',
              N'SELECT_20M', N'SELECT_50M', N'SELECT_100M'
          );
    END;

    DECLARE @CampaignSeed TABLE
    (
        No int NOT NULL PRIMARY KEY,
        Id uniqueidentifier NOT NULL,
        Title nvarchar(200) NOT NULL,
        BudgetAmount decimal(18,2) NOT NULL,
        PackageCode nvarchar(30) NOT NULL,
        Status nvarchar(30) NOT NULL,
        StartOffset int NOT NULL,
        EndOffset int NOT NULL,
        RequiresBooking bit NOT NULL,
        RequiresShipment bit NOT NULL,
        IsVisible bit NOT NULL
    );

    INSERT @CampaignSeed
        (No, Id, Title, BudgetAmount, PackageCode, Status, StartOffset, EndOffset, RequiresBooking, RequiresShipment, IsVisible)
    VALUES
        (1,  'B1000001-2026-4000-8000-000000000001', N'Ra mắt Serum Vitamin C 2026',       4500000,   N'ASSIGNED_UNDER_5M',  N'Draft',      10,  40, 0, 1, 0),
        (2,  'B1000002-2026-4000-8000-000000000002', N'Đánh giá kem chống nắng mùa hè',  12000000,  N'ASSIGNED_5_TO_20M', N'Submitted',  15,  50, 0, 1, 0),
        (3,  'B1000003-2026-4000-8000-000000000003', N'Combo chăm sóc tóc phục hồi',      8500000,   N'ASSIGNED_5_TO_20M', N'NeedEdit',   20,  55, 1, 1, 0),
        (4,  'B1000004-2026-4000-8000-000000000004', N'Topstar Beauty Day',               25000000,  N'SELECT_20M',         N'Published',   7,  45, 1, 1, 1),
        (5,  'B1000005-2026-4000-8000-000000000005', N'Livestream Mega Sale tháng 8',     55000000,  N'SELECT_50M',         N'Published',  12,  42, 1, 1, 1),
        (6,  'B1000006-2026-4000-8000-000000000006', N'Routine skincare 7 ngày',          22000000,  N'SELECT_20M',         N'InProgress', -12,  18, 0, 1, 0),
        (7,  'B1000007-2026-4000-8000-000000000007', N'Challenge da khỏe không filter',   68000000,  N'SELECT_50M',         N'InProgress', -20,  10, 1, 1, 0),
        (8,  'B1000008-2026-4000-8000-000000000008', N'Review mặt nạ dưỡng ẩm',            18000000,  N'ASSIGNED_5_TO_20M', N'Completed',  -75, -35, 0, 1, 0),
        (9,  'B1000009-2026-4000-8000-000000000009', N'Tết rạng rỡ cùng Topstar',         105000000,  N'SELECT_100M',        N'Completed', -140, -90, 1, 1, 0),
        (10, 'B1000010-2026-4000-8000-000000000010', N'BST son lì phiên bản giới hạn',     35000000,  N'SELECT_20M',         N'Rejected',    25,  60, 0, 1, 0),
        (11, 'B1000011-2026-4000-8000-000000000011', N'Workshop chăm sóc da nhạy cảm',     48000000,  N'SELECT_20M',         N'Rejected',    18,  48, 1, 0, 0),
        (12, 'B1000012-2026-4000-8000-000000000012', N'Unbox bộ quà Beauty Box',            9500000,  N'ASSIGNED_5_TO_20M', N'Draft',       30,  65, 0, 1, 0);

    INSERT dbo.Campaigns
        (Id, BrandProfileId, Title, Objective, BudgetAmount, PackageCode, RequiresBooking, RequiresShipment,
         Status, StartDate, EndDate, ReviewNote, ViewCount, IsVisible, CreatedAtUtc, UpdatedAtUtc,
         CancelledByUserId, CancelledAtUtc, CancellationReason)
    SELECT s.Id, @BrandProfileId, s.Title,
        CONCAT(N'<p>Chiến dịch QA Brand: ', s.Title, N'.</p><p>Yêu cầu nội dung chân thực, đúng thông điệp và deadline.</p>'),
        s.BudgetAmount, s.PackageCode, s.RequiresBooking, s.RequiresShipment, s.Status,
        DATEADD(DAY, s.StartOffset, CAST(@Now AS date)), DATEADD(DAY, s.EndOffset, CAST(@Now AS date)),
        CASE s.Status WHEN N'NeedEdit' THEN N'Vui lòng bổ sung thông điệp chính và deadline từng đầu việc.'
                      WHEN N'Rejected' THEN N'Ngân sách và nội dung chưa phù hợp chính sách hiện tại.'
                      ELSE NULL END,
        50 + s.No * 17, s.IsVisible, DATEADD(DAY, -s.No * 3, @Now), @Now,
        CASE WHEN s.Status = N'Cancelled' THEN @BrandUserId ELSE NULL END,
        CASE WHEN s.Status = N'Cancelled' THEN DATEADD(DAY, -5, @Now) ELSE NULL END,
        CASE WHEN s.Status = N'Cancelled' THEN N'Brand hủy chiến dịch seed để kiểm thử trạng thái.' ELSE NULL END
    FROM @CampaignSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.Campaigns c WHERE c.Id = s.Id);

    UPDATE c
    SET c.Title = s.Title,
        c.Objective = CONCAT(N'<p>Chiến dịch QA Brand: ', s.Title, N'.</p><p>Yêu cầu nội dung chân thực, đúng thông điệp và deadline.</p>'),
        c.ReviewNote = CASE s.Status
            WHEN N'NeedEdit' THEN N'Vui lòng bổ sung thông điệp chính và deadline từng đầu việc.'
            WHEN N'Rejected' THEN N'Ngân sách và nội dung chưa phù hợp chính sách hiện tại.'
            ELSE NULL END,
        c.UpdatedAtUtc = @Now
    FROM dbo.Campaigns c
    JOIN @CampaignSeed s ON s.Id = c.Id;

    INSERT dbo.CampaignDeliverables
        (Id, CampaignId, DeliverableType, Quantity, DueDateUtc, CreatedAtUtc)
    SELECT CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('TOPSTAR-BRAND-QA-DELIVERABLE-', s.No))),
           s.Id,
           CASE s.No % 3 WHEN 0 THEN N'Video TikTok' WHEN 1 THEN N'Video review' ELSE N'Livestream' END,
           1 + s.No % 3,
           DATEADD(DAY, s.EndOffset - 5, @Now),
           DATEADD(DAY, -s.No * 3, @Now)
    FROM @CampaignSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CampaignDeliverables d WHERE d.CampaignId = s.Id);

    UPDATE d
    SET d.DeliverableType = CASE s.No % 3 WHEN 0 THEN N'Video TikTok' WHEN 1 THEN N'Video review' ELSE N'Livestream' END,
        d.UpdatedAtUtc = @Now
    FROM dbo.CampaignDeliverables d
    JOIN @CampaignSeed s ON s.Id = d.CampaignId
    WHERE d.Id = CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('TOPSTAR-BRAND-QA-DELIVERABLE-', s.No)));

    INSERT dbo.CampaignTargetings
        (CampaignId, Gender, MinimumFollowers, MaximumFollowers, MinimumEngagementRate, MaximumEngagementRate, CreatedAtUtc)
    SELECT s.Id,
           CASE s.No % 3 WHEN 0 THEN N'Any' WHEN 1 THEN N'Female' ELSE N'Male' END,
           5000 + s.No * 1000, NULL, 2.5, NULL, DATEADD(DAY, -s.No * 3, @Now)
    FROM @CampaignSeed s
    WHERE NOT EXISTS (SELECT 1 FROM dbo.CampaignTargetings t WHERE t.CampaignId = s.Id);

    INSERT dbo.CampaignTargetProvinces (CampaignId, ProvinceId, CreatedAtUtc)
    SELECT s.Id, @ProvinceId, DATEADD(DAY, -s.No * 3, @Now)
    FROM @CampaignSeed s
    WHERE @ProvinceId IS NOT NULL
      AND NOT EXISTS (SELECT 1 FROM dbo.CampaignTargetProvinces x WHERE x.CampaignId = s.Id AND x.ProvinceId = @ProvinceId);

    INSERT dbo.CampaignStatusHistories
        (Id, CampaignId, FromStatus, ToStatus, Note, ActorUserId, CreatedAtUtc)
    SELECT CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('TOPSTAR-BRAND-QA-HISTORY-', s.No))),
           s.Id, NULL, s.Status, N'Dữ liệu seed QA Brand portal.', @BrandUserId, DATEADD(DAY, -s.No * 3, @Now)
    FROM @CampaignSeed s
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.CampaignStatusHistories h
        WHERE h.Id = CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('TOPSTAR-BRAND-QA-HISTORY-', s.No)))
    );

    ;WITH KocSource AS
    (
        SELECT TOP (8) p.Id, ROW_NUMBER() OVER (ORDER BY p.Score DESC, p.Id) AS RowNo
        FROM dbo.KocProfiles p
        JOIN dbo.Users u ON u.Id = p.UserId
        WHERE p.Status = N'Approved' AND u.Status = N'Active'
        ORDER BY p.Score DESC, p.Id
    ),
    ApplicationSeed AS
    (
        SELECT s.No CampaignNo, s.Id CampaignId, k.Id KocProfileId, k.RowNo,
               CASE WHEN s.Status = N'Published' THEN N'Applied'
                    WHEN s.Status IN (N'InProgress', N'Completed') THEN N'Confirmed'
                    ELSE N'Applied' END Status
        FROM @CampaignSeed s
        JOIN KocSource k ON k.RowNo <= CASE WHEN s.Status = N'Published' THEN 8
                                           WHEN s.Status IN (N'InProgress', N'Completed') THEN 5
                                           ELSE 2 END
    )
    INSERT dbo.Applications
        (Id, CampaignId, KocProfileId, Status, PitchNote, AppliedAtUtc, CreatedAtUtc)
    SELECT CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('TOPSTAR-BRAND-QA-APPLICATION-', a.CampaignNo, '-', a.RowNo))),
           a.CampaignId, a.KocProfileId, a.Status,
           N'KOC quan tâm và phù hợp với chiến dịch QA Brand.',
           DATEADD(DAY, -a.RowNo, @Now), DATEADD(DAY, -a.RowNo, @Now)
    FROM ApplicationSeed a
    WHERE NOT EXISTS
    (
        SELECT 1 FROM dbo.Applications x
        WHERE x.CampaignId = a.CampaignId AND x.KocProfileId = a.KocProfileId
    );

    UPDATE a
    SET a.PitchNote = N'KOC quan tâm và phù hợp với chiến dịch QA Brand.',
        a.UpdatedAtUtc = @Now
    FROM dbo.Applications a
    JOIN @CampaignSeed s ON s.Id = a.CampaignId
    WHERE a.Id IN
    (
        SELECT CONVERT(uniqueidentifier, HASHBYTES('MD5', CONCAT('TOPSTAR-BRAND-QA-APPLICATION-', s2.No, '-', n.RowNo)))
        FROM @CampaignSeed s2
        CROSS JOIN (VALUES (1),(2),(3),(4),(5),(6),(7),(8)) n(RowNo)
        WHERE s2.Id = s.Id
    );

    COMMIT TRANSACTION;

    SELECT N'Brand account' Dataset, COUNT(*) [RowCount]
    FROM dbo.Users WHERE Id = @BrandUserId AND Status = N'Active'
    UNION ALL
    SELECT N'Brand campaigns', COUNT(*) FROM dbo.Campaigns WHERE BrandProfileId = @BrandProfileId
    UNION ALL
    SELECT N'QA campaigns', COUNT(*) FROM dbo.Campaigns WHERE Id IN (SELECT Id FROM @CampaignSeed)
    UNION ALL
    SELECT N'QA applications', COUNT(*) FROM dbo.Applications WHERE CampaignId IN (SELECT Id FROM @CampaignSeed);
END TRY
BEGIN CATCH
    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

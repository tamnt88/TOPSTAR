SET NOCOUNT ON;
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @UserId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.Users WHERE Email=N'brand@topstar.local');
    DECLARE @BrandId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.BrandProfiles WHERE UserId=@UserId);
    IF @BrandId IS NULL THROW 51210, N'Không tìm thấy Brand Demo. Hãy chạy 104 trước.', 1;

    DECLARE @Now DATETIME2=SYSUTCDATETIME();

    INSERT dbo.BrandPackagePurchases
        (Id,BrandProfileId,PackagePolicyId,CampaignId,OrderNumber,PackageCodeSnapshot,PackageNameSnapshot,
         AmountSnapshot,ValidityDaysSnapshot,Status,PaymentReference,PurchasedAtUtc,PaidAtUtc,ExpiresAtUtc,
         ConsumedAtUtc,CreatedAtUtc,UpdatedAtUtc)
    SELECT NEWID(),@BrandId,p.Id,c.Id,
           CONCAT('SEED-',LEFT(REPLACE(CONVERT(VARCHAR(36),c.Id),'-',''),20)),
           p.Code,p.Name,p.SalePrice,p.ValidityDays,'Consumed',
           CONCAT('SEED-PAID-',LEFT(REPLACE(CONVERT(VARCHAR(36),c.Id),'-',''),8)),
           DATEADD(DAY,-15,c.CreatedAtUtc),DATEADD(DAY,-14,c.CreatedAtUtc),
           CASE WHEN p.ValidityDays IS NULL THEN NULL ELSE DATEADD(DAY,p.ValidityDays,DATEADD(DAY,-14,c.CreatedAtUtc)) END,
           c.CreatedAtUtc,DATEADD(DAY,-15,c.CreatedAtUtc),c.CreatedAtUtc
    FROM dbo.Campaigns c
    JOIN dbo.CampaignPackagePolicies p ON p.Code=c.PackageCode
    WHERE c.BrandProfileId=@BrandId
      AND NOT EXISTS(SELECT 1 FROM dbo.BrandPackagePurchases x WHERE x.CampaignId=c.Id);

    INSERT dbo.BrandPackagePurchaseHistories(Id,PurchaseId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
    SELECT NEWID(),x.Id,'Paid','Consumed',N'Dữ liệu test: gói đã được dùng để tạo chiến dịch seed.',@UserId,x.ConsumedAtUtc
    FROM dbo.BrandPackagePurchases x
    WHERE x.BrandProfileId=@BrandId AND x.Status='Consumed'
      AND NOT EXISTS(SELECT 1 FROM dbo.BrandPackagePurchaseHistories h WHERE h.PurchaseId=x.Id);

    DECLARE @PolicyId UNIQUEIDENTIFIER=(SELECT TOP(1) Id FROM dbo.CampaignPackagePolicies WHERE Code='SELECT_20M' AND IsActive=1 AND IsDeleted=0);
    IF @PolicyId IS NOT NULL AND NOT EXISTS(
        SELECT 1 FROM dbo.BrandPackagePurchases WHERE BrandProfileId=@BrandId AND OrderNumber='SEED-BRAND-AVAILABLE-001')
    BEGIN
        DECLARE @PurchaseId UNIQUEIDENTIFIER=NEWID();
        INSERT dbo.BrandPackagePurchases
            (Id,BrandProfileId,PackagePolicyId,OrderNumber,PackageCodeSnapshot,PackageNameSnapshot,AmountSnapshot,
             ValidityDaysSnapshot,Status,PaymentReference,PurchasedAtUtc,PaidAtUtc,ExpiresAtUtc,CreatedAtUtc)
        SELECT @PurchaseId,@BrandId,Id,'SEED-BRAND-AVAILABLE-001',Code,Name,SalePrice,ValidityDays,'Paid',
               'DEMO-TRANSFER-001',@Now,@Now,CASE WHEN ValidityDays IS NULL THEN NULL ELSE DATEADD(DAY,ValidityDays,@Now) END,@Now
        FROM dbo.CampaignPackagePolicies WHERE Id=@PolicyId;
        INSERT dbo.BrandPackagePurchaseHistories(Id,PurchaseId,FromStatus,ToStatus,Note,ActorUserId,CreatedAtUtc)
        VALUES(NEWID(),@PurchaseId,'Pending','Paid',N'Dữ liệu test: Topstar đã xác nhận thanh toán chuyển khoản.',@UserId,@Now);
    END;
    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF @@TRANCOUNT>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

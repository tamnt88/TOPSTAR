SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF OBJECT_ID(N'dbo.BrandPackagePurchases', N'U') IS NOT NULL
BEGIN
    ALTER TABLE dbo.BrandPackagePurchases
        ALTER COLUMN PaymentReference nvarchar(100) NULL;

    -- Repair the known confirmation text that was previously written through varchar.
    UPDATE dbo.BrandPackagePurchases
    SET PaymentReference = N'Đã nhận đầy đủ tiền mua gói KOC'
    WHERE PaymentReference = N'Đã nh?n đ?y đ? ti?n mua gói KOC';

    UPDATE dbo.BrandPackagePurchases
    SET PaymentReference = N'Nội dung được lưu vào lịch sử kiểm toán.'
    WHERE PaymentReference = N'N?i dung du?c luu vào l?ch s? ki?m toán.';

    UPDATE history
    SET Note = N'Admin xác nhận thanh toán: Đã nhận đầy đủ tiền mua gói KOC'
    FROM dbo.BrandPackagePurchaseHistories AS history
    INNER JOIN dbo.BrandPackagePurchases AS purchase ON purchase.Id = history.PurchaseId
    WHERE history.Note = N'Admin xác nhận thanh toán: Đã nh?n đ?y đ? ti?n mua gói KOC';
END;

COMMIT TRANSACTION;

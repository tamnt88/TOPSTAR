SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @NowUtc datetime2(7) = SYSUTCDATETIME();
DECLARE @KeepId uniqueidentifier;

SELECT TOP (1) @KeepId = Id
FROM dbo.CampaignFeePolicies
WHERE Code = N'DEFAULT'
  AND IsActive = 1
  AND EffectiveAtUtc <= @NowUtc
ORDER BY CreatedAtUtc DESC, EffectiveAtUtc DESC, Id DESC;

IF @KeepId IS NOT NULL
BEGIN
    UPDATE dbo.CampaignFeePolicies
       SET IsActive = 0,
           UpdatedAtUtc = @NowUtc
     WHERE Code = N'DEFAULT'
       AND IsActive = 1
       AND EffectiveAtUtc <= @NowUtc
       AND Id <> @KeepId;
END;

COMMIT TRANSACTION;

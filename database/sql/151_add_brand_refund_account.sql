SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF COL_LENGTH('dbo.BrandProfiles', 'RefundBankName') IS NULL
    ALTER TABLE dbo.BrandProfiles ADD RefundBankName nvarchar(200) NULL;

IF COL_LENGTH('dbo.BrandProfiles', 'RefundAccountNumber') IS NULL
    ALTER TABLE dbo.BrandProfiles ADD RefundAccountNumber nvarchar(100) NULL;

IF COL_LENGTH('dbo.BrandProfiles', 'RefundAccountHolder') IS NULL
    ALTER TABLE dbo.BrandProfiles ADD RefundAccountHolder nvarchar(200) NULL;

/* Preserve previously supplied refund accounts by copying the latest complete
   campaign-funding snapshot into the owning Brand profile. */
EXEC sys.sp_executesql N'
UPDATE brand
SET RefundBankName = snapshot.RefundBankName,
    RefundAccountNumber = snapshot.RefundAccountNumber,
    RefundAccountHolder = snapshot.RefundAccountHolder
FROM dbo.BrandProfiles AS brand
OUTER APPLY
(
    SELECT TOP (1)
        funding.RefundBankName,
        funding.RefundAccountNumber,
        funding.RefundAccountHolder
    FROM dbo.Campaigns AS campaign
    INNER JOIN dbo.CampaignFundings AS funding ON funding.CampaignId = campaign.Id
    WHERE campaign.BrandProfileId = brand.Id
      AND NULLIF(LTRIM(RTRIM(funding.RefundBankName)), N'''') IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(funding.RefundAccountNumber)), N'''') IS NOT NULL
      AND NULLIF(LTRIM(RTRIM(funding.RefundAccountHolder)), N'''') IS NOT NULL
    ORDER BY COALESCE(funding.UpdatedAtUtc, funding.CreatedAtUtc) DESC, funding.VersionNo DESC
) AS snapshot
WHERE snapshot.RefundBankName IS NOT NULL
  AND NULLIF(LTRIM(RTRIM(brand.RefundBankName)), N'''') IS NULL;
';

COMMIT TRANSACTION;

SELECT
    COL_LENGTH('dbo.BrandProfiles', 'RefundBankName') AS RefundBankNameBytes,
    COL_LENGTH('dbo.BrandProfiles', 'RefundAccountNumber') AS RefundAccountNumberBytes,
    COL_LENGTH('dbo.BrandProfiles', 'RefundAccountHolder') AS RefundAccountHolderBytes;

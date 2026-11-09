SET NOCOUNT ON;

DECLARE @BrandUserId uniqueidentifier = '465397EA-DDBD-47DC-AF44-BF9575B4D08E';
DECLARE @BrandProfileId uniqueidentifier = 'A3C37ED9-F881-4FC8-AB2D-C18FFAA5E7F2';

SELECT u.Email, u.Status UserStatus, p.CompanyName, p.Status ProfileStatus,
       CASE WHEN NULLIF(p.StreetAddress, N'') IS NOT NULL
                  AND p.ProvinceId IS NOT NULL
                  AND p.WardId IS NOT NULL
                  AND NULLIF(p.TaxCode, N'') IS NOT NULL
                  AND NULLIF(p.TaxCompanyName, N'') IS NOT NULL
                  AND NULLIF(p.TaxInvoiceAddress, N'') IS NOT NULL
                  AND NULLIF(p.BusinessLicenseFileUrl, N'') IS NOT NULL
            THEN N'Complete' ELSE N'Incomplete' END Completion
FROM dbo.Users u
JOIN dbo.BrandProfiles p ON p.UserId = u.Id
WHERE u.Id = @BrandUserId AND p.Id = @BrandProfileId;

SELECT c.Status, COUNT(*) CampaignCount, SUM(c.BudgetAmount) TotalBudget,
       SUM(COALESCE(a.ApplicationCount, 0)) ApplicationCount
FROM dbo.Campaigns c
LEFT JOIN
(
    SELECT CampaignId, COUNT(*) ApplicationCount
    FROM dbo.Applications
    GROUP BY CampaignId
) a ON a.CampaignId = c.Id
WHERE c.BrandProfileId = @BrandProfileId
GROUP BY c.Status
ORDER BY c.Status;

SELECT c.Id, c.Title, c.PackageCode, c.Status, c.BudgetAmount,
       c.StartDate, c.EndDate,
       (SELECT COUNT(*) FROM dbo.Applications a WHERE a.CampaignId = c.Id) ApplicationCount,
       (SELECT COUNT(*) FROM dbo.CampaignDeliverables d WHERE d.CampaignId = c.Id) DeliverableCount
FROM dbo.Campaigns c
WHERE c.BrandProfileId = @BrandProfileId
ORDER BY c.CreatedAtUtc DESC;

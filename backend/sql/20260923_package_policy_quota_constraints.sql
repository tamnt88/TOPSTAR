SET XACT_ABORT ON;
BEGIN TRANSACTION;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CampaignPackagePolicyHistories_Quota')
    ALTER TABLE dbo.CampaignPackagePolicyHistories DROP CONSTRAINT CK_CampaignPackagePolicyHistories_Quota;

UPDATE dbo.CampaignPackagePolicies
SET SelectionQuota = CASE Code
    WHEN N'ASSIGNED_UNDER_5M' THEN 1
    WHEN N'ASSIGNED_UNDER_20M' THEN 3
    WHEN N'ASSIGNED_5_TO_20M' THEN 5
    WHEN N'SELECT_20M' THEN 3
    WHEN N'SELECT_50M' THEN 5
    WHEN N'SELECT_100M' THEN 10
    ELSE SelectionQuota
END
WHERE SelectionQuota IS NULL OR SelectionQuota NOT BETWEEN 1 AND 100;

UPDATE history
SET history.SelectionQuota = policy.SelectionQuota
FROM dbo.CampaignPackagePolicyHistories AS history
INNER JOIN dbo.CampaignPackagePolicies AS policy ON policy.Id = history.PolicyId
WHERE (history.SelectionQuota IS NULL OR history.SelectionQuota NOT BETWEEN 1 AND 100)
  AND policy.SelectionQuota BETWEEN 1 AND 100;

IF EXISTS
(
    SELECT 1
    FROM dbo.CampaignPackagePolicies
    WHERE SelectionQuota IS NULL OR SelectionQuota NOT BETWEEN 1 AND 100
)
    THROW 51000, 'Cannot apply package quota constraint: CampaignPackagePolicies contains an invalid SelectionQuota.', 1;

IF EXISTS
(
    SELECT 1
    FROM dbo.CampaignPackagePolicyHistories
    WHERE SelectionQuota IS NULL OR SelectionQuota NOT BETWEEN 1 AND 100
)
    THROW 51001, 'Cannot apply package quota constraint: CampaignPackagePolicyHistories contains an invalid SelectionQuota.', 1;

IF EXISTS (SELECT 1 FROM sys.check_constraints WHERE name = N'CK_CampaignPackagePolicies_Quota')
    ALTER TABLE dbo.CampaignPackagePolicies DROP CONSTRAINT CK_CampaignPackagePolicies_Quota;

ALTER TABLE dbo.CampaignPackagePolicies WITH CHECK
ADD CONSTRAINT CK_CampaignPackagePolicies_Quota
CHECK (SelectionQuota BETWEEN 1 AND 100);

ALTER TABLE dbo.CampaignPackagePolicyHistories WITH CHECK
ADD CONSTRAINT CK_CampaignPackagePolicyHistories_Quota
CHECK (SelectionQuota BETWEEN 1 AND 100);

COMMIT TRANSACTION;

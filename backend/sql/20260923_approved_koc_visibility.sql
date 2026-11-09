-- Approved KOC profiles must be visible to the matching pool.
-- Safe to run repeatedly during deployment.
UPDATE dbo.KocProfiles
SET IsVisible = 1,
    UpdatedAtUtc = COALESCE(UpdatedAtUtc, SYSUTCDATETIME())
WHERE Status = N'Approved'
  AND IsVisible = 0;

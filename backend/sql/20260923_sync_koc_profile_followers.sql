-- Synchronize the KOC profile aggregate with the largest visible social channel.
-- Safe to run repeatedly during deployment.
UPDATE profile
SET profile.Followers = social.MaxFollowers,
    profile.UpdatedAtUtc = COALESCE(profile.UpdatedAtUtc, SYSUTCDATETIME())
FROM dbo.KocProfiles AS profile
INNER JOIN
(
    SELECT KocProfileId, MAX(Followers) AS MaxFollowers
    FROM dbo.KocSocialAccounts
    WHERE IsVisible = 1
    GROUP BY KocProfileId
) AS social ON social.KocProfileId = profile.Id
WHERE profile.Followers <> social.MaxFollowers;

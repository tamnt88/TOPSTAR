# Topstar SQL Scripts

Run order:
1. `00_create_topstar_v1.sql`
2. `01_seed_topstar_v1.sql`

Target:
- SQL Server 2022
- Database name suggested: `TopstarDb`

Notes:
- `00_create_topstar_v1.sql` includes `CREATE DATABASE TopstarDb` (if missing).
- Scripts create tables, constraints, indexes, and sample data.
- This is v1 baseline for Website-first development (Admin + Brand + KOC core flow).
- Wallet/Billing tables are intentionally deferred to next phase.
- All tables include common columns: `CreatedAtUtc`, `UpdatedAtUtc`.
- `ViewCount` and `IsVisible` are applied to content-facing tables only.
- KOC profile is extended for onboarding/layout coverage:
  - `KocBodyProfiles`, `KocChildren`
  - `KocSocialAccounts`
  - `KocCategories`, `KocProfileCategories`
  - `KocExperiences`
  - `KocProductionCapabilities`
  - `KocJobPreferences`
  - `KocPortfolios`
- Social performance tracking tables:
  - `SubmissionPosts`: links submitted by KOC (1 submission can have many links).
  - `PostMetricSnapshots`: time-series metrics per link (views/likes/comments/shares/...).
  - `CampaignKocDailyMetrics`: daily aggregate KPI per campaign x KOC for dashboard/report.

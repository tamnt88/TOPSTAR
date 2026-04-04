# Topstar SQL Scripts

Run order:
1. `00_create_topstar_v1.sql`
2. `01_seed_topstar_v1.sql`
3. `02_create_topstar_v2_auth_rbac.sql`
4. `03_seed_topstar_v2_auth_rbac.sql`

Target:
- SQL Server 2022
- Database name suggested: `TopstarDb`

Notes:
- `00_create_topstar_v1.sql` includes `CREATE DATABASE TopstarDb` (if missing).
- v1 creates campaign/profile core tables for Admin + Brand + KOC.
- v2 extends account/auth/permission/menu/dashboard capabilities.

v2 highlights:
- Keep single `Users` table for all roles (`ADMIN`, `BRAND`, `KOC`).
- Add RBAC + override permissions:
  - `Permissions`, `RolePermissions`, `UserPermissions`.
- Add role/user-based menu and dashboard configs:
  - `Menus`, `RoleMenus`, `UserMenus`
  - `DashboardWidgets`, `RoleDashboardConfigs`, `UserDashboardConfigs`
- Add admin profile table:
  - `AdminProfiles`
- Add full auth flow support for KOC/Brand/Admin:
  - `AuthSessions` (session + refresh token)
  - `PasswordResetTokens` (forgot/reset password)
  - `AuthEvents` (register/login/forgot/reset audit)
  - `UserDevices` (device fingerprint + user agent + IP + geo)

General conventions:
- All tables use `CreatedAtUtc`, `UpdatedAtUtc`.
- `ViewCount` and `IsVisible` are applied to content-facing tables only.
- Script v2 is additive and safe to run after v1.

# Topstar SQL Scripts

Run order:
1. `00_create_topstar_v1.sql`
2. `01_seed_topstar_v1.sql`
3. `02_create_topstar_v2_auth_rbac.sql`
4. `03_seed_topstar_v2_auth_rbac.sql`
5. `04_update_seed_users_password.sql` (optional for quick local login)
6. `05_create_topstar_v3_address_ghn.sql`
7. `06_seed_topstar_v3_address_sample.sql`
8. `07_patch_ward_code_string.sql`
9. `08_patch_provinces_wards_v3_full.sql`

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

v3 highlights (address + shipping readiness):
- Add administrative master tables:
  - `Provinces`, `Wards`
- Add GHN-ready address fields for profile + shipment:
  - `StreetAddress`, `WardId`, `ProvinceId`
  - `AddressV2`, `WardCodeV2`, `IsNewAddress`
- Shipment stores receiver address snapshot for tracking/order fulfillment.
- `06_seed_topstar_v3_address_sample.sql` is sample-only (dev/test), replace with full GHN sync dataset in production.
- `08_patch_provinces_wards_v3_full.sql` aligns schema with GHN v3 payload (province/all + ward/all-by-province-id).
- Use sync script `scripts_sync_ghn_v3_address.ps1` to fully replace old provinces/wards by GHN v3 data.

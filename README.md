# Topstar

Monorepo for:
- Backend: `.NET 9` + `SQL Server 2022`
- Website: `Topstar.Web` (uses shared business logic directly)
- API: `Topstar.Api` (for Flutter mobile and external clients)
- Mobile: Flutter bootstrap script and templates

## Repository Layout

```txt
backend/
  Topstar.sln
  src/
    Topstar.Core/
    Topstar.Application/
    Topstar.Infrastructure/
    Topstar.Web/
    Topstar.Api/
  tests/
    Topstar.Application.Tests/
mobile/
  bootstrap_flutter.ps1
  templates/
document/
```

## Architecture Summary

- `Topstar.Web` and `Topstar.Api` both use shared logic in `Topstar.Application`.
- No internal HTTP call from Website to API.
- Flutter app calls `Topstar.Api`.

## Run Backend

1. Build solution:
```powershell
dotnet build .\backend\Topstar.sln
```

2. Run API (default `http://localhost:5051`):
```powershell
dotnet run --project .\backend\src\Topstar.Api
```

3. Run Web:
```powershell
dotnet run --project .\backend\src\Topstar.Web
```

4. API test endpoint:
```txt
GET http://localhost:5051/api/v1/system/platform-info
```

## SQL Server 2022

Default connection string in both hosts:

```txt
Server=localhost;Database=TopstarDb;Trusted_Connection=True;TrustServerCertificate=True
```

Update in:
- `backend/src/Topstar.Api/appsettings.json`
- `backend/src/Topstar.Web/appsettings.json`

## Bootstrap Flutter App

Prerequisite: Flutter SDK installed and available in `PATH`.

Run:
```powershell
.\mobile\bootstrap_flutter.ps1 -ProjectName topstar_koc_app -ApiBaseUrl http://10.0.2.2:5051
```

Then:
```powershell
cd .\mobile\topstar_koc_app
flutter run
```

`10.0.2.2` is for Android emulator to access host machine localhost.

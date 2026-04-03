# Mobile Setup (Flutter)

This repository includes a bootstrap script to create the Flutter app and wire it to `Topstar.Api`.

## Prerequisite

- Install Flutter SDK and make sure `flutter` command is available in `PATH`.

## Create app

```powershell
.\bootstrap_flutter.ps1 -ProjectName topstar_koc_app -ApiBaseUrl http://10.0.2.2:5051
```

## Run app

```powershell
cd .\topstar_koc_app
flutter run
```

## What bootstrap does

- Runs `flutter create`.
- Replaces `lib/main.dart` with API connectivity sample.
- Creates `lib/env.dart` with configured API base URL.
- Adds `http` package dependency.

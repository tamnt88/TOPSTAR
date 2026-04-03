param(
    [string]$ProjectName = "topstar_koc_app",
    [string]$ApiBaseUrl = "http://10.0.2.2:5051"
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command flutter -ErrorAction SilentlyContinue)) {
    Write-Error "Flutter SDK was not found in PATH. Install Flutter first, then run this script again."
}

$mobileRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$projectPath = Join-Path $mobileRoot $ProjectName
$templateMain = Join-Path $mobileRoot "templates\\main.dart.template"
$templateEnv = Join-Path $mobileRoot "templates\\env.dart.template"

if (-not (Test-Path $projectPath)) {
    flutter create $projectPath
}

$libDir = Join-Path $projectPath "lib"
if (-not (Test-Path $libDir)) {
    New-Item -ItemType Directory -Path $libDir | Out-Null
}

$mainContent = Get-Content $templateMain -Raw
$envContent = (Get-Content $templateEnv -Raw).Replace("__API_BASE_URL__", $ApiBaseUrl)

Set-Content -Path (Join-Path $libDir "main.dart") -Value $mainContent -NoNewline
Set-Content -Path (Join-Path $libDir "env.dart") -Value $envContent -NoNewline

Push-Location $projectPath
try {
    flutter pub add http
}
finally {
    Pop-Location
}

Write-Host "Flutter project is ready at: $projectPath"
Write-Host "API base URL configured: $ApiBaseUrl"
Write-Host "Next: cd $projectPath; flutter pub get; flutter run"

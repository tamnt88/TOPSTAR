$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "backend\\src\\Topstar.Api\\Topstar.Api.csproj"
$webProject = Join-Path $repoRoot "backend\\src\\Topstar.Web\\Topstar.Web.csproj"

Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$apiProject`""
Start-Process powershell -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$webProject`""

Write-Host "Started Topstar.Api and Topstar.Web in separate terminals."

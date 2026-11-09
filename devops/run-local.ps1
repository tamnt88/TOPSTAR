$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
$apiProject = Join-Path $repoRoot "backend\\src\\Topstar.Api\\Topstar.Api.csproj"
$webProject = Join-Path $repoRoot "backend\\src\\Topstar.Web\\Topstar.Web.csproj"
$apiProjectRoot = Split-Path -Parent $apiProject
$webProjectRoot = Split-Path -Parent $webProject

Start-Process powershell -WorkingDirectory $apiProjectRoot -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$apiProject`""
Start-Process powershell -WorkingDirectory $webProjectRoot -ArgumentList "-NoExit", "-Command", "dotnet run --project `"$webProject`""

Write-Host "Started Topstar.Api and Topstar.Web in separate terminals."

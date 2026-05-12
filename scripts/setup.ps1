# PowerShell setup script for Windows.
# Usage: pwsh -File scripts/setup.ps1

$ErrorActionPreference = "Stop"

Write-Host "==> Starting Postgres via Docker..." -ForegroundColor Cyan
docker compose up -d

Write-Host "==> Restoring .NET tools..." -ForegroundColor Cyan
dotnet tool restore

Write-Host "==> Restoring NuGet packages..." -ForegroundColor Cyan
dotnet restore

Write-Host "==> Building solution..." -ForegroundColor Cyan
dotnet build --no-restore

Write-Host "==> Adding initial EF Core migration (skipping if already present)..." -ForegroundColor Cyan
$migrationsDir = "src/ProductService.Infrastructure/Migrations"
if (-not (Test-Path $migrationsDir)) {
    dotnet ef migrations add InitialCreate `
        --project src/ProductService.Infrastructure `
        --startup-project src/ProductService.Api
} else {
    Write-Host "    Migrations folder already exists; skipping." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Setup complete." -ForegroundColor Green
Write-Host "Run the API: dotnet run --project src/ProductService.Api"
Write-Host "Swagger:     https://localhost:7080/swagger"

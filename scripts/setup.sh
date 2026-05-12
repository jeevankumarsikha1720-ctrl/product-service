#!/usr/bin/env bash
# Bash setup script for Mac/Linux.
# Usage: bash scripts/setup.sh

set -euo pipefail

echo "==> Starting Postgres via Docker..."
docker compose up -d

echo "==> Restoring .NET tools..."
dotnet tool restore

echo "==> Restoring NuGet packages..."
dotnet restore

echo "==> Building solution..."
dotnet build --no-restore

echo "==> Adding initial EF Core migration (skipping if already present)..."
if [ ! -d "src/ProductService.Infrastructure/Migrations" ]; then
    dotnet ef migrations add InitialCreate \
        --project src/ProductService.Infrastructure \
        --startup-project src/ProductService.Api
else
    echo "    Migrations folder already exists; skipping."
fi

echo
echo "Setup complete."
echo "Run the API: dotnet run --project src/ProductService.Api"
echo "Swagger:     https://localhost:7080/swagger"

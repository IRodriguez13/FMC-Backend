#!/usr/bin/env bash
# Pobla la BD con 20 cafeterías demo (CABA) + fotos desde Api/SeedAssets.
# Uso: make seed   |   ./scripts/seed-db.sh
set -euo pipefail

ROOT="$(cd "$(dirname "$0")/.." && pwd)"
cd "$ROOT"

ENV="${ENV:-Development}"
export ASPNETCORE_ENVIRONMENT="$ENV"

echo "FMC seed — migraciones + catálogo CABA (22 cafeterías)…"
dotnet run --project Api/Api.csproj --no-launch-profile -- --seed-only "$@"

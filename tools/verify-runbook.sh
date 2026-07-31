#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

runbook=docs/RUNBOOK.md
test -s "$runbook"
for required in \
  'dotnet restore SCEAMS.sln' \
  'dotnet build SCEAMS.sln' \
  'dotnet ef database update' \
  'SCEAMS.NotificationService/SCEAMS.NotificationService.csproj' \
  'SCEAMS.API/SCEAMS.API.csproj' \
  'SCEAMS.MVC/SCEAMS.MVC.csproj' \
  'https://localhost:7069/swagger' \
  'SeedData__AdminPassword' \
  'Jwt__SigningKey'; do
  rg -Fq "$required" "$runbook"
done
if rg -n 'Server=.*Password=[^<\n"]+' "$runbook"; then
  echo 'Runbook chứa connection string password thật.' >&2
  exit 1
fi
echo 'Runbook đầy đủ prerequisites, thứ tự chạy, URL và secret placeholders.'

#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

projects="$(dotnet sln SCEAMS.sln list)"
for project in SCEAMS.API/SCEAMS.API.csproj SCEAMS.MVC/SCEAMS.MVC.csproj SCEAMS.NotificationService/SCEAMS.NotificationService.csproj; do
  grep -Fq "$project" <<<"$projects"
done
if [[ "$(grep -c '\.csproj' <<<"$projects")" -ne 3 ]]; then
  echo 'Solution phải có đúng 3 project.' >&2
  exit 1
fi

for folder in Api Application Domain Infrastructure; do
  test -d "SCEAMS.API/$folder"
done
test -d SCEAMS.API/Infrastructure/Data/Migrations
test -s SCEAMS.API/appsettings.Example.json
test -s docs/architecture/SCEAMS_ERD.svg
test -s docs/TECHNICAL_DOCUMENTATION.md
test -s docs/RUNBOOK.md
test -s postman/SCEAMS.postman_collection.json
test -s postman/SCEAMS.security-audit.postman_collection.json
test -s postman/SCEAMS.e2e-role-smoke.postman_collection.json

if git ls-files | rg '(^|/)(bin|obj|\.vs)/|\.(db|db-shm|db-wal)$'; then
  echo 'Phát hiện build output hoặc database file trong Git.' >&2
  exit 1
fi
if rg -n 'Password=[^<\n"]+|ApiKey"[[:space:]]*:[[:space:]]*"[^<"]+' SCEAMS.API/appsettings.Example.json; then
  echo 'appsettings.Example.json chứa secret thật.' >&2
  exit 1
fi
echo 'Package audit passed: 3 projects, 4 API folders, migrations, docs, Postman và config example.'

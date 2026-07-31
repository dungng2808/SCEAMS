#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

jq empty postman/SCEAMS.e2e-role-smoke.postman_collection.json
jq empty postman/SCEAMS.e2e-role-smoke.environment.example.json

for role in 'Admin flow' 'Staff flow' 'Organizer flow' 'Student flow'; do
  jq -e --arg role "$role" '.. | objects | select(.name? == $role)' postman/SCEAMS.e2e-role-smoke.postman_collection.json >/dev/null
done

if [[ "${1:-}" == "--run" ]]; then
  environment_file="${SCEAMS_E2E_ENV:-/tmp/sceams-e2e-local.json}"
  [[ -f "$environment_file" ]] || { echo "Thiếu environment: $environment_file" >&2; exit 2; }
  command -v newman >/dev/null 2>&1 || { echo 'Cài Newman trước khi chạy E2E.' >&2; exit 2; }
  newman run postman/SCEAMS.e2e-role-smoke.postman_collection.json -e "$environment_file" --insecure --bail
else
  echo 'E2E collection hợp lệ. Dùng --run để gọi API thật.'
fi

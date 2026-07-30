#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

source_only=false
if [[ "${1:-}" == "--source-only" ]]; then
  source_only=true
fi

failures=0
check() {
  local description="$1"
  shift
  if "$@"; then
    printf 'PASS  %s\n' "$description"
  else
    printf 'FAIL  %s\n' "$description" >&2
    failures=$((failures + 1))
  fi
}

check 'API bật HTTPS redirection' rg -q 'app\.UseHttpsRedirection\(\)' SCEAMS.API/Program.cs
check 'API bật authorization middleware' rg -q 'app\.UseAuthorization\(\)' SCEAMS.API/Program.cs
check 'API trả ProblemDetails cho lỗi' rg -q 'AddProblemDetails|ProblemDetailsWriter' SCEAMS.API/Program.cs
check 'API giới hạn OData $top' rg -q 'SetMaxTop\(50\)' SCEAMS.API/Program.cs
check 'JWT validate issuer/audience/lifetime' rg -q 'ValidateIssuer = true' SCEAMS.API/Program.cs
check 'MVC không dùng localStorage cho token' bash -c '! rg -qi "localStorage.*(token|jwt)|((token|jwt).*localStorage)" SCEAMS.MVC --glob "*.cs" --glob "*.cshtml"'
check 'DTO không trả PasswordHash trực tiếp' bash -c '! rg -qi "PasswordHash" SCEAMS.API/Application/DTOs SCEAMS.API/Api/Controllers --glob "*.cs"'
check 'environment template không chứa secret thật' bash -c \
  '! rg -n '"'"'(Password|SigningKey|ApiKey)'"'"'[[:space:]]*:[[:space:]]*'"'"'[^<"]{12,}'"'"' SCEAMS.API/appsettings.Local.example.json'
check 'security collection là JSON hợp lệ' jq empty postman/SCEAMS.security-audit.postman_collection.json
check 'security environment example là JSON hợp lệ' jq empty postman/SCEAMS.security-audit.environment.example.json

if [[ "$source_only" == false ]]; then
  if ! command -v newman >/dev/null 2>&1; then
    echo 'ERROR: Newman chưa được cài. Cài bằng: npm install --global newman' >&2
    exit 2
  fi
  environment_file="${SCEAMS_SECURITY_ENV:-/tmp/sceams-security.json}"
  if [[ ! -f "$environment_file" ]]; then
    echo "ERROR: thiếu environment local: $environment_file" >&2
    exit 2
  fi
  newman run postman/SCEAMS.security-audit.postman_collection.json \
    -e "$environment_file" --bail
fi

if [[ "$failures" -ne 0 ]]; then
  exit 1
fi

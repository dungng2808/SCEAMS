#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

jq empty postman/SCEAMS.odata-content-negotiation.postman_collection.json
test -s docs/api/examples/events-odata-approved.json
test -s docs/api/examples/events-odata-select-expand.json
test -s docs/api/examples/events-odata-top1.xml

if [[ "${1:-}" == "--run" ]]; then
  base_url="${SCEAMS_BASE_URL:-https://localhost:7195}"
  json_response="$(curl -ksSf -H 'Accept: application/json' "$base_url/api/events?\$top=1")"
  grep -q '"id"' <<<"$json_response"
  xml_response="$(curl -ksSf -H 'Accept: application/xml' "$base_url/api/events?\$top=1")"
  grep -q '<ArrayOfEventListResponseDto' <<<"$xml_response"
  echo 'OData JSON/XML smoke test passed.'
else
  echo 'OData/content-negotiation assets hợp lệ. Dùng --run để gọi API thật.'
fi

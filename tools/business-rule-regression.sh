#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT_DIR"

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

check 'registration chặn Event chưa Approved' rg -q 'eventEntity\.Status != EventStatus\.Approved' SCEAMS.API/Application/Services/RegistrationService.cs
check 'registration kiểm tra deadline' rg -q 'RegistrationDeadline <= DateTime\.UtcNow' SCEAMS.API/Application/Services/RegistrationService.cs
check 'registration kiểm tra capacity' rg -q 'registeredCount >= eventEntity\.Capacity' SCEAMS.API/Application/Services/RegistrationService.cs
check 'registration chặn trùng' rg -q 'existing != null' SCEAMS.API/Application/Services/RegistrationService.cs
check 'hủy registration có mốc 24 giờ' rg -q 'StartTime\.AddHours\(-24\)' SCEAMS.API/Application/Services/RegistrationService.cs
check 'feedback chỉ cho Attended' rg -q 'RegistrationStatus\.Attended' SCEAMS.API/Application/Services/FeedbackService.cs
check 'feedback chặn gửi trùng' rg -q 'Student đã gửi feedback' SCEAMS.API/Application/Services/FeedbackService.cs
check 'approve Event kiểm tra overlap' rg -q 'trùng Venue' SCEAMS.API/Application/Services/EventService.cs
check 'Event Completed/Cancelled không được sửa' rg -q 'EventStatus\.Completed or EventStatus\.Cancelled' SCEAMS.API/Application/Services/EventService.cs
check 'chatbot giới hạn theo Student' rg -q 'MaxQuestionsPerHour|10' SCEAMS.API/Application/Services/ChatRateLimiter.cs
check 'Postman có request cho registration/feedback/chatbot' bash -c 'jq -e ".. | objects | select(has(\"name\")) | .name | select(test(\"Phase 91|Phase 101|Phase 123|Phase 127\"))" postman/SCEAMS.postman_collection.json >/dev/null'
check 'regression document tồn tại' test -f docs/testing/BUSINESS_RULE_REGRESSION.md

if [[ "$failures" -ne 0 ]]; then
  exit 1
fi

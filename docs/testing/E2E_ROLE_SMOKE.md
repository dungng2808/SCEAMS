# SCEAMS – Phase 131 E2E theo role

## Phạm vi

`postman/SCEAMS.e2e-role-smoke.postman_collection.json` chạy qua HTTP boundary
thật và kiểm tra landing flow của bốn role. Mỗi request gắn token riêng, không
truyền `UserId`/`StudentId` từ client để quyết định scope.

| Vai trò | Chuỗi smoke test |
|---|---|
| Admin | login/token → user list → category/venue → event-summary report → notification logs |
| Staff | token → pending Club/Event queue → event-summary/club-activity report → notification logs |
| Organizer | token → Club/Event scope → member list → attendance report |
| Student | token → profile → public Club/Event → registration history → chatbot retrieval → history/rate-limit contract |
| gRPC | API notification client được gọi ở workflow approve/cancel; notification logs xác nhận correlation/delivery không trùng |

## Chạy

```bash
npx --yes newman run postman/SCEAMS.e2e-role-smoke.postman_collection.json \
  -e /tmp/sceams-e2e-local.json --insecure --bail
```

`/tmp/sceams-e2e-local.json` phải được tạo từ environment example nội bộ và có
`adminAccessToken`, `staffAccessToken`, `organizerAccessToken`,
`studentAccessToken`, `clubId` và `eventId`. Không commit token.

Collection chỉ dùng GET/POST retrieval không làm thay đổi dữ liệu; lifecycle
mutation (create/approve/register/check-in/feedback) đã có request có thứ tự
trong collection chính và được chạy ở database Development riêng.

## Kết quả mong đợi

- Mỗi role nhận `200` ở tài nguyên đúng scope và `401/403` nếu đổi token sai
  quyền (đã được kiểm tra ở Phase 129).
- Student không đọc được dữ liệu nội bộ của role khác; history chỉ theo subject
  JWT.
- API trả `ProblemDetails` cho lỗi; không có stack trace hoặc credential trong
  response.
- Notification log có tối đa một delivery cho cùng correlation ID.
